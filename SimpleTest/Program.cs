using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Drawing;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MaintCliNS;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace SimpleTest
{
	class Program
	{
		static void Main(string[] args)
		{
			// Start the CLI.
			// Note: You are free to add commands, change settings, and subscribe to events before or after this call, at any point in the application.
			MaintCli.Default.Start(
				httpEndPoint: new IPEndPoint(IPAddress.Any, 5000), 
				httpsEndPoint: new IPEndPoint(IPAddress.Any, 5001), 
				//certificate: ...
				httpsRedirection: true,
				requireAuthentication: true, 
				asynchronous: true, 
				maxConcurrentCommands: 2,
				inactivityTimeout: TimeSpan.FromMinutes(10)
				);

			// Authenticate credentials here
			MaintCli.Default.OnAuthenticate += (string username, string password) =>
			{
				if (username == "username123" && password == "password123")
					return Task.FromResult(true);

				return Task.FromResult(false);
			};

			// Display a message to the user on connection. Note: Sent messages at this point will only appear if authentication is not required.
			MaintCli.Default.OnConnected += (Client client) =>
				MaintCli.Default.SendMessageTo(client, "You connected to my app... Congrats!");

			// Display a message to the user on authentication.
			MaintCli.Default.OnAuthenticated += (Client client) =>
				MaintCli.Default.SendMessageTo(client, "You are now authenticated. Unauthorized activity is prohibited.");

			// Optionally view MaintCli's internal logs for debugging.
			//MaintCli.Default.OnLog += (LogLevel logLevel, string logMessage) =>
				//Console.WriteLine($"{logLevel}: {logMessage}");

			// Create an 'echo' command.
			MaintCli.Default.AddCommand(
				new CommandBuilder("echo", "echos the argument you specify", "echo \"hello wrold\"")
				.AddArgument("text", "the string, surrounded by quotes, to echo")
				.AddOption("r", true, "repeats the echo N times")
				.AddOption("c", false, "capitalizes the string")
				.Execute((sender, args) =>
				{
					var builder = new StringBuilder();

					int repeatCount = 0;
					if (args.TryGetValue("r", out var countStr))
						repeatCount = int.Parse(countStr);

					for (int i = 0; i < repeatCount; i++)
					{
						var text = args["text"];

						if (args.Contains("c"))
							text = text.ToUpper();

						builder.AppendLine(text);
					}

					MaintCli.Default.SendMessageTo(sender, builder.ToString());
				})
				.Build());

			// Uncomment any of these client settings to try them out.
			//MaintCli.Default.EnableTimeStamps = true;
			//MaintCli.Default.MessagesToRetain = 10_000;
			//MaintCli.Default.FontSize = 30;
			//MaintCli.Default.FontFamilies = ImmutableList<string>.Empty.Add("Wingdings").Add("Monaco");
			//MaintCli.Default.Color = Color.GreenYellow;
			//MaintCli.Default.TextShadowColor = Color.OrangeRed;
			//MaintCli.Default.BackgroundColor = Color.FromArgb(255, 50, 0, 0);
			//MaintCli.Default.BackgroundGradientColors = ImmutableList<Color>.Empty.Add(Color.FromArgb(255, 0, 0, 0)).Add(Color.FromArgb(255, 40, 0, 0));
			//MaintCli.Default.BackgroundGradientDirection = 315;
			//MaintCli.Default.BackgroundImageFile = @"C:\memes\thatonewiththecatlol.gif";
			//MaintCli.Default.BackgroundImageUrl = $"https://cataas.com/cat?nocachetrick={DateTime.UtcNow}";
			//MaintCli.Default.BackgroundRepeat = true;
			//MaintCli.Default.BackgroundStretch = true;

			// To add MaintCli as a logger to a web/host (or something else with a LoggingBuilder)
			var myHost = Host.CreateDefaultBuilder(args)
				.ConfigureLogging(builder => builder
					.ClearProviders()
					.SetMinimumLevel(LogLevel.Trace)
					.AddMaintCliLogger(configuration =>
					{
						configuration.LogLevel = LogLevel.Trace;
					}))
					.Build();

			// To manually create a logger for general applications
			var provider = new MaintCliLoggerProvider(new MaintCliLoggerConfiguration() { LogLevel = LogLevel.Trace });
			var logger = provider.CreateLogger(nameof(Program));

			// Test our logger
			MaintCli.Default.OnConnected += (Client client) =>
			{
				logger.LogCritical("Critical");
				logger.LogError("Error");
				logger.LogWarning("Warning");
				logger.LogInformation("Information");
				logger.LogDebug("Debug");
				logger.LogTrace("Trace");
			};

			// Open a web browser to our app for convenience
			if (Debugger.IsAttached)
				OpenBrowserTo("http://localhost:5000");

			Console.WriteLine("Enter text to send to connected clients:");
			for(; ;)
			{
				var line = Console.ReadLine();

				// Send a message to all connected clients
				MaintCli.Default.SendMessage(line);
			}
		}

		static void OpenBrowserTo(string url)
		{
			url = url.Replace("*", "localhost");

			if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
				Process.Start(new ProcessStartInfo("cmd", $"/c start {url.Replace("&", "^&")}") { CreateNoWindow = true });
			else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
				Process.Start("xdg-open", url);
			else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
				Process.Start("open", url);
		}
	}
}
