/*
// $Id: PublicAndPrivateClientSessions.cs 3595 2009-05-20 09:57:17Z Demis Bellot $
//
// Revision      : $Revision: 3595 $
// Modified Date : $LastChangedDate: 2009-05-20 10:57:17 +0100 (Wed, 20 May 2009) $
// Modified By   : $LastChangedBy: Demis Bellot $
//
// (c) Copyright 2010 Liquidbit Ltd
*/


namespace ServiceStack.ServiceInterface.Session
{
	/// <summary>
	/// Holds a 'Secure' and 'Unsecure' client session for the user.
	/// The secure client session should only be transported over a secure channel.
	/// </summary>
	public class PublicAndPrivateClientSessions
	{
		public PublicAndPrivateClientSessions(
			UserClientSession unsecureClientSession, UserClientSession secureClientSession)
		{
			this.UnsecureClientSession = unsecureClientSession;
			this.SecureClientSession = secureClientSession;
		}

		public UserClientSession UnsecureClientSession { get; private set; }
		public UserClientSession SecureClientSession { get; private set; }
	}
}