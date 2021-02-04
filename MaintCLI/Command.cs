using System;
using System.Collections.Immutable;
using System.Text;

namespace MaintCliNS
{
	/// <summary>
	/// A command that is accessible via the CLI and served by the application.
	/// </summary>
	public class Command
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

		internal Command(string name, string help, string example, ImmutableList<CommandParameter> arguments, ImmutableDictionary<string, (bool HasArgument, CommandParameter Parameter)> options, Action<Client, CommandArguments> action) =>
			(Name, Help, Example, Arguments, Options, Action) = (name, help, example, arguments, options, action);

		/// <summary>Returns the command/help/example as a formatted string.</summary>
		public override string ToString()
		{
			var builder = new StringBuilder();

			builder.Append($"{Name}");
			if (Help != null)
				builder.Append($" - {Help}");
			builder.AppendLine();

			builder.Append($"\tusage: {Name}");
			if (Options.Count > 0)
				builder.Append($" [options]");
			foreach (var argument in Arguments)
				builder.Append($" <{argument.Name}>");
			builder.AppendLine();

			if (Example != null)
				builder.AppendLine($"\texample: {Example}");

			if (Arguments.Count > 0)
			{
				builder.AppendLine("\targuments:");
				foreach (var argument in Arguments)
				{
					builder.Append($"\t\t<{argument.Name}>");
					if (argument.Help != null)
						builder.Append($" - {argument.Help}");
					builder.AppendLine();
				}
			}

			if (Options.Count > 0)
			{
				builder.AppendLine("\toptions:");
				foreach (var option in Options.Values)
				{
					builder.Append($"\t\t-{option.Parameter.Name}");
					if (option.HasArgument)
						builder.Append($" <argument>");
					if (option.Parameter.Help != null)
						builder.Append($" - {option.Parameter.Help}");
					builder.AppendLine();
				}
			}

			return builder.ToString();
		}
	}
}
