using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace MaintCliNS
{
    /// <summary>
    /// Logger provider for MaintCliLogger.
    /// </summary>
    public sealed class MaintCliLoggerProvider : ILoggerProvider
    {
        readonly MaintCli _maintCli;
        readonly MaintCliLoggerConfiguration _config;
        readonly ConcurrentDictionary<string, MaintCliLogger> _loggers =
            new ConcurrentDictionary<string, MaintCliLogger>();

        /// <summary>Creates a new instance of MaintCliLoggerProvider.</summary>
        /// <param name="maintCli">An instance of MaintCli.</param>
        /// <param name="config">Configuration for the logger.</param>
        public MaintCliLoggerProvider(MaintCli maintCli, MaintCliLoggerConfiguration config)
        {
            _maintCli = maintCli;
            _config = config;
        }

        /// <summary>Creates a new instance of MaintCliLoggerProvider using the default MaintCli instance.</summary>
        /// <param name="config">Configuration for the logger.</param>
        public MaintCliLoggerProvider(MaintCliLoggerConfiguration config)
        {
            _maintCli = MaintCli.Default;
            _config = config;
        }

        /// <summary>Creates an instance of the logger.</summary>
        /// <param name="categoryName">Category to use for this logger.</param>
        /// <returns>A new MaintCliLogger instance.</returns>
        public ILogger CreateLogger(string categoryName) =>
            _loggers.GetOrAdd(categoryName, name => new MaintCliLogger(_maintCli, name, _config));

        /// <summary></summary>
        public void Dispose() => _loggers.Clear();
    }
}