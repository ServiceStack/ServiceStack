using System;
using System.Collections.Generic;

namespace ServiceStack.ServiceInterface.Session
{
	public interface IUserSessionManager
	{
		/// <summary>
		/// Removes the client session.
		/// </summary>
		/// <param name="userId">The user global id.</param>
		/// <param name="clientSessionIds">The client session ids.</param>
		void RemoveClientSession(long userId, ICollection<Guid> clientSessionIds);

		/// <summary>
		/// Adds a new client session.
		/// </summary>
		/// <param name="userId">The user global id.</param>
		/// <param name="userName">Name of the user.</param>
		/// <param name="ipAddress">The ip address.</param>
		/// <param name="base64ClientModulus">The base64 client modulus.</param>
		/// <param name="userClientGlobalId">The user client global id.</param>
		/// <returns></returns>
		UserClientSessionsTuple AddClientSession(long userId, string userName, string ipAddress, string base64ClientModulus, Guid userClientGlobalId);

		/// <summary>
		/// Updates the UserSession in the cache, or removes expired ones.
		/// </summary>
		/// <param name="userSession">The user session.</param>
		void UpdateUserSession(UserSession userSession);

		/// <summary>
		/// Adds the user session to the cache.
		/// </summary>
		/// <param name="userSession">The user session.</param>
		void AddUserSession(UserSession userSession);

		/// <summary>
		/// Gets the user session if it exists or null.
		/// </summary>
		/// <param name="userId">The user global id.</param>
		/// <returns></returns>
		UserSession GetUserSession(long userId);

		/// <summary>
		/// Gets or create a user session if one doesn't exist.
		/// </summary>
		/// <param name="userId">The user global id.</param>
		/// <param name="userName">Name of the user.</param>
		/// <returns></returns>
		UserSession GetOrCreateSession(long userId, string userName);

		/// <summary>
		/// Gets the user client session identified by the id if exists otherwise null.
		/// </summary>
		/// <param name="userId">The user global id.</param>
		/// <param name="clientSessionId">The client session id.</param>
		/// <returns></returns>
		UserClientSession GetUserClientSession(long userId, Guid clientSessionId);

		/// <summary>
		/// Gets the user secure client session if it exists, otherwise null.
		/// </summary>
		/// <param name="userId">The user global id.</param>
		/// <param name="clientSessionId">The client session id.</param>
		/// <returns></returns>
		UserClientSession GetUserSecureClientSession(long userId, Guid clientSessionId);
	}
}