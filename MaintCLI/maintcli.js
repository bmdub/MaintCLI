"use strict";

// signalr.min.js can be found from:
// https://docs.microsoft.com/en-us/aspnet/core/signalr/javascript-client

// The magic constant below will be replaced at run time with the desired value, indicating whether the application requires authentication.
const authRequired = __REQUIRE_AUTHENTICATION;

// We want to keep track of the authentication state for the handshake.
const AuthStateEnum = Object.freeze({ "default": 1, "promptingForUsername": 2, "promptingForPassword": 3, "authenticated": 4 })
let authState = AuthStateEnum.default;

// Define some client-side settings defaults.
let enableTimeStamps = false;
let messagesToRetain = 1000; // The # of messages to retain in the browser / history element.

// Prepare the SignalR connection.
var connection;
let connecting = false;
let connected = false;

// Grab the page elements we need later.
var cliWindow = document.querySelector('.cliWindow');
var commandCell = cliWindow.querySelector(".commandCell");
var command = cliWindow.querySelector(".command");
var messageHistory = cliWindow.querySelector(".history");

// Prepare the command history list.
var commandHistory = new Array();
var commandHistoryIndex = 0;

// We will need this later to escape commands
function EscapeHtml(unsafe) {
	return unsafe
		.replace(/&/g, "&amp;")
		.replace(/</g, "&lt;")
		.replace(/>/g, "&gt;")
		.replace(/"/g, "&quot;")
		.replace(/'/g, "&#039;");
}

// Using this to add blank lines to the interface, so that the focus remains pushed to the bottom left corner.
function PadMessageHistory() {
	for (i = 0; i < 500; i++) {
		messageHistory.appendChild(document.createElement('br'));
	}
}
PadMessageHistory();

// On mouse click anywhere on the page, ensure the command input box remains focused.
document.body.addEventListener('mouseup', function (e) {

	// If they are making a selection, don't focus.
	if (document.getSelection().toString() !== "")
		return;

	command.focus();
});

// Wires up the command box events
function ListenToCommandEvents() {

	// Handles Enter/Return press on the command input box.
	command.addEventListener('keydown', function (e) {

		// If we are not currently connected, then ignore the command and try to reconnect.
		if (!connected && !connecting) {

			AddToHistory("Disconnected. Attempting reconnect...");
			ConnectToServer();
			return;
		}

		if (authState === AuthStateEnum.default) {
			// noop?
		}
		else if (authState === AuthStateEnum.promptingForUsername) {

			if (e.key === "Enter") {

				// Send the username to the server.
				connection.invoke("AuthenticateUsername", command.value).catch(function (err) {
					AddToHistory(err.toString());
				});

				// Clear the command input box.
				command.value = "";
			}
		}
		else if (authState === AuthStateEnum.promptingForPassword) {

			if (e.key === "Enter") {

				// Send the password to the server.
				connection.invoke("AuthenticatePassword", command.value).catch(function (err) {
					AddToHistory(err.toString());
				});

				// Clear the command input box.
				command.value = "";
			}
		}
		else if (authState === AuthStateEnum.authenticated) {

			if (e.key === "Enter") {

				// Add the entered command to the command history.
				if (command.value.length > 0) {
					commandHistory.push(command.value);
					if (commandHistory.length > 1000)
						commandHistory.pop();
				}

				// Add the entered command to the message history.
				AddToHistory(">" + EscapeHtml(command.value));

				// Send the command to the server.
				connection.invoke("Command", command.value).catch(function (err) {
					AddToHistory(err.toString());
				});

				// Clear the command input box.
				command.value = "";

				// Reset the command history pointer to the end.
				commandHistoryIndex = commandHistory.length;
			}
			else if (e.key === "ArrowUp") {

				// Get the previous entry in the command history
				if (commandHistoryIndex > 0)
					commandHistoryIndex--;

				if (commandHistoryIndex < commandHistory.length)
					command.value = commandHistory[commandHistoryIndex];
			}
			else if (e.key === "ArrowDown") {

				// Get the next entry in the command history
				if (commandHistoryIndex < commandHistory.length)
					commandHistoryIndex++;

				if (commandHistoryIndex === commandHistory.length)
					command.value = "";
				else
					command.value = commandHistory[commandHistoryIndex];
			}
		}

	}, false);
}

// Adds a message to the message history window.
function AddToHistory(message) {

	const messageDiv = document.createElement('div');

	if (enableTimeStamps) {
		// Define our time stamp format.
		// TODO: Find a way to make this configurable.
		let date = new Date();
		let options = {
			year: "2-digit",
			month: "2-digit",
			day: "2-digit",
			hour: "2-digit",
			minute: "2-digit",
			second: "2-digit",
		};

		var datePrefix = "[" + date.toLocaleTimeString([], options) + "] ";

		// Add the time stamp to each line in the message
		message = datePrefix + message.replaceAll("<br/>", "<br/>" + datePrefix);
	}

	//messageDiv.innerText = message
	messageDiv.innerHTML = message;
	messageHistory.appendChild(messageDiv);
	while (messageHistory.childElementCount > messagesToRetain)
		messageHistory.removeChild(messageHistory.firstChild);

	ScrollToBottom();
}

// Ensures that the user remains at the bottom of the page/command history, where the command input box is.
function ScrollToBottom() {
	const scrollingElement = (document.scrollingElement || document.body);
	scrollingElement.scrollTop = scrollingElement.scrollHeight;
}

function SetPromptType(value) {
	let style = command.getAttribute("style");

	commandCell.removeChild(command);
	command = document.createElement('input');
	command.setAttribute('class', 'command');
	command.setAttribute('spellcheck', 'false');
	command.setAttribute('autocapitalize', 'false');
	command.setAttribute('type', value);
	command.setAttribute("style", style);
	commandCell.appendChild(command);

	ListenToCommandEvents();

	command.focus();
}

function GetPromptType() {
	return command.getAttribute('type');
}

function SetAuthState(state) {
	authState = state;

	// Set the command input box type if needed, based on the current auth state.
	if (authState === AuthStateEnum.promptingForPassword) {
		if (GetPromptType() !== "password") {
			SetPromptType("password");
		}
	}
	else {
		if (GetPromptType() !== "text") {
			SetPromptType("text");
		}
	}

	if (authState === AuthStateEnum.authenticated) {
		AddToHistory("Authenticated.");
	}
	if (authState === AuthStateEnum.promptingForUsername) {
		AddToHistory("Username:");
	}
	else if (authState === AuthStateEnum.promptingForPassword) {
		AddToHistory("Password:");
	}
}

// Wire up the SignalR events
function ListenToSignalrEvents() {

	// Handle reception of client settings from the server application.
	connection.on("EnableTimeStamps", function (value) {
		enableTimeStamps = value;
	});
	connection.on("MessagesToRetain", function (value) {
		messagesToRetain = value;
	});
	connection.on("Color", function (value) {
		var elem = document.querySelectorAll('*');
		for (var i = 0; i < elem.length; i++) {
			elem[i].style.color = value;
		}
	});
	connection.on("BackgroundColor", function (value) {
		var elem = document.querySelectorAll('*');
		for (var i = 0; i < elem.length; i++) {
			elem[i].style.backgroundColor = value;
		}
	});
	connection.on("BackgroundRepeat", function (value) {
		document.body.style.backgroundRepeat = value;
	});
	connection.on("BackgroundSize", function (value) {
		document.body.style.backgroundSize = value;
	});
	connection.on("FontSize", function (value) {
		var elem = document.querySelectorAll('*');
		for (var i = 0; i < elem.length; i++) {
			elem[i].style.fontSize = value;
		}
	});
	connection.on("FontFamily", function (value) {
		var elem = document.querySelectorAll('*');
		for (var i = 0; i < elem.length; i++) {
			elem[i].style.fontFamily = value;
		}
	});
	connection.on("TextShadow", function (value) {
		var elem = document.querySelectorAll('*');
		for (var i = 0; i < elem.length; i++) {
			elem[i].style.textShadow = value;
		}
	});
	connection.on("BackgroundImage", function (value) {
		document.body.style.backgroundImage = value;
	});

	// Handles reception of messages from the server application.
	connection.on("ServerMessage", function (message) {
		AddToHistory(message);
	});

	// Handles reception of username prompt request
	connection.on("PromptForUsername", function (message) {
		SetAuthState(AuthStateEnum.promptingForUsername);
	});

	// Handles reception of password prompt request
	connection.on("PromptForPassword", function (message) {
		SetAuthState(AuthStateEnum.promptingForPassword);
	});

	// Handles authentication notification
	connection.on("Authenticated", function (message) {
		SetAuthState(AuthStateEnum.authenticated);
	});

	// Handles reception of a disconnect request
	connection.on("Disconnect", function (message) {
		connection.stop();
	});

	// Handles reception of a clear screen request
	connection.on("ClearScreen", function (message) {
		messageHistory.innerHTML = '';
		PadMessageHistory();
	});

	// Handles connection close.
	connection.onclose(e => {
		connected = false;
		SetAuthState(AuthStateEnum.default);
		AddToHistory("Connection closed.");
	});
}

// Connects to the server application.
function ConnectToServer() {

	connecting = true;

	connection = new signalR.HubConnectionBuilder().withUrl("/maintclihub").build();

	ListenToSignalrEvents();

	//connection.start({ transport: ['webSockets', 'foreverFrame', 'serverSentEvents', 'longPolling'] })
	connection.start().then(function () {
		connecting = false;
		connected = true;

		// With this, the user can view the debugging console to see errors, transports used, etc.
		// Default to always on for now.
		connection.logging = true;

		// Start the authentication handshake if needed
		if (authRequired) {
			connection.invoke("BeginAuthentication").catch(function (err) {
				AddToHistory(err.toString());
			});
		}
		else {
			authState = AuthStateEnum.authenticated;
		}
	}).catch(function (err) {
		connecting = false;
		AddToHistory(err.toString());
	});
}

ListenToCommandEvents();

// Attempt connection to the server.
ConnectToServer();

// Set the cursor on the command input box.
command.focus();


