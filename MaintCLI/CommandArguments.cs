using System.Collections.Generic;

namespace MaintCliNS
{
	/// <summary>
	/// Contains all of the user's input arguments to a command.
	/// </summary>
	public class CommandArguments
	{
		Dictionary<string, string> _argumentsByName = new Dictionary<string, string>();

		internal CommandArguments() { }

		/// <summary>Gets the input argument or option value by name.</summary>
		/// <param name="name">The name of the argument or option (without no leading dash).</param>
		/// <returns>The string value of the argument, or null if there is no value.</returns>
		public string this[string name] =>
			_argumentsByName[name.ToLowerInvariant()];

		/// <summary>Adds an argument+value to the collection of input arguments.</summary>
		/// <param name="name">The name of the argument or option (without no leading dash).</param>
		/// <param name="value">The value of the argument, or null.</param>
		public void Add(string name, string value = null) =>
			_argumentsByName[name.ToLowerInvariant()] = value;

		/// <summary>Gets the input argument or option value by name.</summary>
		/// <param name="name">The name of the argument or option (without no leading dash).</param>
		/// <param name="value">The returned value of the argument, if any.</param>
		/// <returns>True if the argument was entered.</returns>
		public bool TryGetValue(string name, out string value) =>
			_argumentsByName.TryGetValue(name.ToLowerInvariant(), out value);

		/// <summary>Returns whether or not the specified argument or option was specified.</summary>
		/// <param name="name">The name of the argument or option (without no leading dash).</param>
		/// <returns>True if the argument was entered.</returns>
		public bool Contains(string name) =>
			_argumentsByName.ContainsKey(name.ToLowerInvariant());
	}
}
