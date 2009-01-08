using System;
using System.Security.Cryptography;
using System.Text;
using System.Xml;
using ServiceStack.Logging;

namespace ServiceStack.Configuration.Tests.Support.Crypto
{
	/// <summary>
	/// RSA private key. Can generate the public key from the private key.
	/// </summary>
	public class RsaPrivateKey
	{
		private readonly ILog log = LogManager.GetLogger(typeof(RsaPrivateKey));

		private string privateKeyXml;
		private RSACryptoServiceProvider rsaService;

		public string PublicKeyXml { get; private set; }

		/// <summary>
		/// Create a RSA private key from XML
		/// </summary>
		/// <param name="privateKeyXml">XML RSA public key</param>
		/// <exception cref="CryptographicException"/>
		/// <exception cref="ArgumentNullException"/>
		public RsaPrivateKey(string privateKeyXml)
		{
			this.PrivateKeyXml = privateKeyXml;
		}

		/// <summary>
		/// Create a new RSA private key
		/// </summary>
		/// <param name="dwKeySize">The RSA key size in bits</param>
		/// <exception cref="CryptographicException"/>
		public RsaPrivateKey(int dwKeySize)
		{
			this.PrivateKeyXml = GenerateRsaPrivateKey(dwKeySize);
		}

		/// <summary>
		/// XML RSA private key
		/// </summary>
		/// <exception cref="CryptographicException"/>
		/// <exception cref="ArgumentNullException"/>
		public string PrivateKeyXml
		{
			get { return this.privateKeyXml; }
			set
			{
				this.privateKeyXml = value;
				this.rsaService = new RSACryptoServiceProvider();
				this.rsaService.FromXmlString(this.privateKeyXml);
				this.PublicKeyXml = ExtractRsaPublicKey(this.privateKeyXml);
			}
		}

		/// <summary>
		/// Create the associated public key for this private key
		/// </summary>
		/// <returns>The public key or NULL if no valid private key is defined</returns>
		public virtual RsaPublicKey CreatePublicKey()
		{
			return string.IsNullOrEmpty(this.PublicKeyXml) == false ? new RsaPublicKey(this.PublicKeyXml) : null;
		}

		/// <summary>
		/// Decrypt data that was encrypted with the associated public key.
		/// </summary>
		/// <param name="encryptedData">Data to decrypt</param>
		/// <returns>Decrypted data or NULL if 'Bad Data'</returns>
		/// <exception cref="ArgumentNullException"/>
		public virtual byte[] DecryptByteArray(byte[] encryptedData)
		{
			try
			{
				return this.rsaService.Decrypt(encryptedData, false);
			}
			catch (CryptographicException ex)
			{
				// Swallow 'Bad Data' Cryptographic exceptions
				this.log.Error("Decrypting byte array", ex);
				return null;
			}
		}

		/// <summary>
		/// Decrypt data in Base64 format that was encrypted with the associated
		/// public key.
		/// </summary>
		/// <param name="base64EncryptedString">Data to decrypt as Base64 string</param>
		/// <returns>Decrypted data or NULL if string not Base64 or 'Bad Data'</returns>
		/// <exception cref="ArgumentNullException"/>
		public virtual string DecryptBase64String(string base64EncryptedString)
		{
			try
			{
				byte[] encryptedData = Convert.FromBase64String(base64EncryptedString);
				byte[] dataBuffer = this.rsaService.Decrypt(encryptedData, false);
				return Encoding.ASCII.GetString(dataBuffer);
			}
			catch (CryptographicException ex)
			{
				// Swallow 'Bad Data' Cryptographic exceptions
				this.log.Error("Decrypting Base64 string", ex);
				return null;
			}
			catch (FormatException ex)
			{
				// Swallow Base64 format exceptions
				this.log.Error("Decrypting Base64 string", ex);
				return null;
			}
		}

		/// <summary>
		/// Calculate signature by encyrpting the SHA1 hash of the data. This can
		/// only be verified by the associated public key.
		/// </summary>
		/// <param name="data">Data to sign</param>
		/// <param name="hashAlgorithm">The hash algorithm to use</param>
		/// <returns>Signature of the data</returns>
		/// <exception cref="ArgumentNullException"/>
		/// <exception cref="ArgumentException"/>
		public virtual byte[] SHA1SignData(byte[] data, HashAlgorithm hashAlgorithm)
		{
			return this.SignData(data, HashUtils.Sha1HashAlgorithm);
		}

		/// <summary>
		/// Calculate signature by encyrpting the SHA1 hash of the data. The
		/// signature is returned in Base64 format. This can only be verified by
		/// the associated public key.
		/// </summary>
		/// <param name="data">Data to sign</param>
		/// <returns>Signature of the data converted to a Base64 string</returns>
		/// <exception cref="ArgumentNullException"/>
		/// <exception cref="ArgumentException"/>
		public virtual string SHA1SignData(string data)
		{
			return this.SignData(data, HashUtils.Sha1HashAlgorithm);
		}

		/// <summary>
		/// Calculate signature by encyrpting the hash of the data. This can only
		/// be verified by the associated public key.
		/// </summary>
		/// <param name="data">Data to sign</param>
		/// <param name="hashAlgorithm">The hash algorithm to use</param>
		/// <returns>Signature of the data</returns>
		/// <exception cref="ArgumentNullException"/>
		/// <exception cref="ArgumentException"/>
		public virtual byte[] SignData(byte[] data, HashAlgorithm hashAlgorithm)
		{
			return this.rsaService.SignData(data, hashAlgorithm);
		}

		/// <summary>
		/// Calculate signature by encyrpting the hash of the data. The signature
		/// is returned in Base64 format. This can only be verified by the 
		/// associated public key.
		/// </summary>
		/// <param name="data">Data to sign</param>
		/// <param name="hashAlgorithm">The hash algorithm to use</param>
		/// <returns>Signature of the data converted to a Base64 string</returns>
		/// <exception cref="ArgumentNullException"/>
		/// <exception cref="ArgumentException"/>
		public virtual string SignData(string data, HashAlgorithm hashAlgorithm)
		{
			byte[] dataBuffer = Encoding.ASCII.GetBytes(data);
			byte[] signature = this.rsaService.SignData(dataBuffer, hashAlgorithm);
			return Convert.ToBase64String(signature);
		}

		public override bool Equals(object obj)
		{
			string objString = obj as string ?? string.Empty;
			return objString.Equals(this.ToString(), StringComparison.Ordinal);
		}

		public override int GetHashCode()
		{
			return this.ToString().GetHashCode();
		}

		public override string ToString()
		{
			return this.PrivateKeyXml ?? string.Empty;
		}

		#region Static RSA private key generation

		/// <summary>
		/// Generate a RSA private key XML string
		/// </summary>
		/// <param name="dwKeySize">The RSA key size in bits</param>
		/// <returns>Private key XML string</returns>
		/// <exception cref="CryptographicException"/>
		public static string GenerateRsaPrivateKey(int dwKeySize)
		{
			return new RSACryptoServiceProvider(dwKeySize).ToXmlString(true);
		}

		private static string ExtractRsaPublicKey(string privateKey)
		{
			var doc = new XmlDocument();
			doc.LoadXml(privateKey);

			var modulusNode = ExtractXmlNode(doc, "RSAKeyValue/Modulus");
			var exponentNode = ExtractXmlNode(doc, "RSAKeyValue/Exponent");

			if (modulusNode != null && exponentNode != null)
			{
				return string.Format("<RSAKeyValue><Modulus>{0}</Modulus><Exponent>{1}</Exponent></RSAKeyValue>", modulusNode.InnerText, exponentNode.InnerText);
			}

			return string.Empty;
		}

		private static XmlNode ExtractXmlNode(XmlNode node, string xPath)
		{
			var xmlNodes = node.SelectNodes(xPath);
			return (xmlNodes != null && xmlNodes.Count > 0) ? xmlNodes[0] : null;
		}

		#endregion
	}
}