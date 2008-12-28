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

namespace @ServiceNamespace@.Logic.LogicCommands
{
	public class Change@ModelName@PasswordLogicCommand : LogicCommandBase<bool>
	{
		public Guid @ModelName@GlobalId { get; set; }
		public string Base64OldEncryptedPassword { get; set; }
		public string Base64NewEncryptedPassword { get; set; }

		public override bool Execute()
		{
			var db@ModelName@ = this.Provider.Get@ModelName@(this.@ModelName@GlobalId);

			if (db@ModelName@ != null)
			{
				// Decrypt the old and new passwords
				string oldDecryptedPassword = base.AppContext.ServerPrivateKey.DecryptBase64String(this.Base64OldEncryptedPassword);
				string newDecryptedPassword = base.AppContext.ServerPrivateKey.DecryptBase64String(this.Base64NewEncryptedPassword);

				if (oldDecryptedPassword == null || newDecryptedPassword == null)
				{
					// TODO log error message
					return false;
				}

				// Verify that the old password matches the stored salted password hash
				if (HashUtils.VerifySHA1PasswordHash(oldDecryptedPassword, db@ModelName@.SaltPassword))
				{
					// Generate a salt password and store it on the user
					db@ModelName@.SaltPassword = HashUtils.GenerateSHA1SaltPassword(newDecryptedPassword);

					this.Provider.Store(db@ModelName@);
					
					return true;
				}
			}

			return false;
		}
	}
}