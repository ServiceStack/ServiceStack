using System;
using System.Collections.Generic;
using ServiceStack.CacheAccess;
using ServiceStack.Common;

namespace ServiceStack.ServiceInterface.Session
{
	/// <summary>
	/// Manages all the User Sessions
	/// </summary>
	public class UserSessionManager : IUserSessionManager
	{
		/// <summary>
		/// Google/Yahoo seems to make you to login every 2 weeks??
		/// </summary>
		private readonly ICacheClient cacheClient;

		public UserSessionManager(ICacheClient cacheClient)
		{
			this.cacheClient = cacheClient;
		}

		/// <summary>
		/// Removes the client session.
		/// </summary>
		/// <param name="userId">The user global id.</param>
		/// <param name="clientSessionIds">The client session ids.</param>
		public void RemoveClientSession(long userId, ICollection<Guid> clientSessionIds)
		{
			var userSession = GetUserSession(userId);
			if (userSession == null) return;

			foreach (var clientSessionId in clientSessionIds)
			{
				userSession.RemoveClientSession(clientSessionId);
			}
			UpdateUserSession(userSession);
		}

		/// <summary>
		/// Adds a new client session.
		/// </summary>
		/// <param name="userId">The user global id.</param>
		/// <param name="userName">Name of the user.</param>
		/// <param name="ipAddress">The ip address.</param>
		/// <param name="base64ClientModulus">The base64 client modulus.</param>
		/// <param name="userClientGlobalId">The user client global id.</param>
		/// <returns></returns>
		public UserClientSessionsTuple AddClientSession(long userId, string userName, string ipAddress, string base64ClientModulus, Guid userClientGlobalId)
		{
			var userSession = GetOrCreateSession(userId, userName);
			var clientSessions = userSession.CreateNewClientSessions(ipAddress, base64ClientModulus, userClientGlobalId);
			UpdateUserSession(userSession);
			return clientSessions;
		}

		/// <summary>
		/// Updates the UserSession in the cache, or removes expired ones.
		/// </summary>
		/// <param name="userSession">The user session.</param>
		public void UpdateUserSession(UserSession userSession)
		{
			var hasSessionExpired = userSession.HasExpired();
			var cacheKey = UrnId.Create(userSession.GetType(), userSession.UserId.ToString());
			if (hasSessionExpired)
			{
				this.cacheClient.Remove(cacheKey);
			}
			else
			{
				this.cacheClient.Replace(cacheKey, userSession, userSession.ExpiryDate);
			}
		}

		/// <summary>
		/// Adds the user session to the cache.
		/// </summary>
		/// <param name="userSession">The user session.</param>
		public void AddUserSession(UserSession userSession)
		{
			var cacheKey = UrnId.Create(userSession.GetType(), userSession.UserId.ToString());
			this.cacheClient.Add(cacheKey, userSession, userSession.ExpiryDate);
		}

		/// <summary>
		/// Gets the user session if it exists or null.
		/// </summary>
		/// <param name="userId">The user global id.</param>
		/// <returns></returns>
		public UserSession GetUserSession(long userId)
		{
			var cacheKey = UrnId.Create(typeof(UserSession), userId.ToString());
			return this.cacheClient.Get<UserSession>(cacheKey);
		}

		/// <summary>
		/// Gets or create a user session if one doesn't exist.
		/// </summary>
		/// <param name="userId">The user global id.</param>
		/// <param name="userName">Name of the user.</param>
		/// <returns></returns>
		public UserSession GetOrCreateSession(long userId, string userName)
		{
			var userSession = GetUserSession(userId);
			if (userSession == null)
			{
				userSession = new UserSession(userId, userName);
				AddUserSession(userSession);
			}
			return userSession;
		}

		/// <summary>
		/// Gets the user client session identified by the id if exists otherwise null.
		/// </summary>
		/// <param name="userId">The user global id.</param>
		/// <param name="clientSessionId">The client session id.</param>
		/// <returns></returns>
		public UserClientSession GetUserClientSession(long userId, Guid clientSessionId)
		{
			var userSession = GetUserSession(userId);
			return userSession != null ? userSession.GetClientSession(clientSessionId) : null;
		}

		/// <summary>
		/// Gets the user secure client session if it exists, otherwise null.
		/// </summary>
		/// <param name="userId">The user global id.</param>
		/// <param name="clientSessionId">The client session id.</param>
		/// <returns></returns>
		public UserClientSession GetUserSecureClientSession(long userId, Guid clientSessionId)
		{
			var userSession = GetUserSession(userId);
			if (userSession != null)
			{
				if (userSession.SecureClientSessions.ContainsKey(clientSessionId))
				{
					return userSession.SecureClientSessions[clientSessionId];
				}
			}
			return null;
		}
	}
}