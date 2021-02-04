using System.Collections.Immutable;
using System.Drawing;
using System.IO;
using System.Linq;

// In this partial class, we maintain the client-side settings (that the server can modify) along with their default values.
// Typically this means translating property changes to their CSS equivalents, and sending them to the client as they are updated.

namespace MaintCliNS
{
	/// <summary>
	/// Provides a command-line interface to an application via HTTP.
	/// </summary>
	public partial class MaintCli
	{
		/// <summary>
		/// This method is called once upon connection, to send all of the settings to the client.
		/// </summary>
		internal void SendAllSettingsTo(IClientRpc client)
		{
			SendEnableTimeStamps(client);
			SendMessagesToRetain(client);
			SendColor(client);
			SendBackgroundColor(client);
			SendBackgroundRepeat(client);
			SendBackgroundStretch(client);
			SendFontSize(client);
			SendFontFamilies(client);
			SendTextShadowColor(client);
			SendBackgroundImage(client);
		}

		/* Client Settings/Properties Below */

		/// <summary>Displays a timestamp in front of each command in the client's command history.</summary>
		public bool EnableTimeStamps
		{
			get => _enableTimeStamps;
			set
			{
				_enableTimeStamps = value;
				SendEnableTimeStamps(GetAuthenticatedClientRpcInterfaces());
			}
		}
		bool _enableTimeStamps;
		void SendEnableTimeStamps(IClientRpc client) =>
			client.EnableTimeStamps(_enableTimeStamps);

		/// <summary>The number of messages to retain in the client's command history.</summary>
		public int MessagesToRetain
		{
			get => _messagesToRetain;
			set
			{
				_messagesToRetain = value;
				SendMessagesToRetain(GetAuthenticatedClientRpcInterfaces());
			}
		}
		int _messagesToRetain = 1_000;
		void SendMessagesToRetain(IClientRpc client) =>
			client.MessagesToRetain(_messagesToRetain);

		/// <summary>The color of the text displayed on the CLI.</summary>
		public Color Color
		{
			get => _color;
			set
			{
				_color = value;
				SendColor(GetAuthenticatedClientRpcInterfaces());
			}
		}
		Color _color = Color.FromArgb(255, 211, 211, 211);
		void SendColor(IClientRpc client) =>
			client.Color($"rgba({_color.R}, {_color.G}, {_color.B}, {(double)_color.A / 255.0})");

		/// <summary>The background color of the CLI.</summary>
		public Color BackgroundColor
		{
			get => _backgroundColor;
			set
			{
				_backgroundColor = value;

				if(_backgroundColor != Color.Transparent)
				{
					_backgroundGradientColors = ImmutableList<Color>.Empty;
					_backgroundImageUrl = null;
					_backgroundImageEmbedded = null;
				}

				SendBackgroundColor(GetAuthenticatedClientRpcInterfaces());
				SendBackgroundImage(GetAuthenticatedClientRpcInterfaces());
			}
		}
		Color _backgroundColor = Color.Transparent;
		void SendBackgroundColor(IClientRpc client) =>
			client.BackgroundColor($"rgba({_backgroundColor.R}, {_backgroundColor.G}, {_backgroundColor.B}, {(double)_backgroundColor.A / 255.0})");

		/// <summary>The background of the CLI as a list of gradient colors.</summary>
		public ImmutableList<Color> BackgroundGradientColors
		{
			get => _backgroundGradientColors;
			set
			{
				_backgroundGradientColors = value;

				if (_backgroundGradientColors != null && _backgroundGradientColors.Count > 0)
				{
					_backgroundImageUrl = null;
					_backgroundImageEmbedded = null;
					_backgroundColor = Color.Transparent;
				}

				SendBackgroundColor(GetAuthenticatedClientRpcInterfaces());
				SendBackgroundImage(GetAuthenticatedClientRpcInterfaces());
			}
		}
		ImmutableList<Color> _backgroundGradientColors = ImmutableList<Color>.Empty.Add(Color.FromArgb(255, 0, 0, 0)).Add(Color.FromArgb(255, 40, 40, 40));

		/// <summary>The direction, in degrees, of the background gradient of the CLI (if one is specified.)</summary>
		public int BackgroundGradientDirection
		{
			get => _backgroundGradientDirection;
			set
			{
				_backgroundGradientDirection = value;

				SendBackgroundImage(GetAuthenticatedClientRpcInterfaces());
			}
		}
		int _backgroundGradientDirection = 45;

		/// <summary>A URL to display as the background image of the CLI.</summary>
		public string BackgroundImageUrl
		{
			get => _backgroundImageUrl;
			set
			{
				_backgroundImageUrl = value;

				if (_backgroundImageUrl != null)
				{
					_backgroundGradientColors = ImmutableList<Color>.Empty;
					_backgroundImageEmbedded = null;
					_backgroundColor = Color.Transparent;
				}

				SendBackgroundColor(GetAuthenticatedClientRpcInterfaces());
				SendBackgroundImage(GetAuthenticatedClientRpcInterfaces());
			}
		}
		string _backgroundImageUrl = null;

		/// <summary>The path of a local (to the application) image to display as the background image of the CLI.</summary>
		public string BackgroundImageFile
		{
			get => _backgroundImageFile;
			set
			{
				_backgroundImageFile = value;

				if (_backgroundImageFile == null)
				{
					_backgroundImageEmbedded = null;
				}
				else
				{
					var bytes = File.ReadAllBytes(value);
					var base64 = System.Convert.ToBase64String(bytes);
					_backgroundImageEmbedded = $"url(data:image/{new FileInfo(value).Extension.TrimStart('.')};base64,{base64})";

					_backgroundColor = Color.Transparent;
					_backgroundGradientColors = ImmutableList<Color>.Empty;
					_backgroundImageUrl = null;
				}

				SendBackgroundColor(GetAuthenticatedClientRpcInterfaces());
				SendBackgroundImage(GetAuthenticatedClientRpcInterfaces());
			}
		}
		string _backgroundImageEmbedded = null;
		string _backgroundImageFile = null;

		void SendBackgroundImage(IClientRpc client)
		{
			if (_backgroundImageUrl != null)
			{
				client.BackgroundImage($"url(\"{_backgroundImageUrl}\")");
			}
			else if (_backgroundImageEmbedded != null)
			{
				client.BackgroundImage(_backgroundImageEmbedded);
			}
			else if (_backgroundGradientColors != null && _backgroundGradientColors.Count > 0)
			{
				client.BackgroundImage($"linear-gradient({BackgroundGradientDirection}deg, {string.Join(',', _backgroundGradientColors.Select(color => $"rgba({color.R}, {color.G}, {color.B}, {(double)color.A / 255.0})"))})");
			}
			else
			{
				client.BackgroundImage($"none");
			}
		}

		/// <summary>Tiles the specified image across the CLI.</summary>
		public bool BackgroundRepeat
		{
			get => _backgroundRepeat;
			set
			{
				_backgroundRepeat = value;
				SendBackgroundRepeat(GetAuthenticatedClientRpcInterfaces());
			}
		}
		bool _backgroundRepeat = false;
		void SendBackgroundRepeat(IClientRpc client) =>
			client.BackgroundRepeat(_backgroundRepeat ? "repeat" : "no-repeat");

		/// <summary>Stretches the specified image across the CLI.</summary>
		public bool BackgroundStretch
		{
			get => _backgroundStretch;
			set
			{
				_backgroundStretch = value;
				SendBackgroundStretch(GetAuthenticatedClientRpcInterfaces());
			}
		}
		bool _backgroundStretch = false;
		void SendBackgroundStretch(IClientRpc client) =>
			client.BackgroundSize(_backgroundStretch ? "cover" : "auto");

		/// <summary>The font size of the CLI text..</summary>
		public int FontSize
		{
			get => _fontSize;
			set
			{
				_fontSize = value;
				SendFontSize(GetAuthenticatedClientRpcInterfaces());
			}
		}
		int _fontSize = 10;
		void SendFontSize(IClientRpc client) =>
			client.FontSize($"{_fontSize}pt");

		/// <summary>A list of the desired font families for the CLI text, in order of preference.</summary>
		public ImmutableList<string> FontFamilies
		{
			get => _fontFamilies;
			set
			{
				_fontFamilies = value;
				SendFontFamilies(GetAuthenticatedClientRpcInterfaces());
			}
		}
		ImmutableList<string> _fontFamilies = ImmutableList<string>.Empty.Add("Consolas").Add("Monaco");
		void SendFontFamilies(IClientRpc client) =>
			client.FontFamily($"{string.Join(',', _fontFamilies.Select(fam => $"\"{fam}\""))}, monospace");

		/// <summary>Sets the color of the drop shadow for the CLI text.</summary>
		public Color TextShadowColor
		{
			get => _textShadowColor;
			set
			{
				_textShadowColor = value;
				SendTextShadowColor(GetAuthenticatedClientRpcInterfaces());
			}
		}
		Color _textShadowColor = Color.Black;
		void SendTextShadowColor(IClientRpc client) =>
			client.TextShadow($"1px 1px 0px rgba({_textShadowColor.R}, {_textShadowColor.G}, {_textShadowColor.B}, {(double)_textShadowColor.A / 255.0})");
	}
}
