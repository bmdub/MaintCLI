using System.Text.RegularExpressions;
using System.Web;

namespace MaintCliNS
{
	static class HtmlUtility
	{
		static Regex _crlfRegex;

		static HtmlUtility() =>
			_crlfRegex = new Regex(@"\r?\n", RegexOptions.Compiled);

		public static string HtmlEncode(string message)
		{
			// Escape standard HTML characters
			message = HttpUtility.HtmlEncode(message);

			// Replace newlines with HTML <br/>
			message = _crlfRegex.Replace(message, "<br/>");

			// Replace tabs with 4 spaces.
			message = message.Replace("\t", "&nbsp;&nbsp;&nbsp;&nbsp;");

			return message;
		}
	}
}
