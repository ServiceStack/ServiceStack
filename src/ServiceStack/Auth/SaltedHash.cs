using System;
using System.Security.Cryptography;
using System.Text;
using ServiceStack.Text;

namespace ServiceStack.Auth
{
    public interface IHashProvider
    {
        void GetHashAndSalt(byte[] Data, out byte[] Hash, out byte[] Salt);
        void GetHashAndSaltString(string Data, out string Hash, out string Salt);
        bool VerifyHash(byte[] Data, byte[] Hash, byte[] Salt);
        bool VerifyHashString(string Data, string Hash, string Salt);
    }

    /// <summary>
    /// Thank you Martijn
    /// http://www.dijksterhuis.org/creating-salted-hash-values-in-c/
    /// 
    /// Stronger/Slower Alternative: 
    /// https://github.com/defuse/password-hashing/blob/master/PasswordStorage.cs
    /// </summary>
    public class SaltedHash : IHashProvider
    {
        readonly HashAlgorithm HashProvider;
        readonly int SalthLength;

        public SaltedHash(HashAlgorithm HashAlgorithm, int theSaltLength)
        {
            HashProvider = HashAlgorithm;
            SalthLength = theSaltLength;
        }

        public SaltedHash() : this(SHA256.Create(), 4) {}

        private byte[] ComputeHash(byte[] Data, byte[] Salt)
        {
            var DataAndSalt = new byte[Data.Length + SalthLength];
            Array.Copy(Data, DataAndSalt, Data.Length);
            Array.Copy(Salt, 0, DataAndSalt, Data.Length, SalthLength);

            return HashProvider.ComputeHash(DataAndSalt);
        }

        public void GetHashAndSalt(byte[] Data, out byte[] Hash, out byte[] Salt)
        {
            Salt = new byte[SalthLength];

            var random = RandomNumberGenerator.Create();
#if !NETSTANDARD1_6
            random.GetNonZeroBytes(Salt);
#else
            random.GetBytes(Salt);
#endif

            Hash = ComputeHash(Data, Salt);
        }

        public void GetHashAndSaltString(string Data, out string Hash, out string Salt)
        {
            byte[] HashOut;
            byte[] SaltOut;

            GetHashAndSalt(Encoding.UTF8.GetBytes(Data), out HashOut, out SaltOut);

            Hash = Convert.ToBase64String(HashOut);
            Salt = Convert.ToBase64String(SaltOut);
        }

        public bool VerifyHash(byte[] Data, byte[] Hash, byte[] Salt)
        {
            var NewHash = ComputeHash(Data, Salt);

            if (NewHash.Length != Hash.Length) return false;

            for (int Lp = 0; Lp < Hash.Length; Lp++)
                if (!Hash[Lp].Equals(NewHash[Lp]))
                    return false;

            return true;
        }

        public bool VerifyHashString(string Data, string Hash, string Salt)
        {
            if (Hash == null || Salt == null)
                return false;
            
            byte[] HashToVerify = Convert.FromBase64String(Hash);
            byte[] SaltToVerify = Convert.FromBase64String(Salt);
            byte[] DataToVerify = Encoding.UTF8.GetBytes(Data);
            return VerifyHash(DataToVerify, HashToVerify, SaltToVerify);
        }
    }

    public static class HashExtensions
    {
        public static string ToSha256Hash(this string value)
        {
            var sb = StringBuilderCache.Allocate();
            using (var hash = SHA256.Create())
            {
                var result = hash.ComputeHash(value.ToUtf8Bytes());
                foreach (var b in result)
                {
                    sb.Append(b.ToString("x2"));
                }
            }
            return StringBuilderCache.ReturnAndFree(sb);
        }

        public static byte[] ToSha256HashBytes(this byte[] bytes)
        {
            using (var hash = SHA256.Create())
            {
                return hash.ComputeHash(bytes);
            }
        }

        public static byte[] ToSha512HashBytes(this byte[] bytes)
        {
            using (var hash = SHA512.Create())
            {
                return hash.ComputeHash(bytes);
            }
        }
    }

    /*
    /// <summary>
    /// This little demo code shows how to encode a users password.
    /// </summary>
    class SaltedHashDemo
    {
        public static void Main(string[] args)
        {
            // We use the default SHA-256 & 4 byte length
            SaltedHash demo = new SaltedHash();

            // We have a password, which will generate a Hash and Salt
            string Password = "MyGlook234";
            string Hash;
            string Salt;

            demo.GetHashAndSaltString(Password, out Hash, out Salt);
            Console.WriteLine("Password = {0} , Hash = {1} , Salt = {2}", Password, Hash, Salt);

            // Password validation
            //
            // We need to pass both the earlier calculated Hash and Salt (we need to store this somewhere safe between sessions)

            // First check if a wrong password passes
            string WrongPassword = "OopsOops";
            Console.WriteLine("Verifying {0} = {1}", WrongPassword, demo.VerifyHashString(WrongPassword, Hash, Salt));

            // Check if the correct password passes
            Console.WriteLine("Verifying {0} = {1}", Password, demo.VerifyHashString(Password, Hash, Salt));
        }	 
    }
 */

}