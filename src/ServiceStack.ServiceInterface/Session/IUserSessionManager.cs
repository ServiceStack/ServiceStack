/*
// $Id: IUserSessionManager.cs 13365 2010-03-08 18:59:26Z DDNGLOBAL\Demis $
//
// Revision      : $Revision: 13365 $
// Modified Date : $LastChangedDate: 2010-03-08 18:59:26 +0000 (Mon, 08 Mar 2010) $
// Modified By   : $LastChangedBy: DDNGLOBAL\Demis $
//
// (c) Copyright 2010 Liquidbit Ltd
*/

using System;
using System.Collections.Generic;

namespace ServiceStack.ServiceInterface.Session
{
	/// <summary>
	/// Manager Interface listing all the methods required to manage a users session.
	/// </summary>
	public interface IUserSessionManager
	{
		/// <summary>
		/// Removes the client session.
		/// </summary>
		/// <param name="userId">The user global id.</param>
		/// <param name="clientSessionIds">The client session ids.</param>
		void RemoveClientSession(
			Guid userId, 
			ICollection<Guid> clientSessionIds);

		/// <summary>
		/// Adds a new client session.
		/// </summary>
		/// <param name="userId">The user global id.</param>
		/// <param name="userName">Title of the user.</param>
		/// <param name="shardId">The shard id.</param>
		/// <param name="ipAddress">The ip address.</param>
		/// <param name="userAgent">The user agent.</param>
		/// <param name="base64ClientModulus">The base64 client modulus.</param>
		/// <param name="userClientGlobalId">The user client global id.</param>
		/// <returns></returns>
		UserClientSession StoreClientSession(
			Guid userId, 
			string userName, 
			string shardId,
			string ipAddress, 
			string userAgent, 
			string base64ClientModulus, 
			Guid userClientGlobalId);

		/// <summary>
		/// Updates the UserSession in the cache, or removes expired ones.
		/// </summary>
		/// <param name="userSession">The user session.</param>
		void UpdateUserSession(UserSession userSession);

		/// <summary>
		/// Gets the user session if it exists or null.
		/// </summary>
		/// <param name="userId">The user global id.</param>
		/// <returns></returns>
		UserSession GetUserSession(Guid userId);

		/// <summary>
		/// Gets or create a user session if one doesn't exist.
		/// </summary>
		/// <param name="userId">The user global id.</param>
		/// <param name="userName">Title of the user.</param>
		/// <param name="shardId"></param>
		/// <returns></returns>
		UserSession GetOrCreateSession(Guid userId, string userName, string shardId);

		/// <summary>
		/// Gets the user client session identified by the id if exists otherwise null.
		/// </summary>
		/// <param name="userId">The user global id.</param>
		/// <param name="clientSessionId">The client session id.</param>
		/// <returns></returns>
		UserClientSession GetUserClientSession(Guid userId, Guid clientSessionId);
	}
}