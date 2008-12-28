/*
// $Id$
//
// Revision      : $Revision$
// Modified Date : $LastChangedDate$
// Modified By   : $LastChangedBy$
//
// (c) Copyright 2008 Digital Distribution Networks Ltd
*/

using System;
using Ddn.Common.Services.Crypto;
using @DomainModelNamespace@.@ServiceName@;

namespace @ServiceNamespace@.Logic.LogicCommands
{
	public class GetLoginAuthLogicCommand : LogicCommandBase<LoginAuth>
	{
		public string @ModelName@Name { get; set; }
		public string Base64EncryptedPassword { get; set; }
		public string Base64ClientModulus { get; set; }
		public string ClientIPAddress { get; set; }

		public override LoginAuth Execute()
		{
			var db@ModelName@ = this.Provider.Get@ModelName@By@ModelName@Name(this.@ModelName@Name);

			if (db@ModelName@ != null)
			{
				// Decrypt the password attempt
				string decryptedPassword = base.AppContext.ServerPrivateKey.DecryptBase64String(
						this.Base64EncryptedPassword);

				// Verify that the password attempt matches the stored salted password hash
				if (decryptedPassword != null
					&& HashUtils.VerifySHA1PasswordHash(decryptedPassword, db@ModelName@.SaltPassword))
				{
					DateTime utcNow = DateTime.UtcNow;

					var userId = (int)db@ModelName@.Id;
					var clientSessions = this.AppContext.SessionManager.AddClientSession(
							userId, this.@ModelName@Name, this.Base64ClientModulus, this.ClientIPAddress);

					return new LoginAuth {
						@ModelName@Id = userId,
						PublicSessionId = clientSessions.UnsecureClientSession.Id,
						SecureSessionId = clientSessions.SecureClientSession.Id,
						ServerTime = utcNow,
						ExpiryDate = clientSessions.UnsecureClientSession.ExpiryDate,
					};
				}
			}

			return null;
		}
	}
}