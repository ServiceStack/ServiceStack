using System;
using System.Collections.Generic;
using ServiceStack.ServiceInterface.Session;

namespace ServiceStack.ServiceInterface.Session
{
	/// <summary>
	/// Holds all the data required for a User Session
	/// </summary>
	[Serializable]
	public class UserSession
	{
		public long UserId { get; private set; }
		public string UserName { get; private set; }
		public Dictionary<Guid, UserClientSession> PublicClientSessions { get; private set; }
		public Dictionary<Guid, UserClientSession> SecureClientSessions { get; private set; }

		public UserSession(long userId, string userName)
		{
			this.UserId = userId;
			this.UserName = userName;
			this.PublicClientSessions = new Dictionary<Guid, UserClientSession>();
			this.SecureClientSessions = new Dictionary<Guid, UserClientSession>();
		}

		/// <summary>
		/// Gets the max expiry date of all the users client sessions.
		/// If the user has no more active client sessions we can remove them from the cache.
		/// </summary>
		/// <value>The expiry date.</value>
		public DateTime ExpiryDate
		{
			get
			{
				var maxExpiryDate = DateTime.Now;
				foreach (var session in this.PublicClientSessions.Values)
				{
					if (session.ExpiryDate > maxExpiryDate)
					{
						maxExpiryDate = session.ExpiryDate;
					}
				}
				return maxExpiryDate;
			}
		}

		/// <summary>
		/// Creates two new client sessions for the user.
		/// An 'Public' one to be used when making 'clear text' requests
		/// And a 'Secure' one which should only be used on an encrypted channel
		/// </summary>
		/// <param name="ipAddress">The ip address.</param>
		/// <param name="base64ClientModulus">The base64 client modulus.</param>
		/// <returns></returns>
		public UserClientSessionsTuple CreateNewClientSessions(string ipAddress, string base64ClientModulus)
		{
			var clientSession = new UserClientSession(this, Guid.NewGuid(), ipAddress, base64ClientModulus);
			this.PublicClientSessions[clientSession.Id] = clientSession;

			var secureClientSession = new UserClientSession(this, Guid.NewGuid(), ipAddress, base64ClientModulus);
			this.SecureClientSessions[secureClientSession.Id] = secureClientSession;

			return new UserClientSessionsTuple(clientSession, secureClientSession);
		}

		/// <summary>
		/// Removes the client session.
		/// </summary>
		/// <param name="clientSessionId">The client session id.</param>
		public void RemoveClientSession(Guid clientSessionId)
		{
			if (this.PublicClientSessions.ContainsKey(clientSessionId))
			{
				this.PublicClientSessions.Remove(clientSessionId);
			}
			if (this.SecureClientSessions.ContainsKey(clientSessionId))
			{
				this.SecureClientSessions.Remove(clientSessionId);
			}
		}

		/// <summary>
		/// Verifies this UserSession, removing any expired sessions.
		/// Returns true to keep the UserSession in the cache.
		/// </summary>
		/// <returns>
		/// 	<c>true</c> if this session has any active client sessions; otherwise, <c>false</c>.
		/// </returns>
		public bool HasExpired()
		{
			RemoveExpiredSessions(this.PublicClientSessions);
			RemoveExpiredSessions(this.SecureClientSessions);

			//If there are no more active client sessions we can remove the entire UserSessions
			var sessionHasExpired = this.ExpiryDate <= DateTime.Now;
			return sessionHasExpired;
		}

		private static void RemoveExpiredSessions(IDictionary<Guid, UserClientSession> clientSessions)
		{
			var expiredSessionKeys = new List<Guid>();
			foreach (var clientSession in clientSessions)
			{
				if (clientSession.Value.ExpiryDate < DateTime.Now)
				{
					expiredSessionKeys.Add(clientSession.Key);
				}
			}
			foreach (var sessionKey in expiredSessionKeys)
			{
				clientSessions.Remove(sessionKey);
			}
		}

		public UserClientSession GetClientSession(Guid clientSessionId)
		{
			if (this.PublicClientSessions.ContainsKey(clientSessionId))
			{
				return this.PublicClientSessions[clientSessionId];
			}
			else
			{
				if (this.SecureClientSessions.ContainsKey(clientSessionId))
				{
					return this.SecureClientSessions[clientSessionId];
				}
			}
			return null;
		}
	}
}