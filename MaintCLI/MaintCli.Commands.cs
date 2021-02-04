using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MaintCliNS
{
	/// <summary>
	/// Provides a command-line interface to an application via HTTP.
	/// </summary>
	public partial class MaintCli
	{
		ConcurrentDictionary<string, Command> _commands = new ConcurrentDictionary<string, Command>();

		/// <summary>Adds a new command to the CLI.</summary>
		public void AddCommand(Command command)
		{
			if (_commands.TryAdd(command.Name.ToLowerInvariant(), command) == false)
				throw new InvalidOperationException($"A command named '{command.Name}' already exists.");
		}

		internal void RunCommand(Client sender, string line)
		{
			var tokens = Tokenize(line).ToArray();

			// If nothing entered, assume they want the "help" command.
			if (tokens.Length == 0)
				tokens = new string[] { "help" };

			if (_commands.TryGetValue(tokens[0].ToLowerInvariant(), out var command) == false)
				throw new ArgumentException($"Unknown command '{tokens[0]}'. Type 'help' or '' (blank) to list the available commands.");

			// Get the arguments by excluding the first token (the command name)
			tokens = tokens[1..];

			var commandArguments = new CommandArguments();

			var argIndex = 0;

			// Match up the tokens with the specified arguments/options for this command.
			for (int tokenIndex = 0; tokenIndex < tokens.Length; tokenIndex++)
			{
				var token = tokens[tokenIndex];

				bool escapedArgument = false;
				if (token.StartsWith("--"))
				{
					token = token.Substring(1);
					escapedArgument = true;
				}

				if (token.StartsWith('-') && !escapedArgument)
				{
					// Token is an option

					// Strip leading dashes
					token = token.TrimStart('-');

					string name = token;
					string argument = null;

					// Is this the magic "help" option? If so, display help instead of running the command.
					if (name.ToLowerInvariant() == "help")
					{
						SendMessageTo(sender, command.ToString());
						return;
					}

					// Verify that this is a valid option for this command
					if (command.Options.TryGetValue(name.ToLowerInvariant(), out var option) == false)
						throw new ArgumentException($"Unknown option '{token}' specified. Type '{command.Name} -help' for more information.");

					if (option.HasArgument)
					{
						// The next token should be the argument for this option
						tokenIndex++;

						if (tokenIndex >= tokens.Length)
							throw new ArgumentException($"Option '{token}' did not specify an argument. Type '{command.Name} -help' for more information.");

						argument = tokens[tokenIndex];

						escapedArgument = false;
						if (argument.StartsWith("--"))
						{
							argument = token.Substring(1);
							escapedArgument = true;
						}

						if (argument.StartsWith('-') && !escapedArgument)
							throw new ArgumentException($"Option '{token}' did not specify an argument. Type '{command.Name} -help' for more information.");

						argument = argument.Trim('\"', '\'');
					}

					// Store this option for lookup by the user
					commandArguments.Add(option.Parameter.Name, argument);
				}
				else if (argIndex < command.Arguments.Count)
				{
					// Token is an argument, since there are arguments left to specify

					// Store this argument for lookup by the user
					commandArguments.Add(command.Arguments[argIndex].Name, token);

					argIndex++;
				}
				else
				{
					if (command.Options.ContainsKey(token.ToLowerInvariant()))
						throw new ArgumentException($"Unexpected token '{token}'. Did you forget to prefix an option with '-'? Type '{command.Name} -help' for more information.");
					else
						throw new ArgumentException($"Unexpected token '{token}'. Type '{command.Name} -help' for more information.");
				}
			}

			// Did the user send in all of the required arguments?
			if (argIndex < command.Arguments.Count)
				throw new ArgumentException($"Missing argument '{command.Arguments[argIndex].Name}'. Type '{command.Name} -help' for more information.");

			// Run the user code
			command.Action?.Invoke(sender, commandArguments);
		}

		/// <summary>
		/// Returns a formatted string that lists the available commands.
		/// </summary>
		/// <returns></returns>
		internal string ListCommands()
		{
			var builder = new StringBuilder();

			builder.AppendLine("Commands:");

			foreach (var command in _commands
				.OrderBy(kvp => kvp.Key)
				.Select(kvp => kvp.Value))
			{
				builder.Append($"\t{command.Name}");
				if (command.Help != null)
					builder.Append($" - {command.Help}");
				builder.AppendLine();
			}

			return builder.ToString();
		}

		IEnumerable<string> Tokenize(string line)
		{
			if (line == null)
				yield break;

			char lookingForEndQuote = '\0';
			int start = 0;
			int i = 0;
			for (; i < line.Length; i++)
			{
				if (lookingForEndQuote != '\0')
				{
					if (line[i] == lookingForEndQuote)
					{
						yield return line.Substring(start, i - start);
						start = i + 1;
						lookingForEndQuote = '\0';
					}
				}
				else if (line[i] == '\'' || line[i] == '\"')
				{
					lookingForEndQuote = line[i];
					start = i + 1;
				}
				else if (char.IsWhiteSpace(line[i]))
				{
					if (i == start)
					{
						start = i + 1;
					}
					else
					{
						yield return line.Substring(start, i - start).TrimStart();
						start = i + 1;
					}
				}
			}

			if (start < i)
				if (lookingForEndQuote != '\0')
					yield return line.Substring(start, i - start);
				else
					yield return line.Substring(start, i - start).TrimStart();
		}
	}
}
