using Microsoft.AspNetCore.SignalR;
using System;
using System.Threading.Tasks;

namespace MaintCliNS
{
	/// <summary>
	/// The SignalR hub used for reception of client messages.
	/// </summary>
	class MaintCliHub : Hub<IClientRpc>
	{
		private MaintCli _maintCli;

		public MaintCliHub(MaintCli maintCli)
		{
			_maintCli = maintCli;
		}

		public override async Task OnConnectedAsync()
		{
			await base.OnConnectedAsync();

			// Keep track of who is connected
			_maintCli.RegisterConnection(Context.ConnectionId);

			// Send the client settings over
			_maintCli.SendAllSettingsTo(Clients.Caller);
		}

		public override async Task OnDisconnectedAsync(Exception exception)
		{
			await base.OnDisconnectedAsync(exception);

			// Keep track of who is connected
			_maintCli.DeregisterConnection(Context.ConnectionId);
		}

		public async Task BeginAuthentication()
		{
			// Client has requested to start the authentication handshake.
			// Tell the client to send us the user name.
			await Clients.Caller.PromptForUsername();
		}

		public async Task AuthenticateUsername(string username)
		{
			// Client sent us the user name.
			// Tell the client to send us the password.
			Context.Items["username"] = username;

			await Clients.Caller.PromptForPassword();
		}

		public async Task AuthenticatePassword(string password)
		{
			// Given the username/password, ask the application if they are authorized.

			try
			{
				var username = Context.Items["username"] as string;

				if (await _maintCli.Authenticate(username, password) == false)
					throw new UnauthorizedAccessException("Authorization failed - Bad username/password.");

				// Mark the connection as authenticated.
				_maintCli.MarkConnectionAsAuthenticated(Context.ConnectionId, username);

				// Notify the client that they are authenticated.
				await Clients.Caller.Authenticated();
			}
			catch (UnauthorizedAccessException ex)
			{
				// Bad credentials; prompt for the creds again
				await Clients.Caller.ServerMessage(ex.Message);

				await Clients.Caller.PromptForUsername();

				return;
			}
		}

		public async Task Command(string message)
		{
			// Client has sent us a command

			try
			{
				if (!_maintCli.ConnectionIsAuthenticated(Context.ConnectionId))
					throw new UnauthorizedAccessException("User not authorized to perform this action.");

				_maintCli.RegisterConnectionActivity(Context.ConnectionId);
			}
			catch (Exception ex)
			{
				_maintCli.LogMessage(Microsoft.Extensions.Logging.LogLevel.Warning, $"Error executing client command: {ex.Message}");

				await Clients.Caller.ServerMessage(ex.Message);

				_maintCli.Disconnect(Context.ConnectionId);

				return;
			}

			_maintCli.HandleCommandMessage(Context.ConnectionId, message);
		}
	}
}