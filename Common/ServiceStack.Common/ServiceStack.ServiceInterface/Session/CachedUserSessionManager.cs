/*
// $Id: CachedUserSessionManager.cs 13365 2010-03-08 18:59:26Z DDNGLOBAL\Demis $
//
// Revision      : $Revision: 13365 $
// Modified Date : $LastChangedDate: 2010-03-08 18:59:26 +0000 (Mon, 08 Mar 2010) $
// Modified By   : $LastChangedBy: DDNGLOBAL\Demis $
//
// (c) Copyright 2010 Liquidbit Ltd
*/

using System;
using System.Collections.Generic;
using System.Diagnostics;
using ServiceStack.CacheAccess;
using ServiceStack.Logging;

namespace ServiceStack.ServiceInterface.Session
{
	/// <summary>
	/// Manages all the User Sessions into the ICacheClient provided
	/// </summary>
	public class CachedUserSessionManager
		: IUserSessionManager
	{
		private static readonly ILog Log = LogManager.GetLogger(typeof(CachedUserSessionManager));

		private readonly ICacheClient cacheClient;

		/// <summary>
		/// Big perf hit if we Log on every session change
		/// </summary>
		/// <param name="fmt">The FMT.</param>
		/// <param name="args">The args.</param>
		[Conditional("DEBUG")]
		protected void LogIfDebug(string fmt, params object[] args)
		{
			if (args.Length > 0)
				Log.DebugFormat(fmt, args);
			else
				Log.Debug(fmt);
		}

		public CachedUserSessionManager(ICacheClient cacheClient)
		{
			this.cacheClient = cacheClient;
		}

		/// <summary>
		/// Removes the client session.
		/// </summary>
		/// <param name="userId">The user global id.</param>
		/// <param name="clientSessionIds">The client session ids.</param>
		public void RemoveClientSession(Guid userId, ICollection<Guid> clientSessionIds)
		{
			var userSession = this.GetUserSession(userId);
			if (userSession == null) return;

			foreach (var clientSessionId in clientSessionIds)
			{
				userSession.RemoveClientSession(clientSessionId);
			}
			this.UpdateUserSession(userSession);
		}

		/// <summary>
		/// Adds a new client session.
		/// Should this be changed to GetOrCreateClientSession?
		/// </summary>
		/// <param name="userId">The user global id.</param>
		/// <param name="userName">Title of the user.</param>
		/// <param name="shardId">The shard id.</param>
		/// <param name="ipAddress">The ip address.</param>
		/// <param name="userAgent"></param>
		/// <param name="base64ClientModulus">The base64 client modulus.</param>
		/// <param name="userClientGlobalId">The user client global id.</param>
		/// <returns></returns>
		public UserClientSession StoreClientSession(Guid userId, string userName, string shardId, string ipAddress, string userAgent, string base64ClientModulus, Guid userClientGlobalId)
		{
			var userSession = this.GetOrCreateSession(userId, userName, shardId);

			var existingClientSession = userSession.GetClientSessionWithClientId(userClientGlobalId);
			if (existingClientSession != null)
			{
				userSession.RemoveClientSession(existingClientSession.Id);
			}

			var newClientSession = userSession.CreateNewClientSession(
				ipAddress, userAgent, base64ClientModulus, userClientGlobalId);

			this.UpdateUserSession(userSession);

			return newClientSession;
		}

		/// <summary>
		/// Updates the UserSession in the cache, or removes expired ones.
		/// </summary>
		/// <param name="userSession">The user session.</param>
		public void UpdateUserSession(UserSession userSession)
		{
			var hasSessionExpired = userSession.HasExpired();
			if (hasSessionExpired)
			{
				LogIfDebug("Session has expired, removing: " + userSession.ToCacheKey());
				this.cacheClient.Remove(userSession.ToCacheKey());
			}
			else
			{
				LogIfDebug("Updating session: " + userSession.ToCacheKey());
				this.cacheClient.Replace(userSession.ToCacheKey(), userSession, userSession.ExpiryDate.Value);
			}
		}

		/// <summary>
		/// Gets the user session if it exists or null.
		/// </summary>
		/// <param name="userId">The user global id.</param>
		/// <returns></returns>
		public UserSession GetUserSession(Guid userId)
		{
			var cacheKey = UserSession.ToCacheKey(userId);
			return this.cacheClient.Get<UserSession>(cacheKey);
		}

		/// <summary>
		/// Gets or create a user session if one doesn't exist.
		/// </summary>
		/// <param name="userId">The user global id.</param>
		/// <param name="userName">Title of the user.</param>
		/// <param name="shardId"></param>
		/// <returns></returns>
		public UserSession GetOrCreateSession(Guid userId, string userName, string shardId)
		{
			var userSession = this.GetUserSession(userId);
			if (userSession == null)
			{
				userSession = new UserSession(userId, userName, shardId);

				this.cacheClient.Add(userSession.ToCacheKey(), userSession,
				                     userSession.ExpiryDate.GetValueOrDefault(DateTime.UtcNow) + TimeSpan.FromHours(1));
			}
			return userSession;
		}

		/// <summary>
		/// Gets the user client session identified by the id if exists otherwise null.
		/// </summary>
		/// <param name="userId">The user global id.</param>
		/// <param name="clientSessionId">The client session id.</param>
		/// <returns></returns>
		public UserClientSession GetUserClientSession(Guid userId, Guid clientSessionId)
		{
			var userSession = this.GetUserSession(userId);
			return userSession != null ? userSession.GetClientSession(clientSessionId) : null;
		}
	}
}