using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using ServiceStack.Text;

namespace ServiceStack
{
    [Obsolete("Use Rsa.* static class")]
    public class CryptUtils
    {
        public static string Encrypt(string publicKeyXml, string data, RsaKeyLengths rsaKeyLength = RsaKeyLengths.Bit2048)
        {
            return Rsa.Encrypt(data, publicKeyXml, rsaKeyLength);
        }

        public static string Decrypt(string privateKeyXml, string encryptedData, RsaKeyLengths rsaKeyLength = RsaKeyLengths.Bit2048)
        {
            return Rsa.Encrypt(encryptedData, privateKeyXml, rsaKeyLength);
        }

        public static RsaKeyPair CreatePublicAndPrivateKeyPair(RsaKeyLengths rsaKeyLength = RsaKeyLengths.Bit2048)
        {
            return Rsa.CreatePublicAndPrivateKeyPair(rsaKeyLength);
        }
    }

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
    public static class Rsa
    {
        public static RsaKeyLengths KeyLength = RsaKeyLengths.Bit2048;
        public static RsaKeyPair DefaultKeyPair; 
        public static bool DoOAEPPadding = true;

        public static string FromRSAParameters(this RSAParameters rsaParams)
        {
            using (var rsa = new RSACryptoServiceProvider())
            {
                rsa.ImportParameters(rsaParams);
                return rsa.ToXmlString(includePrivateParameters: true);
            }
        }

        public static RSAParameters ToRSAParameters(this string privateKeyXml)
        {
            using (var rsa = new RSACryptoServiceProvider())
            {
                rsa.FromXmlString(privateKeyXml);
                return rsa.ExportParameters(includePrivateParameters: true);
            }
        }

        public static string ToPublicKeyXml(this RSAParameters privateKey)
        {
            using (var rsa = new RSACryptoServiceProvider())
            {
                rsa.ImportParameters(privateKey);

                return rsa.ToXmlString(includePrivateParameters: false);
            }
        }

        public static string Encrypt(this string text)
        {
            if (DefaultKeyPair != null)
                return Encrypt(text, DefaultKeyPair.PublicKey, KeyLength);
            else 
                throw new ArgumentNullException("No KeyPair given for encryption in CryptUtils");
        }

        public static string Decrypt(this string data)
        {
            if (DefaultKeyPair != null)
                return Decrypt(data, DefaultKeyPair.PrivateKey, KeyLength);
            else 
                throw new ArgumentNullException("No KeyPair given for encryption in CryptUtils");
        }

        public static string Encrypt(string text, string publicKeyXml, RsaKeyLengths rsaKeyLength = RsaKeyLengths.Bit2048)
        {
            var bytes = Encoding.UTF8.GetBytes(text);
            var encryptedBytes = Encrypt(publicKeyXml, bytes, rsaKeyLength);
            string encryptedData = Convert.ToBase64String(encryptedBytes);
            return encryptedData;
        }

        public static string Encrypt(string text, RSAParameters publicKey, RsaKeyLengths rsaKeyLength = RsaKeyLengths.Bit2048)
        {
            var bytes = Encoding.UTF8.GetBytes(text);
            var encryptedBytes = Encrypt(bytes, publicKey, rsaKeyLength);
            string encryptedData = Convert.ToBase64String(encryptedBytes);
            return encryptedData;
        }

        public static byte[] Encrypt(string publicKeyXml, byte[] bytes, RsaKeyLengths rsaKeyLength = RsaKeyLengths.Bit2048)
        {
            using (var rsa = new RSACryptoServiceProvider((int)rsaKeyLength))
            {
                rsa.FromXmlString(publicKeyXml);
                return rsa.Encrypt(bytes, DoOAEPPadding);
            }
        }

        public static byte[] Encrypt(byte[] bytes, RSAParameters publicKey, RsaKeyLengths rsaKeyLength = RsaKeyLengths.Bit2048)
        {
            using (var rsa = new RSACryptoServiceProvider((int)rsaKeyLength))
            {
                rsa.ImportParameters(publicKey);
                return rsa.Encrypt(bytes, DoOAEPPadding);
            }
        }

        public static string Decrypt(string encryptedText, string privateKeyXml, RsaKeyLengths rsaKeyLength = RsaKeyLengths.Bit2048)
        {
            var encryptedBytes = Convert.FromBase64String(encryptedText);
            var bytes = Decrypt(encryptedBytes, privateKeyXml, rsaKeyLength);
            var data = Encoding.UTF8.GetString(bytes);
            return data;
        }

        public static string Decrypt(string encryptedText, RSAParameters privateKey, RsaKeyLengths rsaKeyLength = RsaKeyLengths.Bit2048)
        {
            var encryptedBytes = Convert.FromBase64String(encryptedText);
            var bytes = Decrypt(encryptedBytes, privateKey, rsaKeyLength);
            var data = Encoding.UTF8.GetString(bytes);
            return data;
        }

        public static byte[] Decrypt(byte[] encryptedBytes, string privateKeyXml, RsaKeyLengths rsaKeyLength = RsaKeyLengths.Bit2048)
        {
            using (var rsa = new RSACryptoServiceProvider((int)rsaKeyLength))
            {
                rsa.FromXmlString(privateKeyXml);
                byte[] bytes = rsa.Decrypt(encryptedBytes, DoOAEPPadding);
                return bytes;
            }
        }

        public static byte[] Decrypt(byte[] encryptedBytes, RSAParameters privateKey, RsaKeyLengths rsaKeyLength = RsaKeyLengths.Bit2048)
        {
            using (var rsa = new RSACryptoServiceProvider((int)rsaKeyLength))
            {
                rsa.ImportParameters(privateKey);
                byte[] bytes = rsa.Decrypt(encryptedBytes, DoOAEPPadding);
                return bytes;
            }
        }

        public static RsaKeyPair CreatePublicAndPrivateKeyPair(RsaKeyLengths rsaKeyLength = RsaKeyLengths.Bit2048)
        {
            using (var rsa = new RSACryptoServiceProvider((int) rsaKeyLength))
            {
                return new RsaKeyPair
                {
                    PrivateKey = rsa.ToXmlString(true),
                    PublicKey = rsa.ToXmlString(false),
                };
            }
        }

        public static RSAParameters CreatePrivateKeyParams(RsaKeyLengths rsaKeyLength = RsaKeyLengths.Bit2048)
        {
            using (var rsa = new RSACryptoServiceProvider((int)rsaKeyLength))
            {
                return rsa.ExportParameters(includePrivateParameters: true);
            }
        }
    }

    public static class Aes
    {
        public readonly static int KeySize = 256;
        public readonly static int IvSize = 128;

        public static string Encrypt(string text, byte[] aesKey, byte[] iv)
        {
            using (SymmetricAlgorithm aes = new AesCryptoServiceProvider())
            {
                aes.KeySize = KeySize;
                aes.Key = aesKey;
                aes.IV = iv;

                using (var ms = MemoryStreamFactory.GetStream())
                {
                    using (var encryptor = aes.CreateEncryptor(aes.Key, aes.IV))
                    using (var encryptStream = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
                    using (var swEncrypt = new StreamWriter(encryptStream))
                    {
                        swEncrypt.Write(text);
                        swEncrypt.Flush();
                        encryptStream.FlushFinalBlock();

                        var encryptedBody = Convert.ToBase64String(ms.GetBuffer(), 0, (int)ms.Length);
                        return encryptedBody;
                    }
                }
            }
        }

        public static string Decrypt(string encryptedBase64, byte[] aesKey, byte[] iv)
        {
            var bytes = Decrypt(Convert.FromBase64String(encryptedBase64), aesKey, iv);
            return bytes.FromUtf8Bytes();
        }

        public static byte[] Decrypt(byte[] encryptedBytes, byte[] aesKey, byte[] iv)
        {
            using (SymmetricAlgorithm aes = new AesCryptoServiceProvider())
            {
                aes.KeySize = KeySize;
                aes.Key = aesKey;
                aes.IV = iv;

                using (var decryptor = aes.CreateDecryptor(aes.Key, aes.IV))
                using (var ms = MemoryStreamFactory.GetStream(encryptedBytes))
                using (var cryptStream = new CryptoStream(ms, decryptor, CryptoStreamMode.Read))
                {
                    return cryptStream.ReadFully();
                }
            }
        }
    }

}