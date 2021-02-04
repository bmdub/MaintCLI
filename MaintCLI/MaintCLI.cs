using System.Diagnostics;
using System.IO;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Collections.Concurrent;
using System.Linq;
using System.Collections.Generic;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace MaintCliNS
{
	/// <summary>
	/// Provides a command-line interface to an application via HTTP.
	/// </summary>
	public partial class MaintCli
	{
		/// <summary>A default, static instance of MaintCli that can be used for convenience.</summary>
		public static MaintCli Default { get; } = new MaintCli();

		/// <summary>Delegate type used for receiving MaintCli log messages.</summary>
		/// <param name="logLevel">The log level for this event.</param>
		/// <param name="logMessage">The log message.</param>
		public delegate void LogHandler(LogLevel logLevel, string logMessage);
		/// <summary>Event that fires when MaintCli logs information internally.</summary>
		public event LogHandler OnLog;

		/// <summary>Delegate type used for authenticating a new connection.</summary>
		/// <param name="username">The given username.</param>
		/// <param name="password">The given password.</param>
		/// <returns>True if the credentials pass authentication, allowing the client to run commands.</returns>
		public delegate Task<bool> AuthHandler(string username, string password);
		/// <summary>Event that fires when a connected client requests authentication. If authentication is required, then this event must be handled, and authentication implemented.</summary>
		public event AuthHandler OnAuthenticate;

		/// <summary>The HTTP endpoint that the CLI is listening on.</summary>
		public IPEndPoint HttpEndPoint { get; private set; }
		/// <summary>The HTTPS endpoint that the CLI is listening on.</summary>
		public IPEndPoint HttpsEndPoint { get; private set; }
		/// <summary>If true, redirects incoming HTTP connections to the HTTPS end point.</summary>
		public bool HttpsRedirection { get; private set; }
		/// <summary>The certificate that is used to autheticate the server.</summary>
		public X509Certificate2 Certificate { get; private set; }
		/// <summary>If true, requires that all clients send in credentials to be authenticated before issuing commands.</summary>
		public bool RequireAuthentication { get; private set; }
		/// <summary>True if incoming commands are to be executed asynchronously on threadpool threads.</summary>
		public bool Asynchronous { get; private set; }
		/// <summary>The number of received commands that can be executed simultaneously.</summary>
		public int MaxConcurrentCommands { get; private set; }
		/// <summary>The number of messages that can be sent to all users simultaneously.</summary>
		public int MaxConcurrentMessages { get; private set; }
		/// <summary>The time to wait before closing a client connection due to inactivity by the client.</summary>
		public TimeSpan InactivityTimeout { get; private set; }
		/// <summary>Any client will be disconnected after this period of time, regardless of activity.</summary>
		public TimeSpan SessionTimeout { get; private set; }

		ClientCache _clientCache = new ClientCache();
		IHubContext<MaintCliHub, IClientRpc> _hubContext;
		SemaphoreSlim _commandSemaphore;
		SemaphoreSlim _messageSemaphore;
		ConcurrentQueue<(Client Sender, string Message)> _syncCommandQueue = new ConcurrentQueue<(Client Sender, string Message)>();
		object _runLock = new object();
		bool _isRunning;

		/// <summary>Creates a new instance of MaintCli.</summary>
		public MaintCli()
		{
			// Add some default commands
			AddCommand(
				new CommandBuilder("cls", "Clears the message history.")
					.Execute((user, args) => _hubContext?.Clients?.Client(user.ConnectionID)?.ClearScreen())
					.Build());
			AddCommand(
				new CommandBuilder("help", "Displays a list of the available commands.")
					.Execute((user, args) => SendMessageTo(user, ListCommands()))
					.Build());
			AddCommand(
				new CommandBuilder("exit", "Disconnects from the server.")
					.Execute((user, args) => DisconnectClient(user))
					.Build());
		}

		/// <summary>Starts the MaintCli server.</summary>
		/// <param name="httpEndPoint">The HTTP endpoint that the CLI will listen on. If null, only the HTTPS endpoint will be used.</param>
		/// <param name="httpsEndPoint">The (optional) HTTPS endpoint that the CLI will listen on. If null, only the HTTP endpoint will be used.</param>
		/// <param name="httpsRedirection">If true, redirects incoming HTTP connections to the HTTPS end point.</param>
		/// <param name="certificate">The certificate that is used to authenticate the server over HTTPS. If null, an untrusted SSL cert will be generated locally (see the 'dotnet dev-certs' command for more information).</param>
		/// <param name="requireAuthentication"><em>NOTE: Authentication is NOT secure when using an HTTP end point.</em> If true, requires that clients authenticate with credentials. Authentication must be handled by subscribing to the OnAuthenticate event.</param>
		/// <param name="asynchronous">True if incoming commands are to be executed asynchronously on threadpool threads. If false, the user must call HandleCommands() periodically to execute incoming commands.</param>
		/// <param name="maxConcurrentCommands">The number of received commands that can be executed simultaneously, or with each call to HandleCommands() (if asynchronous.) When exceeded, commands will be dropped.</param>
		/// <param name="inactivityTimeout">The time to wait before closing a client connection due to inactivity by the client. Default: 10 minutes.</param>
		/// <param name="sessionTimeout">Any client will be disconnected after this period of time, regardless of activity. Default: 1 month.</param>
		public void Start(
			IPEndPoint httpEndPoint = null,
			IPEndPoint httpsEndPoint = null,
			bool httpsRedirection = false,
			X509Certificate2 certificate = null,
			bool requireAuthentication = false,
			bool asynchronous = true,
			int maxConcurrentCommands = 2,
			TimeSpan? inactivityTimeout = null,
			TimeSpan? sessionTimeout = null)
		{
			lock (_runLock)
			{
				if (_isRunning)
					throw new InvalidOperationException($"Instance is already in the 'running' state.");

				HttpEndPoint = httpEndPoint;
				HttpsEndPoint = httpsEndPoint;
				Certificate = certificate;
				RequireAuthentication = requireAuthentication;
				MaxConcurrentCommands = maxConcurrentCommands;
				Asynchronous = asynchronous;
				InactivityTimeout = inactivityTimeout == null ? TimeSpan.FromMinutes(10) : inactivityTimeout.Value;
				SessionTimeout = sessionTimeout == null ? TimeSpan.FromDays(30) : sessionTimeout.Value;
				HttpsRedirection = httpsRedirection;

				if (HttpEndPoint == null && HttpsEndPoint == null)
					throw new ArgumentNullException($"At least one of 'httpEndPoint' or 'httpsEndPoint' must not be null.");

				_clientCache = new ClientCache(InactivityTimeout, SessionTimeout);

				// Throttle how many commands that the application will service simultaneously.
				// Additional commands exceeding this amount will be dropped and the client notified.
				_commandSemaphore = new SemaphoreSlim(MaxConcurrentCommands);

				// Disallow buildup of message sends in memory.
				// If the application attempts to send too many messages at once, they will be dropped.
				_messageSemaphore = new SemaphoreSlim(256);

				// Start a thread that checks the client list every few seconds for session expiry.
				// (Note: Slightly more efficient solutions exist at the cost of complexity).
				var sessionTimeoutThread = Task.Factory.StartNew(() =>
				{
					for (; ; )
					{
						foreach (var user in _clientCache.Clients)
							if (_clientCache.IsInactive(user.ConnectionID) || _clientCache.IsSessionExpired(user.ConnectionID))
								DisconnectClient(user);

						Thread.Sleep(5_000);
					}
				},
				TaskCreationOptions.LongRunning);

				// Configure and start our web host.
				var host = Host.CreateDefaultBuilder()
					.ConfigureWebHostDefaults(webHostBuilder =>
					{
						webHostBuilder.ConfigureServices(services =>
						{
							// Configure SignalR here.
							services.AddSignalR(hubOptions =>
							{
								// Options defined: https://docs.microsoft.com/en-us/aspnet/core/signalr/configuration
								hubOptions.EnableDetailedErrors = true;
								hubOptions.MaximumReceiveMessageSize = 4096; // TODO: Make configurable?
							});
						})
						.UseKestrel(kestrelServerOptions =>
						{
							// Configure the endpoints here.
							if (HttpEndPoint != null)
								kestrelServerOptions.Listen(HttpEndPoint);

							if (HttpsEndPoint != null)
							{
								kestrelServerOptions.Listen(HttpsEndPoint, listenOptions =>
								{
									if (Certificate == null)
										listenOptions.UseHttps();
									else
										listenOptions.UseHttps(Certificate);
								});
							}
						})
						.ConfigureLogging(builder =>
						{
							// Redirect internal messages to our OnLog callback event, if the user is interested in seeing them.
							builder.ClearProviders();
							builder.AddProvider(new CallbackLoggerProvider(this));
						})
						.ConfigureServices(services =>
						{
							// We'll need to inject this instance into the SignalR hubs.
							services.AddSingleton(this);
						})
						.Configure(app =>
						{
							// Configure routing.
							if (Debugger.IsAttached)
								app.UseDeveloperExceptionPage();

							if (HttpsRedirection)
								app.UseHttpsRedirection();

							app.UseRouting();

							app.UseAuthorization();

							// Retrieve the client html and js that we have embedded in the library.
							// We then make necessary runtime modifications (authentication) before sending it to the client.
							// Note: An embedded resource is prefixed by the default namespace.
							var clientJS = 
							 $@"{ResourceUtility.GetTextResource($"{nameof(MaintCliNS)}.signalr.min.js")}\r\n
								{ResourceUtility.GetTextResource($"{nameof(MaintCliNS)}.maintcli.js")
									.Replace("__REQUIRE_AUTHENTICATION", RequireAuthentication ? "true" : "false")}";

							var clientHTML = ResourceUtility.GetTextResource($"{nameof(MaintCliNS)}.index.html")
													.Replace("<!--SCRIPT-->", "<script>" + clientJS + "</script>");

							// When someone browses to "/", send them the HTML for the CLI.
							app.UseEndpoints(endpoints =>
							{
								endpoints.MapGet("/", async context =>
								{
									await context.Response.WriteAsync(clientHTML);
								});

								// Subsequent SignalR communication will be via "/maintclihub".
								// Options listed here: https://docs.microsoft.com/en-us/aspnet/core/signalr/configuration
								endpoints.MapHub<MaintCliHub>("/maintclihub");
							});
						});
					})
					.Build();

				// We need the SignalR hub context so that we can proactively send messages to SignalR connections.
				_hubContext = (IHubContext<MaintCliHub, IClientRpc>)host.Services.GetService(typeof(IHubContext<MaintCliHub, IClientRpc>));

				// Run our web host. If there is a problem, fail silently.
				host
					.RunAsync()
					.ContinueWith(task =>
					{
						LogMessage(LogLevel.Error, task.Exception.ToString());
					}, TaskContinuationOptions.OnlyOnFaulted);

				_isRunning = true;
			}
		}

		internal void HandleCommandMessage(string connectionId, string message)
		{
			if (_clientCache.TryGetClient(connectionId, out var client) == false)
			{
				LogMessage(LogLevel.Warning, $"Received command from unknown connection with ID: {connectionId}");
				throw new InvalidOperationException($"Received command from unknown connection.");
			}

			if (_commandSemaphore.Wait(0) == false)
			{
				throw new InvalidOperationException($"Server exceeded max concurrent {nameof(MaintCli)} jobs: {MaxConcurrentCommands}.");
			}

			if (Asynchronous)
			{
				// Fire off the command in a threadpool thread.
				_ = Task.Run(() => HandleCommand(client, message));
			}
			else
			{
				// Queue up the command for manual execution later using "HandleCommands".
				_syncCommandQueue.Enqueue((client, message));
			}
		}

		IEnumerable<string> GetUnauthedConnectionIDs() =>
			_clientCache.Clients.Where(client => !client.Authenticated).Select(client => client.ConnectionID);

		IClientRpc GetAuthenticatedClientRpcInterfaces()
		{
			return RequireAuthentication ?
				// Look in our token cache for authenticated users,
				// since we can't count on unauthenticated users being actually disconnected.
				_hubContext?.Clients?.AllExcept(GetUnauthedConnectionIDs())
				:
				_hubContext?.Clients?.All;
		}

		/// <summary>Sends the given message to all connected and authenticated clients.</summary>
		/// <param name="message">The message to display.</param>
		/// <param name="sendOptions">Send options.</param>
		public void SendMessage(string message, SendOptions sendOptions = SendOptions.None) =>
			SendMessageToClientRpcInterfaces(GetAuthenticatedClientRpcInterfaces(), message, sendOptions);

		/// <summary>Sends the given message to all connected and authenticated clients.</summary>
		/// <param name="client">The client to send the message to.</param>
		/// <param name="message">The message to display.</param>
		/// <param name="sendOptions">Send options.</param>
		public void SendMessageTo(Client client, string message, SendOptions sendOptions = SendOptions.None)
		{
			var clientRpc = _hubContext?.Clients?.Client(client.ConnectionID);

			if (RequireAuthentication)
				if (_clientCache.TryGetClient(client.ConnectionID, out var freshClient) == false || freshClient.Authenticated == false)
					return;

			if (clientRpc == null)
			{
				LogMessage(LogLevel.Warning, $"Attempt to send to unavailable client.");
				return;
			}

			SendMessageToClientRpcInterfaces(clientRpc, message, sendOptions);
		}

		internal void SendMessageToClientRpcInterfaces(IClientRpc clientRpc, string message, SendOptions sendOptions = SendOptions.None)
		{
			// Make sure that the service is initialized before attempting send.
			if (clientRpc == null)
			{
				LogMessage(LogLevel.Warning, $"Attempt to send message when {nameof(MaintCli)} is not running.");
				return;
			}

			if (!sendOptions.HasFlag(SendOptions.AllowHtml))
			{
				// No HTML content; escape the message, and translate special characters for HTML display.
				message = HtmlUtility.HtmlEncode(message);
			}

			if (_messageSemaphore.Wait(0) == false)
			{
				LogMessage(LogLevel.Warning, $"Message discarded: Server exceeded max concurrent {nameof(MaintCli)} messages in flight: {MaxConcurrentMessages}.");
				return;
			}

			_ = clientRpc.ServerMessage(message).ContinueWith(t =>
			{
				_messageSemaphore.Release();

				if (t.IsFaulted)
					LogMessage(LogLevel.Warning, $"{nameof(MaintCli)} message send error: {t.Exception.Message}.");
			});
		}

		/// <summary>When Asynchronous=false, causes pending client commands to execute on the current thread, blocking until they have finished.</summary>
		public void HandleCommands()
		{
			while (_syncCommandQueue.TryDequeue(out var info))
				HandleCommand(info.Sender, info.Message);
		}

		void HandleCommand(Client client, string message)
		{
			try
			{
				RunCommand(client, message);
			}
			catch (Exception ex)
			{
				SendMessageTo(client, ex.Message);
			}
			finally
			{
				_commandSemaphore.Release();
			}
		}

		/// <summary>Instructs MaintCli to disconnect from a connected client.</summary>
		/// <param name="username">The username of the client.</param>
		public void DisconnectClient(string username)
		{
			foreach (var client in _clientCache.GetClientsByUsername(username))
				DisconnectClient(client);
		}

		/// <summary>Instructs MaintCli to disconnect from a connected client.</summary>
		/// <param name="client">The client.</param>
		public void DisconnectClient(Client client) =>
			Disconnect(client.ConnectionID);

		internal void Disconnect(string connectionId)
		{
			// Ensure that they can no longer send commands or receive messages.
			// Unfortunately there is no way to forcefully disconnect a connection on the server side.
			_clientCache.Remove(connectionId);

			// Tell the client to disconnect from their end.
			_hubContext?.Clients?.Client(connectionId)?.Disconnect();
		}

		internal async Task<bool> Authenticate(string username, string password)
		{
			// Check with the application server to determine authenticity.
			if (OnAuthenticate == null)
				return false;

			return await OnAuthenticate.Invoke(username, password);
		}

		internal void MarkConnectionAsAuthenticated(string connectionId, string username) =>
			_clientCache.MarkAsAuthenticated(connectionId, username);

		internal bool ConnectionIsAuthenticated(string connectionId)
		{
			if (_clientCache.TryGetClient(connectionId, out var client) == false)
				return false;

			if (RequireAuthentication && !client.Authenticated)
				return false;

			return true;
		}

		internal void RegisterConnectionActivity(string connectionId)
		{
			// For the purposes of keeping the session alive.
			_clientCache.RegisterActivity(connectionId);
		}

		internal void RegisterConnection(string connectionId)
		{
			// Register the connection for the purposes of keeping track of the session.
			_clientCache.Add(connectionId);
		}

		internal void DeregisterConnection(string connectionId)
		{
			// Deregister to prevent further commands by the client.
			_clientCache.Remove(connectionId);
		}

		internal void LogMessage(LogLevel logLevel, string logMessage) =>
			OnLog?.Invoke(logLevel, logMessage);
	}
}
