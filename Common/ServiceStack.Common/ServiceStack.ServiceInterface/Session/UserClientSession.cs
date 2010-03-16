/*
// $Id: UserClientSession.cs 13369 2010-03-08 19:09:44Z DDNGLOBAL\Demis $
//
// Revision      : $Revision: 13369 $
// Modified Date : $LastChangedDate: 2010-03-08 19:09:44 +0000 (Mon, 08 Mar 2010) $
// Modified By   : $LastChangedBy: DDNGLOBAL\Demis $
//
// (c) Copyright 2010 Liquidbit Ltd
*/

using System;
using ServiceStack.DesignPatterns.Model;

namespace ServiceStack.ServiceInterface.Session
{
	/// <summary>
	/// Holds information on a single 'User Client' session.
	/// 
	/// A user can have multiple of these client sessions, 
	/// each from a different web browser or client application.
	/// </summary>
	public class UserClientSession
		: IHasGuidId
	{
		private const int ValidForTwoWeeks = 14;

		/// <summary>
		/// Unique Id for this session
		/// </summary>
		public Guid Id { get; set; }

		/// <summary>
		/// Id of the User for this session
		/// </summary>
		public Guid UserId { get; set; }

		/// <summary>
		/// If provided stores the clients public key to enable secure transmission
		/// </summary>
		public string Base64ClientModulus { get; set; }

		/// <summary>
		/// The unique global and persistent id for the client application. 
		/// (Can be stored in a persistent cookie)
		/// </summary>
		public Guid UserClientGlobalId { get; set; }
		
		/// <summary>
		/// The IpAddress for the client 
		/// </summary>
		public string IpAddress { get; private set; }
		
		/// <summary>
		/// Holds meta-information about the operating environment of this client
		/// </summary>
		public string UserAgent { get; private set; }
		
		/// <summary>
		/// When this client session expires
		/// </summary>
		public DateTime ExpiryDate { get; set; }

		/// <summary>
		/// Empty constructor required for TypeSerializer
		/// </summary>
		public UserClientSession() {}

		public UserClientSession(
			Guid sessionId, 
			Guid userId, 
			string ipAddress, 
			string userAgent, 
			string base64ClientModulus, 
			Guid userClientGlobalId)
		{
			this.Id = sessionId;
			this.UserId = userId;
			this.IpAddress = ipAddress;
			this.UserAgent = userAgent;
			this.Base64ClientModulus = base64ClientModulus;
			this.UserClientGlobalId = userClientGlobalId;
			this.ExpiryDate = DateTime.UtcNow.AddDays(ValidForTwoWeeks);
		}
	}

}