using System;
using System.Collections.Immutable;
using System.Linq;

namespace MaintCliNS
{
	/// <summary>
	/// Builds a command that is accessible via the CLI and served by the application.
	/// </summary>
	public class CommandBuilder
	{
		/// <summary>The name of the command, as typed by the user.</summary>
		public readonly string Name;
		/// <summary>Describes what the command does.</summary>
		public readonly string Help;
		/// <summary>Provides an example of how the command can be run.</summary>
		public readonly string Example;

		internal ImmutableList<CommandParameter> Arguments { get; private set; } = ImmutableList<CommandParameter>.Empty;
		internal ImmutableDictionary<string, (bool HasArgument, CommandParameter Parameter)> Options { get; private set; } = ImmutableDictionary<string, (bool HasArgument, CommandParameter Parameter)>.Empty;
		internal Action<Client, CommandArguments> Action { get; private set; }

		/// <summary>
		/// Builds a command that is accessible via the CLI and served by the application.
		/// </summary>
		/// <param name="name">The name of the argument, which can later be used by the application to retrieve the argument.</param>
		/// <param name="help">Describes what the argument is used for.</param>
		/// <param name="example">Provides an example of how the command can be run.</param>
		public CommandBuilder(string name, string help = null, string example = null) =>
			(Name, Help, Example) = (name, help, example);

		/// <summary>Adds an optional argument to the command.</summary>
		/// <param name="name">The name of the argument, which can later be used by the application to retrieve the argument.</param>
		/// <param name="hasArgument">True if this option requires an argument.</param>
		/// <param name="help">Describes what the argument is used for.</param>
		/// <returns>The same modified instance of the command object.</returns>
		public CommandBuilder AddOption(string name, bool hasArgument, string help = null)
		{
			name = name.TrimStart('-');

			if (Options.ContainsKey(name.ToLowerInvariant()))
				throw new InvalidOperationException("An option with this name has already been specified.");
			if (Arguments.Any(arg => string.Equals(arg.Name, name, StringComparison.InvariantCultureIgnoreCase)))
				throw new InvalidOperationException("An argument with this name has already been specified.");

			Options = Options.Add(name, (hasArgument, new CommandParameter(name, help)));
			return this;
		}

		/// <summary>Adds a required argument to the command.</summary>
		/// <param name="name">The name of the argument, which can later be used by the application to retrieve the argument.</param>
		/// <param name="help">Describes what the argument is used for.</param>
		/// <returns>The same modified instance of the command object.</returns>
		public CommandBuilder AddArgument(string name, string help = null)
		{
			if (Options.ContainsKey(name.ToLowerInvariant()))
				throw new InvalidOperationException("An option with this name has already been specified.");
			if (Arguments.Any(arg => string.Equals(arg.Name, name, StringComparison.InvariantCultureIgnoreCase)))
				throw new InvalidOperationException("An argument with this name has already been specified.");

			Arguments = Arguments.Add(new CommandParameter(name, help));
			return this;
		}

		/// <summary>Defines the action to execute to service the command after it is sent by a user.</summary>
		/// <param name="action">The action delegate to execute, given a snapshot of the client state and the entered arguments.</param>
		/// <returns>The same modified instance of the command object.</returns>
		public CommandBuilder Execute(Action<Client, CommandArguments> action)
		{
			if (Action != null)
				throw new InvalidOperationException("An action delegate has already been specified.");

			Action = action;

			return this;
		}

		/// <summary>Builds a finalized command.</summary>
		/// <returns>The command.</returns>
		public Command Build() =>
			 new Command(Name, Help, Example, Arguments, Options, Action);
	}
}
