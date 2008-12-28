using System;
using System.Collections.Generic;
using Ddn.Common.Services.Crypto;
using Ddn.Common.Services.Utils;

namespace @ServiceNamespace@.Logic.Support
{
	public class PasswordHelper
	{
		private PasswordHelper() { }

		public string Password { get; set; }
		public string ConfirmPassword { get; set; }

		public static PasswordHelper Create(RsaPrivateKey privateKey, string base64EncryptedPassword, string base64EncryptedConfirmPassword)
		{
			if (privateKey == null) throw new ArgumentNullException("privateKey");
			if (base64EncryptedPassword == null) throw new ArgumentNullException("base64EncryptedPassword");

			var passwords = new PasswordHelper {
				Password = privateKey.DecryptBase64String(base64EncryptedPassword),
				ConfirmPassword = privateKey.DecryptBase64String(base64EncryptedConfirmPassword)
			};
			return passwords;
		}

		public bool PasswordsAreEqual
		{
			get { return Password != null && Password.Equals(ConfirmPassword, StringComparison.Ordinal); }
		}

		public string SaltedPassword
		{
			get { return HashUtils.GenerateSHA1SaltPassword(Password); }
		}
	}
}