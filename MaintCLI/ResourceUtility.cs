using System.IO;
using System.Reflection;

namespace MaintCliNS
{
	class ResourceUtility
	{
		static Assembly _executingAssembly;

		static ResourceUtility() =>
			_executingAssembly = Assembly.GetExecutingAssembly();

		/// <summary>Gets an embedded resource from the currently executing assembly, and returns it as string.</summary>
		/// <param name="resourceName">The resource name.</param>
		/// <returns>The resource as text.</returns>
		public static string GetTextResource(string resourceName)
		{
			Stream stream = _executingAssembly.GetManifestResourceStream(resourceName);

			using (StreamReader reader = new StreamReader(stream))
			{
				return reader.ReadToEnd();
			}
		}
	}
}
