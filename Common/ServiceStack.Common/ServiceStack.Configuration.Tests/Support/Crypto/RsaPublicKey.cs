using System;
using System.Security.Cryptography;
using System.Text;
using System.Xml;
using ServiceStack.Logging;

namespace ServiceStack.Configuration.Tests.Support.Crypto
{
	/// <summary>
	/// RSA public key
	/// </summary>
	public class RsaPublicKey
	{
		private string publicKeyXml;
		private RSACryptoServiceProvider rsaService;

		private readonly ILog log= LogManager.GetLogger(typeof(RsaPublicKey));

		protected RsaPublicKey()
		{
		}

		/// <summary>
		/// Create an RSA public key from XML
		/// </summary>
		/// <param name="publicKeyXml">XML RSA public key</param>
		public RsaPublicKey(string publicKeyXml)
		{
			this.PublicKeyXml = publicKeyXml;
		}

		/// <summary>
		/// XML RSA public key
		/// </summary>
		public string PublicKeyXml
		{
			get { return this.publicKeyXml; }
			set
			{
				this.publicKeyXml = value;

				// Create the RSAS crypto service provider from the public key xml
				this.rsaService = new RSACryptoServiceProvider();
				this.rsaService.FromXmlString(this.publicKeyXml);
			}
		}

		/// <summary>
		/// Encypt data with the public key. This can only be decrypted with the 
		/// associated private key
		/// </summary>
		/// <param name="data">Data to encrypt</param>
		/// <returns>Encrypted data</returns>
		public virtual byte[] EncryptData(byte[] data)
		{
			return this.rsaService.Encrypt(data, false);
		}

		/// <summary>
		/// Encypt data with the public key and return the encyrpted value in 
		/// Base64 format. This can only be decrypted with the associated private
		/// key.
		/// </summary>
		/// <param name="data">Data to encrypt</param>
		/// <returns>Encrypted data converted to a Base64 string</returns>
		public virtual string EncryptData(string data)
		{
			byte[] dataBuffer = Encoding.ASCII.GetBytes(data);
			byte[] encryptedDataBuffer = this.rsaService.Encrypt(dataBuffer, false);
			return Convert.ToBase64String(encryptedDataBuffer);
		}

		/// <summary>
		/// Verify data signed by the associated private key using SHA1 hash.
		/// </summary>
		/// <param name="data">Data to check the signature on</param>
		/// <param name="signature">Signature of the data</param>
		/// <returns>True if the signature is valid for the data else false</returns>
		public virtual bool VerifySHA1Signature(byte[] data, byte[] signature)
		{
			return this.VerifySignature(data, HashUtils.Sha1HashAlgorithm, signature);
		}

		/// <summary>
		/// Verify data signed by the associated private key using SHA1 hash. The
		/// signature must be in Base64 format.
		/// </summary>
		/// <param name="data">Data to check the signature on</param>
		/// <param name="base64Signature">Signature of the data converted to a Base64 string</param>
		/// <returns>True if the signature is valid for the data else false</returns>
		public virtual bool VerifySHA1Signature(string data, string base64Signature)
		{
			return this.VerifySignature(data, HashUtils.Sha1HashAlgorithm, base64Signature);
		}

		/// <summary>
		/// Verify data signed by the associated private key.
		/// </summary>
		/// <param name="data">Data to check the signature on</param>
		/// <param name="hashAlgorithm">The hash algorithm used when signing the data</param>
		/// <param name="signature">Signature of the data</param>
		/// <returns>True if the signature is valid for the data else false</returns>
		public virtual bool VerifySignature(byte[] data, HashAlgorithm hashAlgorithm, byte[] signature)
		{
			return this.rsaService.VerifyData(data, hashAlgorithm, signature);
		}

		/// <summary>
		/// Verify data signed by the associated private key. The signature must
		/// be in Base64 format.
		/// </summary>
		/// <param name="data">Data to check the signature on</param>
		/// <param name="hashAlgorithm">The hash algorithm used when signing the data</param>
		/// <param name="base64Signature">Signature of the data converted to a Base64 string</param>
		/// <returns>True if the signature is valid for the data else false</returns>
		public virtual bool VerifySignature(string data, HashAlgorithm hashAlgorithm, string base64Signature)
		{
			try
			{
				byte[] dataBuffer = Encoding.ASCII.GetBytes(data);
				byte[] signatureBuffer = Convert.FromBase64String(base64Signature);
				return this.rsaService.VerifyData(dataBuffer, hashAlgorithm, signatureBuffer);
			}
			catch (FormatException ex)
			{
				// Swallow Base64 format exceptions
				this.log.Error("Verifying Base64 signature", ex);
				return false;
			}
		}

		public string Base64Modulus
		{
			get
			{
				var doc = new XmlDocument();
				doc.LoadXml(this.PublicKeyXml);

				var modulusNode = ExtractXmlNode(doc, "RSAKeyValue/Modulus");

				if (modulusNode != null)
				{
					return modulusNode.InnerText;
				}

				return string.Empty;
			}
		}

		private static XmlNode ExtractXmlNode(XmlNode node, string xPath)
		{
			var xmlNodes = node.SelectNodes(xPath);
			return (xmlNodes != null && xmlNodes.Count > 0) ? xmlNodes[0] : null;
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
			return this.PublicKeyXml ?? string.Empty;
		}
	}
}