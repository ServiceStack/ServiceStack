/*
// $Id: UserSession.cs 13369 2010-03-08 19:09:44Z DDNGLOBAL\Demis $
//
// Revision      : $Revision: 13369 $
// Modified Date : $LastChangedDate: 2010-03-08 19:09:44 +0000 (Mon, 08 Mar 2010) $
// Modified By   : $LastChangedBy: DDNGLOBAL\Demis $
//
// (c) Copyright 2010 Liquidbit Ltd
*/

using System;
using System.Collections.Generic;
using ServiceStack.Common;

namespace ServiceStack.ServiceInterface.Session
{
	/// <summary>
	/// Holds all the data required for a User Session
	/// </summary>
	public class UserSession
	{
		//Empty constructor required for TypeSerializer
		public UserSession()
		{
			this.PublicClientSessions = new Dictionary<Guid, UserClientSession>();
		}

		public Guid UserId { get; private set; }

		public string UserName { get; private set; }

		public string ShardId { get; private set; }

		public Dictionary<Guid, UserClientSession> PublicClientSessions { get; private set; }

		public UserSession(Guid userId, string userName, string shardId)
			: this()
		{
			this.UserId = userId;
			this.UserName = userName;
			this.ShardId = shardId;
		}

		/// <summary>
		/// Gets the max expiry date of all the users client sessions.
		/// If the user has no more active client sessions we can remove them from the cache.
		/// </summary>
		/// <value>The expiry date.</value>
		public DateTime? ExpiryDate
		{
			get
			{
				DateTime? maxExpiryDate = null;

				foreach (var session in this.PublicClientSessions.Values)
				{
					if (maxExpiryDate == null || session.ExpiryDate > maxExpiryDate)
					{
						maxExpiryDate = session.ExpiryDate;
					}
				}
				return maxExpiryDate;
			}
		}

		/// <summary>
		/// Creates a new client session for the user.
		/// </summary>
		/// <param name="ipAddress">The ip address.</param>
		/// <param name="userAgent">The user agent.</param>
		/// <param name="base64ClientModulus">The base64 client modulus.</param>
		/// <param name="userClientGlobalId">The user client global id.</param>
		/// <returns></returns>
		public UserClientSession CreateNewClientSession(string ipAddress, string userAgent, string base64ClientModulus, Guid userClientGlobalId)
		{
			return this.CreateClientSession(Guid.NewGuid(), ipAddress, userAgent, base64ClientModulus, userClientGlobalId);
		}

		public UserClientSession CreateClientSession(Guid sessionId, string ipAddress, string userAgent, string base64ClientModulus, Guid userClientGlobalId)
		{
			var clientSession = new UserClientSession(
				sessionId, this.UserId, ipAddress, userAgent, base64ClientModulus, userClientGlobalId);

			this.PublicClientSessions[clientSession.Id] = clientSession;

			return clientSession;
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
		}

		public UserClientSession GetClientSessionWithClientId(Guid userClientId)
		{
			foreach (var entry in this.PublicClientSessions)
			{
				if (entry.Value.UserClientGlobalId == userClientId)
				{
					return entry.Value;
				}
			}

			return null;
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

			//If there are no more active client sessions we can remove the entire UserSession
			var sessionHasExpired = 
				this.ExpiryDate == null							//There are no UserClientSessions
				|| this.ExpiryDate.Value <= DateTime.UtcNow;	//The max UserClientSession ExpiryDate has expired

			return sessionHasExpired;
		}

		private static void RemoveExpiredSessions(IDictionary<Guid, UserClientSession> clientSessions)
		{
			var expiredSessionKeys = new List<Guid>();

			foreach (var clientSession in clientSessions)
			{
				if (clientSession.Value.ExpiryDate < DateTime.UtcNow)
				{
					expiredSessionKeys.Add(clientSession.Key);
				}
			}

			foreach (var sessionKey in expiredSessionKeys)
			{
				clientSessions.Remove(sessionKey);
			}
		}

		public void RemoveAllSessions()
		{
			this.PublicClientSessions.Clear();
		}

		public UserClientSession GetClientSession(Guid clientSessionId)
		{
			UserClientSession session;

			return this.PublicClientSessions.TryGetValue(clientSessionId, out session) 
				? session : null;
		}

		public string ToCacheKey()
		{
			return ToCacheKey(this.UserId);
		}

		public static string ToCacheKey(Guid userId)
		{
			return UrnId.Create<UserSession>(userId.ToString());
		}
	}
}