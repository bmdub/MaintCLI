using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace MaintCliNS
{	
	/// <summary>
	/// This class is used to store client connections and manage authenticated sessions.
	/// </summary>
	class ClientCache
	{
		readonly TimeSpan _inactivityTimeout;
		readonly TimeSpan _sessionTimeout;
		readonly ConcurrentDictionary<string, Client> _clientsByConnectionId = new ConcurrentDictionary<string, Client>();

		public ClientCache() =>
			(_inactivityTimeout, _sessionTimeout) = (TimeSpan.MaxValue, TimeSpan.MaxValue);

		public ClientCache(TimeSpan inactivityTimeout, TimeSpan sessionTimeout) =>
			(_inactivityTimeout, _sessionTimeout) = (inactivityTimeout, sessionTimeout);

		public void Add(string connectionId) =>
			_clientsByConnectionId[connectionId] = new Client(connectionId, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, null, false);

		public void Remove(string connectionId) =>
			_clientsByConnectionId.TryRemove(connectionId, out _);

		public bool TryGetClient(string connectionId, out Client client) =>
			_clientsByConnectionId.TryGetValue(connectionId, out client);

		public IEnumerable<Client> GetClientsByUsername(string username)
		{
			foreach(var client in _clientsByConnectionId.Values)
			{
				if(client.Username == username)
				{
					yield return client;
				}
			}
		}

		public void MarkAsAuthenticated(string connectionId, string username)
		{
			if (_clientsByConnectionId.TryGetValue(connectionId, out var client) == false)
				return;

			_clientsByConnectionId[connectionId] = client with { Username = username, Authenticated = true, LastActivityTime = DateTimeOffset.UtcNow };
		}

		public void RegisterActivity(string connectionId)
		{
			if (_clientsByConnectionId.TryGetValue(connectionId, out var client) == false)
				return;

			_clientsByConnectionId[connectionId] = client with { LastActivityTime = DateTimeOffset.UtcNow };
		}

		public bool IsInactive(string connectionId)
		{
			if (_clientsByConnectionId.TryGetValue(connectionId, out var client) == false)
				return true;

			return DateTimeOffset.UtcNow - client.LastActivityTime >= _inactivityTimeout;
		}

		public bool IsSessionExpired(string connectionId)
		{
			if (_clientsByConnectionId.TryGetValue(connectionId, out var entry) == false)
				return true;

			return DateTimeOffset.UtcNow - entry.LoginTime >= _sessionTimeout;
		}

		public IEnumerable<Client> Clients => _clientsByConnectionId.Values;
	}
}
