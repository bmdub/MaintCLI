using System.ComponentModel;
using System.Threading.Tasks;

#pragma warning disable 1591

namespace MaintCliNS
{
	/// <summary>
	/// Defines the client-side SignalR methods, callable by the server.
	/// </summary>
	[EditorBrowsable(EditorBrowsableState.Never)]
	public interface IClientRpc
	{
		// Sends client-side settings to the client.
		Task EnableTimeStamps(bool value);
		Task MessagesToRetain(int value);
		Task Color(string value);
		Task BackgroundColor(string value);
		Task BackgroundRepeat(string value);
		Task BackgroundSize(string value);
		Task FontSize(string value);
		Task FontFamily(string value);
		Task TextShadow(string value);
		Task BackgroundImage(string value);

		// Sends a message to the client to be displayed.
		Task ServerMessage(string message);

		// Directs the client to perform authentication steps.
		Task PromptForUsername();
		Task PromptForPassword();
		Task Authenticated();

		// Misc. orders.
		Task Disconnect();
		Task ClearScreen();
	}
}

#pragma warning restore 1591