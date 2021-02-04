using System;
using Microsoft.Extensions.Logging;

namespace MaintCliNS
{
    /// <summary>
    /// Extensions that provide easy ways to add the logger to an application.
    /// </summary>
    public static class MaintCliLoggerExtensions
    {
        /// <summary>Adds MaintCli as a logger to a LoggingBuilder.</summary>
        /// <param name="builder">The logging builder.</param>
        /// <param name="maintCliInst">An instance of MaintCli. If not provided, the default static MaintCli.Default will be used.</param>
        /// <returns>The same logging builder.</returns>
        public static ILoggingBuilder AddMaintCliLogger(this ILoggingBuilder builder, MaintCli maintCliInst = null) =>
            builder.AddMaintCliLogger(new MaintCliLoggerConfiguration(), maintCliInst);

        /// <summary>Adds MaintCli as a logger to a LoggingBuilder.</summary>
        /// <param name="builder">The logging builder.</param>
        /// <param name="configure">Action that will provide the logger configuration.</param>
        /// <param name="maintCliInst">An instance of MaintCli. If not provided, the default static MaintCli.Default will be used.</param>
        /// <returns>The same logging builder.</returns>
        public static ILoggingBuilder AddMaintCliLogger(this ILoggingBuilder builder, Action<MaintCliLoggerConfiguration> configure, MaintCli maintCliInst = null)
        {
            var config = new MaintCliLoggerConfiguration();
            configure(config);

            return builder.AddMaintCliLogger(config, maintCliInst);
        }

        /// <summary>Adds MaintCli as a logger to a LoggingBuilder.</summary>
        /// <param name="builder">The logging builder.</param>
        /// <param name="config">The logger configuration.</param>
        /// <param name="maintCliInst">An instance of MaintCli. If not provided, the default static MaintCli.Default will be used.</param>
        /// <returns>The same logging builder.</returns>
        public static ILoggingBuilder AddMaintCliLogger(this ILoggingBuilder builder, MaintCliLoggerConfiguration config, MaintCli maintCliInst = null)
        {
            if (maintCliInst == null)
                maintCliInst = MaintCli.Default;

            builder.AddProvider(new MaintCliLoggerProvider(maintCliInst, config));
            return builder;
        }
    }
}