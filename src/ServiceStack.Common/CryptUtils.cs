using System;
using System.Security.Cryptography;
using System.Text;

namespace ServiceStack
{
    public enum RsaKeyLengths
    {
        Bit1024 = 1024,
        Bit2048 = 2048,
        Bit4096 = 4096
    }

    public class RsaKeyPair
    {
        public string PrivateKey { get; set; }
        public string PublicKey { get; set; }
    }

    /// <summary>
    /// Useful .NET Encryption Utils from:
    /// https://msdn.microsoft.com/en-us/library/system.security.cryptography.rsacryptoserviceprovider(v=vs.110).aspx
    /// </summary>
    public static class CryptUtils
    {
        public static RsaKeyLengths Length = RsaKeyLengths.Bit2048;
        public static RsaKeyPair KeyPair; 
        
        public static bool DoOAEPPadding = true;

        public static string Encrypt(this string data)
        {
            if (KeyPair != null)
                return Encrypt(KeyPair.PublicKey, data, Length);
            else 
                throw new ArgumentNullException("No KeyPair given for encryption in CryptUtils");
        }

        public static string Decrypt(this string data)
        {
            if (KeyPair != null)
                return Decrypt(KeyPair.PrivateKey, data, Length);
            else 
                throw new ArgumentNullException("No KeyPair given for encryption in CryptUtils");
        }

        public static string Encrypt(string publicKeyXml, string data, RsaKeyLengths rsaKeyLength = RsaKeyLengths.Bit2048)
        {
            var bytes = Encoding.UTF8.GetBytes(data);
            var encryptedBytes = Encrypt(publicKeyXml, bytes, rsaKeyLength);
            string encryptedData = Convert.ToBase64String(encryptedBytes);
            return encryptedData;
        }

        private static byte[] Encrypt(string publicKeyXml, byte[] bytes, RsaKeyLengths rsaKeyLength = RsaKeyLengths.Bit2048)
        {
            byte[] encryptedBytes;
            using (var RSA = new RSACryptoServiceProvider((int) rsaKeyLength))
            {
                RSA.FromXmlString(publicKeyXml);
                encryptedBytes = RSA.Encrypt(bytes, DoOAEPPadding);
            }
            return encryptedBytes;
        }

        public static string Decrypt(string privateKeyXml, string encryptedData, RsaKeyLengths rsaKeyLength = RsaKeyLengths.Bit2048)
        {
            var encryptedBytes = Convert.FromBase64String(encryptedData);
            var bytes = Decrypt(privateKeyXml, encryptedBytes, rsaKeyLength);
            var data = Encoding.UTF8.GetString(bytes);
            return data;
        }

        private static byte[] Decrypt(string privateKeyXml, byte[] encryptedBytes, RsaKeyLengths rsaKeyLength)
        {
            using (var RSA = new RSACryptoServiceProvider((int) rsaKeyLength))
            {
                RSA.FromXmlString(privateKeyXml);
                byte[] bytes = RSA.Decrypt(encryptedBytes, DoOAEPPadding);
                return bytes;
            }
        }

        public static RsaKeyPair CreatePublicAndPrivateKeyPair(RsaKeyLengths rsaKeyLength = RsaKeyLengths.Bit2048)
        {
            var rsaProvider = new RSACryptoServiceProvider((int)rsaKeyLength);
            return new RsaKeyPair
            {
                PrivateKey = rsaProvider.ToXmlString(true),
                PublicKey = rsaProvider.ToXmlString(false),
            };
        }
    }

}