using System;
using System.Security.Cryptography;
using System.Text;

namespace ServiceStack.Configuration.Tests.Support.Crypto
{
	/// <summary>
	/// Helper methods for hashing data
	/// </summary>
	public static class HashUtils
	{
		private const int SaltLength = 0x10;

		internal static readonly RNGCryptoServiceProvider RngSaltGenerator = new RNGCryptoServiceProvider();
		internal static readonly SHA1CryptoServiceProvider Sha1HashAlgorithm = new SHA1CryptoServiceProvider();

		public static byte[] SHA1Hash(byte[] data)
		{
			return Hash(data, Sha1HashAlgorithm);
		}

		public static string SHA1Hash(string data)
		{
			return Hash(data, Sha1HashAlgorithm);
		}

		/// <summary>
		/// Calculate the hash of the provided data.
		/// </summary>
		/// <param name="data">Data to hash</param>
		/// <param name="hashAlgorithm">Hash algorithm to use</param>
		/// <returns>Hash of the data</returns>
		public static byte[] Hash(byte[] data, HashAlgorithm hashAlgorithm)
		{
			return hashAlgorithm.ComputeHash(data);
		}

		/// <summary>
		/// Calculate the hash of the provided data.
		/// </summary>
		/// <param name="data">Data to hash</param>
		/// <param name="hashAlgorithm">Hash algorithm to use</param>
		/// <returns>Hash of the data as a Base64 string</returns>
		public static string Hash(string data, HashAlgorithm hashAlgorithm)
		{
			byte[] dataBuffer = Encoding.ASCII.GetBytes(data);
			byte[] hashedDataBuffer = hashAlgorithm.ComputeHash(dataBuffer);
			return Convert.ToBase64String(hashedDataBuffer);
		}

		public static byte[] GenerateSalt(int saltLength)
		{
			byte[] salt = new byte[saltLength];
			RngSaltGenerator.GetBytes(salt);
			return salt;
		}

		public static string GenerateSHA1SaltPassword(string password)
		{
			return GenerateSHA1SaltPassword(password, SaltLength);
		}

		public static string GenerateSHA1SaltPassword(string password, int saltLength)
		{
			byte[] salt = GenerateSalt(saltLength);
			byte[] saltedPasswordHash = CalculateSaltedPasswordHash(password, salt, Sha1HashAlgorithm);
			return string.Format("{0}:{1}", Convert.ToBase64String(salt), Convert.ToBase64String(saltedPasswordHash));
		}

		public static string GenerateSaltPassword(string password, int saltLength, HashAlgorithm hashAlgorithm)
		{
			byte[] salt = GenerateSalt(saltLength);
			byte[] saltedPasswordHash = CalculateSaltedPasswordHash(password, salt, hashAlgorithm);
			return string.Format("{0}:{1}", Convert.ToBase64String(salt), Convert.ToBase64String(saltedPasswordHash));
		}

		public static byte[] ExtractSalt(string saltPassword)
		{
			string base64Salt = saltPassword.Split(':')[0];
			return Convert.FromBase64String(base64Salt);
		}

		public static byte[] ExtractSaltedPasswordHash(string saltPassword)
		{
			string base64SaltedPasswordHash = saltPassword.Split(':')[1];
			return Convert.FromBase64String(base64SaltedPasswordHash);
		}

		public static bool VerifySHA1PasswordHash(string password, string saltPassword)
		{
			return VerifyPasswordHash(password, saltPassword, Sha1HashAlgorithm);
		}

		public static bool VerifyPasswordHash(string password, string saltPassword, HashAlgorithm hashAlgorithm)
		{
			// Extract the salt used to salt the password hash
			byte[] salt = ExtractSalt(saltPassword);

			// Extract the salted password hash from the salt password
			byte[] saltedPasswordHash = ExtractSaltedPasswordHash(saltPassword);

			// Generate a salted password hash using the same salt as the current password
			byte[] attemptSaltedPasswordHash = CalculateSaltedPasswordHash(password, salt, hashAlgorithm);

			// Compare the two salted password hash arrays
			return ByteArrayEquals(attemptSaltedPasswordHash, saltedPasswordHash);
		}

		private static byte[] CalculateSaltedPasswordHash(string password, byte[] salt, HashAlgorithm hashAlgorithm)
		{
			byte[] passwordData = Encoding.ASCII.GetBytes(password);
			byte[] saltPasswordData = ByteArrayMerge(salt, passwordData);
			return hashAlgorithm.ComputeHash(saltPasswordData);
		}

		private static byte[] ByteArrayMerge(byte[] first, byte[] second)
		{
			byte[] result = new byte[first.Length + second.Length];
			Buffer.BlockCopy(first, 0, result, 0, first.Length);
			Buffer.BlockCopy(second, 0, result, first.Length, second.Length);
			return result;
		}

		private static bool ByteArrayEquals(byte[] a1, byte[] a2)
		{
			if (a1 == a2)
			{
				return true;
			}

			if (a1 != null && a2 != null)
			{
				if (a1.Length != a2.Length)
				{
					return false;
				}

				for (int i = 0; i < a1.Length; i++)
				{
					if (a1[i] != a2[i])
					{
						return false;
					}
				}

				return true;
			}

			return false;
		}
	}
}