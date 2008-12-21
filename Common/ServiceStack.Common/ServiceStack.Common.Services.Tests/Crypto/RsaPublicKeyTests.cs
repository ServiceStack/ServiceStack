using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using ServiceStack.Common.Services.Crypto;
using NUnit.Framework;
using NUnit.Framework.SyntaxHelpers;

namespace ServiceStack.Common.Services.Tests.Crypto
{
	[TestFixture]
	public class RsaPublicKeyTests
	{
		private RsaPrivateKey PrivateKey { get; set; }
		private RsaPublicKey PublicKey { get; set; }

		[SetUp]
		public void SetUp()
		{
			this.PrivateKey = new RsaPrivateKey(512);
			this.PublicKey = this.PrivateKey.CreatePublicKey();
		}

		[TearDown]
		public void TearDown()
		{
			this.PublicKey = null;
			this.PrivateKey = null;
		}

		[Test]
		public void GenerateRsaPrivateKey()
		{
			Console.WriteLine("            PrivateKey = {0}", this.PrivateKey);
			Console.WriteLine("            PublicKey  = {0}", this.PublicKey);

			// Uncomment to dump private and public key string to text files
			//DumpText(this.PrivateKey.PrivateKeyXml, "RsaPrivateKey.txt");
			//DumpText(this.PrivateKey.PublicKeyXml, "RsaPublicKey.txt");

			var xmlFriendlyPrivateKey = MakeXmlKeyFriendly(this.PrivateKey.PrivateKeyXml);
			var xmlFriendlyPublicKey = MakeXmlKeyFriendly(this.PrivateKey.PublicKeyXml);

			Console.WriteLine("XmlFriendly PrivateKey = {0}", xmlFriendlyPrivateKey);
			Console.WriteLine("XmlFriendly PublicKey  = {0}", xmlFriendlyPublicKey);

			// Uncomment to dump XML friendly private and public key string to text files
			//DumpText(xmlFriendlyPrivateKey, "XmlFriendlyRsaPrivateKey.txt");
			//DumpText(xmlFriendlyPublicKey, "XmlFriendlyRsaPublicKey.txt");

			Assert.That(string.IsNullOrEmpty(this.PrivateKey.PrivateKeyXml), Is.False);
			Assert.That(string.IsNullOrEmpty(this.PrivateKey.PublicKeyXml), Is.False);
			Assert.That(string.IsNullOrEmpty(this.PublicKey.PublicKeyXml), Is.False);
		}

		[Test]
		public void EncryptAndDecryptHashedPassword()
		{
			byte[] password = new ASCIIEncoding().GetBytes("afterallthat909");
			byte[] hashedPassword = HashUtils.HashData(password, new SHA1CryptoServiceProvider());
			byte[] encryptedHashedPassword = this.PublicKey.EncryptData(hashedPassword);
			byte[] decryptedHashedPassword = this.PrivateKey.DecryptData(encryptedHashedPassword);

			if (decryptedHashedPassword.Length != hashedPassword.Length)
			{
				Assert.Fail("The decrypted hashed password is not the same length as the original hashed password");
			}

			for (int i = 0; i < decryptedHashedPassword.Length; i++)
			{
				if (hashedPassword[i] != decryptedHashedPassword[i])
				{
					Assert.Fail("The decrypted hashed password is not the same as the original hashed password");
				}
			}
		}

		[Test]
		public void EncryptAndDecryptHashedPasswordString()
		{
			string password = "afterallthat909";
			string hashedPassword = HashUtils.HashData(password, new SHA1CryptoServiceProvider());

			Console.WriteLine("Hashed password = {0}", hashedPassword);

			string encryptedHashedPassword = this.PublicKey.EncryptData(hashedPassword);

			Console.WriteLine("Encrypted hashed password = {0}", encryptedHashedPassword);

			string decryptedHashedPassword = this.PrivateKey.DecryptData(encryptedHashedPassword);

			Console.WriteLine("Decrypted hashed password = {0}", decryptedHashedPassword);

			Assert.That(hashedPassword, Is.EqualTo(decryptedHashedPassword));
		}

		[Test]
		public void EncryptAndDecryptUnhashedPasswordString()
		{
			string password = "afterallthat909";

			Console.WriteLine("Hashed password = {0}", password);

			string encryptedPassword = this.PublicKey.EncryptData(password);

			Console.WriteLine("Encrypted hashed password = {0}", encryptedPassword);

			string decryptedPassword = this.PrivateKey.DecryptData(encryptedPassword);

			Console.WriteLine("Decrypted hashed password = {0}", decryptedPassword);

			Assert.That(password, Is.EqualTo(decryptedPassword));
		}

		[Test]
		public void SignAndVerifyData()
		{
			string token = string.Format("<PotopeToken><DateCreated>{0}</DateCreated><IpAddress>{1}</IpAddress></PotopeToken>", DateTime.UtcNow.ToString("u"), "127.0.0.1");
			byte[] data = new ASCIIEncoding().GetBytes(token);
			byte[] signature = this.PrivateKey.SignData(data, new SHA1CryptoServiceProvider());
			bool verified = this.PublicKey.VerifySignature(data, new SHA1CryptoServiceProvider(), signature);
			
			Assert.That(verified, Is.True);
		}

		[Test]
		public void SignAndVerifyDataString()
		{
			string token = string.Format("<PotopeToken><DateCreated>{0}</DateCreated><IpAddress>{1}</IpAddress></PotopeToken>", DateTime.UtcNow.ToString("u"), "127.0.0.1");

			Console.WriteLine("Token = {0}", token);

			string signature = this.PrivateKey.SignData(token, new SHA1CryptoServiceProvider());

			Console.WriteLine("Signature = {0}", signature);

			bool verified = this.PublicKey.VerifySignature(token, new SHA1CryptoServiceProvider(), signature);

			Assert.That(verified, Is.True);
		}

		[Test]
		public void SignAndVerifyToken()
		{
			var loginToken = string.Format("auth={0}|{1}sign=", DateTime.UtcNow.ToString("u"), "127.0.0.1");
			var signature = this.PrivateKey.SignData(loginToken, new SHA1CryptoServiceProvider());
			loginToken += signature;

			Assert.That(loginToken.StartsWith("auth="), Is.True);
			Assert.That(loginToken.Contains("|"), Is.True);
			Assert.That(loginToken.Contains("sign="), Is.True);

			var tokenElements = loginToken.Split(new[] {"auth=", "|", "sign="}, StringSplitOptions.RemoveEmptyEntries);
			
			Assert.That(tokenElements.Length == 3, Is.True);
			
			var loginTokenExcludeSignature = loginToken.Substring(0, loginToken.IndexOf(tokenElements[2]));

			var loginTokenValid = this.PublicKey.VerifySignature(loginTokenExcludeSignature, new SHA1CryptoServiceProvider(), tokenElements[2]);
			Assert.That(loginTokenValid, Is.True);
		}

		private static string MakeXmlKeyFriendly(string xml)
		{
			return xml.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;").Replace("\"", "&quot;").Replace("'", "&apos;");
		}

		private static void DumpText(string keyXml, string fileName)
		{
			var fileDumpName = Environment.CurrentDirectory + "\\" + fileName;
			using (TextWriter writer = new StreamWriter(fileDumpName, false))
			{
				writer.Write(keyXml);
			}
		}
	}
}