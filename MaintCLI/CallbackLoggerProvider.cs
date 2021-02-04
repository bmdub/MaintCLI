using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace MaintCliNS
{
    /// <summary>
    /// This class is simply used by the CLI to capture log messages from Microsoft's web host, exposing them to the user.
    /// </summary>
    class CallbackLoggerProvider : ILoggerProvider
    {
        readonly MaintCli _maintCli;
        readonly ConcurrentDictionary<string, CallbackLogger> _loggers =
            new ConcurrentDictionary<string, CallbackLogger>();

        public CallbackLoggerProvider(MaintCli maintCli) =>
            _maintCli = maintCli;

        public ILogger CreateLogger(string categoryName) =>
            _loggers.GetOrAdd(categoryName, name => new CallbackLogger(_maintCli, name));

        public void Dispose() => _loggers.Clear();
    }
}