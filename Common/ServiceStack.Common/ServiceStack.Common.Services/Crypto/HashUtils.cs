using System;
using System.Security.Cryptography;
using System.Text;

namespace ServiceStack.Common.Services.Crypto
{
	/// <summary>
	/// Helper methods for hashing data
	/// </summary>
	public static class HashUtils
	{
		/// <summary>
		/// Calculate the hash of the provided data.
		/// </summary>
		/// <param name="data">Data to hash</param>
		/// <param name="hashAlgorithm">Hash algorithm to use</param>
		/// <returns>Hash of the data</returns>
		public static byte[] HashData(byte[] data, HashAlgorithm hashAlgorithm)
		{
			return hashAlgorithm.ComputeHash(data);
		}

		/// <summary>
		/// Calculate the hash of the provided data.
		/// </summary>
		/// <param name="data">Data to hash</param>
		/// <param name="hashAlgorithm">Hash algorithm to use</param>
		/// <returns>Hash of the data as a Base64 string</returns>
		public static string HashData(string data, HashAlgorithm hashAlgorithm)
		{
			byte[] dataBuffer = new ASCIIEncoding().GetBytes(data);
			byte[] hashedDataBuffer = hashAlgorithm.ComputeHash(dataBuffer);
			return Convert.ToBase64String(hashedDataBuffer);
		}
	}
}