using System;

namespace MaintCliNS
{
	/// <summary>
	/// Options for handling a sent message to the client's CLI.
	/// </summary>
	[Flags]
	public enum SendOptions
	{
		/// <summary></summary>
		None = 0b0000_0000_0000_0000,

		/// <summary>Instructs the CLI to not escape any HTML characters prior to display.</summary>
		AllowHtml = 0b0000_0000_0000_0001,
	}
}
