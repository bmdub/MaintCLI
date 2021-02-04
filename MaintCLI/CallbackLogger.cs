using System;
using Microsoft.Extensions.Logging;

namespace MaintCliNS
{
	/// <summary>
	/// This class is simply used by the CLI to capture log messages from Microsoft's web host, exposing them to the user.
	/// </summary>
	class CallbackLogger : ILogger
	{
		readonly MaintCli _maintCli;
		readonly string _name;

		public CallbackLogger(MaintCli maintCli, string name)
		{
			_maintCli = maintCli;
			_name = name;
		}

		public IDisposable BeginScope<TState>(TState state) => default;

		public bool IsEnabled(LogLevel logLevel) => true;

		public void Log<TState>(
			LogLevel logLevel,
			EventId eventId,
			TState state,
			Exception exception,
			Func<TState, Exception, string> formatter)
		{
			_maintCli.LogMessage(logLevel, formatter(state, exception));
		}
	}
}