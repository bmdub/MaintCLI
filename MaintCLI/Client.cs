
using System;

namespace MaintCliNS
{
	/// <summary>
	/// Represents the state of a client connection at a certain point in time.
	/// </summary>
	public record Client
	{
		internal readonly string ConnectionID;

		/// <summary>Represents the time, in UTC, when the connection was established.</summary>
		public DateTimeOffset LoginTime { get; init; }

		/// <summary>Represents the time, in UTC, when the client last issued a command.</summary>
		public DateTimeOffset LastActivityTime { get; init; }

		/// <summary>If authenticated, the username provided by the client.</summary>
		public string Username { get; init; }

		/// <summary>Indicates whether or not the client's credentials have been authenticated.</summary>
		public bool Authenticated { get; init; }

		private Client() { }

		internal Client(string connectionID, DateTimeOffset loginTime, DateTimeOffset lastActivityTime, string username, bool authenticated) =>
			(ConnectionID, LoginTime, LastActivityTime, Username, Authenticated) = (connectionID, loginTime, lastActivityTime, username, authenticated);
	}
}
