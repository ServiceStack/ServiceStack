// Copyright (c) Service Stack LLC. All Rights Reserved.
// License: https://raw.github.com/ServiceStack/ServiceStack/master/license.txt

#if !(PCL || SL5) || __IOS__ || ANDROID

using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using ServiceStack.Text;

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
    public static class RsaUtils
    {
        public static RsaKeyLengths KeyLength = RsaKeyLengths.Bit2048;
        public static RsaKeyPair DefaultKeyPair;
        public static bool DoOAEPPadding = true;

        public static RsaKeyPair CreatePublicAndPrivateKeyPair(RsaKeyLengths rsaKeyLength = RsaKeyLengths.Bit2048)
        {
            using (var rsa = new RSACryptoServiceProvider((int)rsaKeyLength))
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

        public static string FromPrivateRSAParameters(this RSAParameters privateKey)
        {
            using (var rsa = new RSACryptoServiceProvider())
            {
                rsa.ImportParameters(privateKey);
                return rsa.ToXmlString(includePrivateParameters: true);
            }
        }

        public static string FromPublicRSAParameters(this RSAParameters publicKey)
        {
            using (var rsa = new RSACryptoServiceProvider())
            {
                rsa.ImportParameters(publicKey);
                return rsa.ToXmlString(includePrivateParameters: false);
            }
        }

        public static RSAParameters ToPrivateRSAParameters(this string privateKeyXml)
        {
            using (var rsa = new RSACryptoServiceProvider())
            {
                rsa.FromXmlString(privateKeyXml);
                return rsa.ExportParameters(includePrivateParameters: true);
            }
        }

        public static RSAParameters ToPublicRSAParameters(this string publicKeyXml)
        {
            using (var rsa = new RSACryptoServiceProvider())
            {
                rsa.FromXmlString(publicKeyXml);
                return rsa.ExportParameters(includePrivateParameters: false);
            }
        }

        public static string ToPublicKeyXml(this RSAParameters publicKey)
        {
            using (var rsa = new RSACryptoServiceProvider())
            {
                rsa.ImportParameters(publicKey);
                return rsa.ToXmlString(includePrivateParameters: false);
            }
        }

        public static RSAParameters ToPublicRsaParameters(this RSAParameters privateKey)
        {
            using (var rsa = new RSACryptoServiceProvider())
            {
                rsa.ImportParameters(privateKey);
                return rsa.ExportParameters(includePrivateParameters: false);
            }
        }

        public static string ToPrivateKeyXml(this RSAParameters privateKey)
        {
            using (var rsa = new RSACryptoServiceProvider())
            {
                rsa.ImportParameters(privateKey);
                return rsa.ToXmlString(includePrivateParameters: true);
            }
        }

        public static string Encrypt(this string text)
        {
            if (DefaultKeyPair != null)
                return Encrypt(text, DefaultKeyPair.PublicKey, KeyLength);
            else
                throw new ArgumentNullException("No KeyPair given for encryption in CryptUtils");
        }

        public static string Decrypt(this string text)
        {
            if (DefaultKeyPair != null)
                return Decrypt(text, DefaultKeyPair.PrivateKey, KeyLength);
            else
                throw new ArgumentNullException("No KeyPair given for encryption in CryptUtils");
        }

        public static string Encrypt(string text, string publicKeyXml, RsaKeyLengths rsaKeyLength = RsaKeyLengths.Bit2048)
        {
            var bytes = Encoding.UTF8.GetBytes(text);
            var encryptedBytes = Encrypt(bytes, publicKeyXml, rsaKeyLength);
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

        public static byte[] Encrypt(byte[] bytes, string publicKeyXml, RsaKeyLengths rsaKeyLength = RsaKeyLengths.Bit2048)
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

        public static byte[] Authenticate(byte[] dataToSign, RSAParameters privateKey, string hashAlgorithm = "SHA512", RsaKeyLengths rsaKeyLength = RsaKeyLengths.Bit2048)
        {
            using (var rsa = new RSACryptoServiceProvider((int)rsaKeyLength))
            {
                rsa.ImportParameters(privateKey);

                //.NET 4.5 doesn't let you specify padding, defaults to PKCS#1 v1.5 padding
                var signature = rsa.SignData(dataToSign, hashAlgorithm);
                return signature;
            }
        }

        public static bool Verify(byte[] dataToVerify, byte[] signature, RSAParameters publicKey, string hashAlgorithm = "SHA512", RsaKeyLengths rsaKeyLength = RsaKeyLengths.Bit2048)
        {
            using (var rsa = new RSACryptoServiceProvider((int)rsaKeyLength))
            {
                rsa.ImportParameters(publicKey);
                var verified = rsa.VerifyData(dataToVerify, hashAlgorithm, signature);
                return verified;
            }
        }
    }

    public static class HashUtils
    {
        public static HashAlgorithm GetHashAlgorithm(string hashAlgorithm)
        {
            switch (hashAlgorithm)
            {
                case "SHA1":
                    return new SHA1Managed();
                case "SHA256":
                    return new SHA256Managed();
                case "SHA512":
                    return new SHA512Managed();
                default:
                    throw new NotSupportedException(hashAlgorithm);
            }
        }
    }

    public static class AesUtils
    {
        public const int KeySize = 256;
        public const int KeySizeBytes = 256 / 8;
        public const int BlockSize = 128;
        public const int BlockSizeBytes = 128 / 8;

        public static SymmetricAlgorithm CreateSymmetricAlgorithm()
        {
            return new AesCryptoServiceProvider
            {
                KeySize = KeySize,
                BlockSize = BlockSize,
                Mode = CipherMode.CBC,
                Padding = PaddingMode.PKCS7
            };
        }

        public static byte[] CreateKey()
        {
            using (var aes = CreateSymmetricAlgorithm())
            {
                return aes.Key;
            }
        }

        public static byte[] CreateIv()
        {
            using (var aes = CreateSymmetricAlgorithm())
            {
                return aes.IV;
            }
        }

        public static void CreateKeyAndIv(out byte[] cryptKey, out byte[] iv)
        {
            using (var aes = CreateSymmetricAlgorithm())
            {
                cryptKey = aes.Key;
                iv = aes.IV;
            }
        }

        public static void CreateCryptAuthKeysAndIv(out byte[] cryptKey, out byte[] authKey, out byte[] iv)
        {
            using (var aes = CreateSymmetricAlgorithm())
            {
                cryptKey = aes.Key;
                iv = aes.IV;
            }
            using (var aes = CreateSymmetricAlgorithm())
            {
                authKey = aes.Key;
            }
        }

        public static string Encrypt(string text, byte[] cryptKey, byte[] iv)
        {
            var encBytes = Encrypt(text.ToUtf8Bytes(), cryptKey, iv);
            return Convert.ToBase64String(encBytes);
        }

        public static byte[] Encrypt(byte[] bytesToEncrypt, byte[] cryptKey, byte[] iv)
        {
            using (var aes = CreateSymmetricAlgorithm())
            using (var encrypter = aes.CreateEncryptor(cryptKey, iv))
            using (var cipherStream = MemoryStreamFactory.GetStream())
            {
                using (var cryptoStream = new CryptoStream(cipherStream, encrypter, CryptoStreamMode.Write))
                using (var binaryWriter = new BinaryWriter(cryptoStream))
                {
                    binaryWriter.Write(bytesToEncrypt);
                }
                return cipherStream.ToArray();
            }
        }

        public static string Decrypt(string encryptedBase64, byte[] cryptKey, byte[] iv)
        {
            var bytes = Decrypt(Convert.FromBase64String(encryptedBase64), cryptKey, iv);
            return bytes.FromUtf8Bytes();
        }

        public static byte[] Decrypt(byte[] encryptedBytes, byte[] cryptKey, byte[] iv)
        {
            using (var aes = CreateSymmetricAlgorithm())
            using (var decryptor = aes.CreateDecryptor(cryptKey, iv))
            using (var ms = MemoryStreamFactory.GetStream(encryptedBytes))
            using (var cryptStream = new CryptoStream(ms, decryptor, CryptoStreamMode.Read))
            {
                return cryptStream.ReadFully();
            }
        }
    }

    /*
     * Original Source:
     * This work (Modern Encryption of a String C#, by James Tuley), 
     * identified by James Tuley, is free of known copyright restrictions.
     * https://gist.github.com/4336842
     * http://creativecommons.org/publicdomain/mark/1.0/ 
     */
    public static class HmacUtils
    {
        public const int KeySize = 256;
        public const int KeySizeBytes = 256 / 8;

        public static HMAC CreateHashAlgorithm(byte[] authKey)
        {
            return new HMACSHA256(authKey);
        }

        public static byte[] Authenticate(byte[] encryptedBytes, byte[] authKey, byte[] iv)
        {
            using (var hmac = CreateHashAlgorithm(authKey))
            using (var ms = MemoryStreamFactory.GetStream())
            {
                using (var writer = new BinaryWriter(ms))
                {
                    //Prepend IV
                    writer.Write(iv);
                    //Write Ciphertext
                    writer.Write(encryptedBytes);
                    writer.Flush();

                    //Authenticate all data
                    var tag = hmac.ComputeHash(ms.ToArray());
                    //Postpend tag
                    writer.Write(tag);
                }
                return ms.ToArray();
            }
        }

        public static bool Verify(byte[] authEncryptedBytes, byte[] authKey)
        {
            if (authKey == null || authKey.Length != KeySizeBytes)
                throw new ArgumentException("AuthKey needs to be {0} bit!".Fmt(KeySize), "authKey");

            if (authEncryptedBytes == null || authEncryptedBytes.Length == 0)
                throw new ArgumentException("Encrypted Message Required!", "authEncryptedBytes");

            using (var hmac = CreateHashAlgorithm(authKey))
            {
                var sentTag = new byte[KeySizeBytes];
                //Calculate Tag
                var calcTag = hmac.ComputeHash(authEncryptedBytes, 0, authEncryptedBytes.Length - sentTag.Length);
                const int ivLength = AesUtils.BlockSizeBytes;

                //return false if message length is too small
                if (authEncryptedBytes.Length < sentTag.Length + ivLength)
                    return false;

                //Grab Sent Tag
                Buffer.BlockCopy(authEncryptedBytes, authEncryptedBytes.Length - sentTag.Length, sentTag, 0, sentTag.Length);

                //Compare Tag with constant time comparison
                var compare = 0;
                for (var i = 0; i < sentTag.Length; i++)
                    compare |= sentTag[i] ^ calcTag[i];

                //return false if message doesn't authenticate
                if (compare != 0)
                    return false;
            }

            return true; //Haz Success!
        }

        public static byte[] DecryptAuthenticated(byte[] authEncryptedBytes, byte[] cryptKey)
        {
            if (cryptKey == null || cryptKey.Length != KeySizeBytes)
                throw new ArgumentException("CryptKey needs to be {0} bit!".Fmt(KeySize), "cryptKey");

            //Grab IV from message
            var iv = new byte[AesUtils.BlockSizeBytes];
            Buffer.BlockCopy(authEncryptedBytes, 0, iv, 0, iv.Length);

            using (var aes = AesUtils.CreateSymmetricAlgorithm())
            {
                using (var decrypter = aes.CreateDecryptor(cryptKey, iv))
                using (var decryptedStream = MemoryStreamFactory.GetStream())
                {
                    using (var decrypterStream = new CryptoStream(decryptedStream, decrypter, CryptoStreamMode.Write))
                    using (var writer = new BinaryWriter(decrypterStream))
                    {
                        //Decrypt Cipher Text from Message
                        writer.Write(
                            authEncryptedBytes,
                            iv.Length,
                            authEncryptedBytes.Length - iv.Length - KeySizeBytes);
                    }

                    return decryptedStream.ToArray();
                }
            }
        }
    }

    [Obsolete("Use RsaUtils.* static class")]
    public class CryptUtils
    {
        public static string Encrypt(string publicKeyXml, string data, RsaKeyLengths rsaKeyLength = RsaKeyLengths.Bit2048)
        {
            return RsaUtils.Encrypt(data, publicKeyXml, rsaKeyLength);
        }

        public static string Decrypt(string privateKeyXml, string encryptedData, RsaKeyLengths rsaKeyLength = RsaKeyLengths.Bit2048)
        {
            return RsaUtils.Encrypt(encryptedData, privateKeyXml, rsaKeyLength);
        }

        public static RsaKeyPair CreatePublicAndPrivateKeyPair(RsaKeyLengths rsaKeyLength = RsaKeyLengths.Bit2048)
        {
            return RsaUtils.CreatePublicAndPrivateKeyPair(rsaKeyLength);
        }
    }
}
#endif