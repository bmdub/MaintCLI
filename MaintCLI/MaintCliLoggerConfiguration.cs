using System;
using Microsoft.Extensions.Logging;

namespace MaintCliNS
{
	/// <summary>
	/// Configuration input for the MaintCliLogger.
	/// </summary>
	public class MaintCliLoggerConfiguration
	{
		/// <summary>Used to match with only the event IDs of the events you want to log. Use '0' to match all event IDs.</summary>
		public int EventId { get; set; }

		/// <summary>The minimum log level of events to log.</summary>
		public LogLevel LogLevel { get; set; } = LogLevel.Information;
	}
}