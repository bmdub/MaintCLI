using System;
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
			var echo = new CommandBuilder("echo", "repeats what you typed", "echo \"what up\"")
				.AddArgument("string", "the string, surrounded by quotes, to echo")
				.AddOption("s", true, "appends a sender name.")
				.AddOption("b", false, "appends exclamation.")
				.Execute((sender, args) =>
				{
					//Thread.Sleep(1000);

					var builder = new StringBuilder();

					builder.Append(args["string"]);
					if (args.Contains("B"))
						builder.Append("!");
					if (args.TryGetValue("s", out var theSender))
						builder.Append($" -{theSender}");
					//builder.AppendLine();

					MaintCli.Default.SendMessageTo(sender, builder.ToString());
				})
				.Build();

			MaintCli.Default.AddCommand(echo);

			//Console.WriteLine(MaintCli.ListCommands());

			//Console.WriteLine(command.ToString());

			//MaintCli.RunCommand(@"echo -b ""whats up"" --s=""it's me""");

			//Console.ReadKey();

			MaintCli.Default.OnLog += (LogLevel logLevel, string logMessage) =>
			{
				Console.WriteLine($"{logLevel}: {logMessage}");
			};



			OpenBrowser("http://localhost:5000");

			// 5000 redirects to 5001 for https upgrade
			MaintCli.Default.Start(httpEndPoint: new IPEndPoint(IPAddress.Any, 5000), httpsEndPoint: new IPEndPoint(IPAddress.Any, 5001), requireAuthentication: true, asynchronous: true, inactivityTimeout: TimeSpan.FromMinutes(1), sessionTimeout: TimeSpan.FromMinutes(2));

			MaintCli.Default.OnAuthenticate += (string username, string password) =>
			{
				return Task.FromResult(true);
			};

			MaintCli.Default.EnableTimeStamps = true;

			//MaintCli.MessagesToRetain = 10;

			//MaintCli.Color = Color.Green;

			//MaintCli.TextShadowColor = Color.Orange;

			//MaintCli.BackgroundColor = Color.White;
			//MaintCli.BackgroundColor = System.Drawing.Color.FromArgb(200, rand.Next(0, 256), rand.Next(0, 256), rand.Next(0, 256));

			//MaintCli.BackgroundRepeat = true;

			//MaintCli.BackgroundStretch = true;

			//MaintCli.FontSize = 30;

			//MaintCli.FontFamilies = ImmutableList<string>.Empty.Add("Courier New");

			//MaintCli.Default.BackgroundImageFile = @"C:\Users\bwoeg\Downloads\asdffff.gif";

			//MaintCli.Default.BackgroundImageUrl = $"https://cataas.com/cat?{DateTime.Now.ToString()}";

			//MaintCli.Default.BackgroundRepeat = true;

			var host = Host.CreateDefaultBuilder(args)
				.ConfigureLogging(builder =>
					builder
					.ClearProviders()
					.SetMinimumLevel(LogLevel.Trace)
					/*.AddProvider(
						new MaintCliLoggerProvider(
							new MaintCliLoggerConfiguration
							{
								LogLevel = LogLevel.Error,
							}))
					.AddMaintCliLogger()*/					
					.AddMaintCliLogger(configuration =>
					{
						configuration.LogLevel = LogLevel.Trace;
					}))
					.Build();
			var logger = (ILogger<Program>)host.Services.GetService(typeof(ILogger<Program>));


			for (; ; )
			{
				break;
				Thread.Sleep(1000);

				//MaintCli.HandleCommands();

				logger.LogCritical("Critical");
				logger.LogError("Error");
				logger.LogWarning("Warning");
				logger.LogInformation("Information");
				logger.LogDebug("Debug");
				logger.LogTrace("Trace");

				//MaintCliNS.MaintCli.SendMessage("test");

				//MaintCli.Default.Disconnect("asdf");
			}

			Console.WriteLine("Done.");
			Console.ReadKey();
		}

		private static void OpenBrowser(string url)
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
