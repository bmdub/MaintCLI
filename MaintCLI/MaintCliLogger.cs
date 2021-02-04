using System;
using Microsoft.Extensions.Logging;

namespace MaintCliNS
{
    /// <summary>
    /// An ILogger implementation that logs to MaintCli.
    /// </summary>
    public class MaintCliLogger : ILogger
    {
        readonly MaintCli _maintCli;
        readonly string _name;
        readonly MaintCliLoggerConfiguration _config;

        /// <summary>Instantiates a new MaintCliLogger.</summary>
        /// <param name="maintCli">An existing MaintCli instance.</param>
        /// <param name="name">The name of the logger.</param>
        /// <param name="config">The logger configuration.</param>
        public MaintCliLogger(MaintCli maintCli, string name, MaintCliLoggerConfiguration config) =>
            (_maintCli, _name, _config) = (maintCli, name, config);

        /// <summary>Currently a no-op.</summary>
        /// <typeparam name="TState"></typeparam>
        /// <param name="state"></param>
        /// <returns></returns>        
        public IDisposable BeginScope<TState>(TState state) => default;

        /// <summary>Returns if this logger is enabled for the given log level.</summary>
        /// <param name="logLevel">The log level.</param>
        /// <returns>True if enabled.</returns>
        public bool IsEnabled(LogLevel logLevel) =>
            logLevel >= _config.LogLevel;

        /// <summary>Writes a log entry to the CLI.</summary>
        /// <typeparam name="TState">The state type.</typeparam>
        /// <param name="logLevel">The log level of the event.</param>
        /// <param name="eventId">The event ID.</param>
        /// <param name="state">The state.</param>
        /// <param name="exception">The exception, if any.</param>
        /// <param name="formatter">The formatter used to create the text for display.</param>
        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception exception,
            Func<TState, Exception, string> formatter)
        {
            if (!IsEnabled(logLevel))
            {
                return;
            }

            if (_config.EventId == 0 || _config.EventId == eventId.Id)
            {
                // Color code the message according to the log level, and send to the CLI.

                string color;
                switch(logLevel)
				{
                    case LogLevel.Critical:
                        color = "#FF00FF";
                        break;
                    case LogLevel.Error:
                        color = "#FF0000";
                        break;
                    case LogLevel.Warning:
                        color = "#FFFF00";
                        break;
                    case LogLevel.Information:
                        color = "#FFFFFF";
                        break;
                    case LogLevel.Debug:
                        color = "#BBBBBB";
                        break;
                    case LogLevel.Trace:
                        color = "#00FFFF";
                        break;
                    default:
                        color = "#FFFFFF";
                        break;
                }

                var message = 
                    $"<div style='color:{color}'>" +
                    HtmlUtility.HtmlEncode($"{logLevel}: {_name}[{eventId}]: {formatter(state, exception)}") +
                    "</div>";

                _maintCli.SendMessage(message, SendOptions.AllowHtml);
            }
        }
    }
}