// Copyright (c) Service Stack LLC. All Rights Reserved.
// License: https://raw.github.com/ServiceStack/ServiceStack/master/license.txt

#if !(PCL || SL5 || NETSTANDARD1_1) || __IOS__ || ANDROID

using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Xml;
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

        private static RSA CreateRsa(RsaKeyLengths rsaKeyLength)
        {
            var rsa = RSA.Create();
            rsa.KeySize = (int)rsaKeyLength;
            return rsa;
        }

        public static RsaKeyPair CreatePublicAndPrivateKeyPair(RsaKeyLengths rsaKeyLength = RsaKeyLengths.Bit2048)
        {
            using (var rsa = CreateRsa(rsaKeyLength))
            {
                return new RsaKeyPair
                {
                    PrivateKey = rsa.ToXmlString(includePrivateParameters: true),
                    PublicKey = rsa.ToXmlString(includePrivateParameters: false),
                };
            }
        }

        public static RSAParameters CreatePrivateKeyParams(RsaKeyLengths rsaKeyLength = RsaKeyLengths.Bit2048)
        {
            using (var rsa = CreateRsa(rsaKeyLength))
            {
                return rsa.ExportParameters(includePrivateParameters: true);
            }
        }

        public static string FromPrivateRSAParameters(this RSAParameters privateKey)
        {
            using (var rsa = RSA.Create())
            {
                rsa.ImportParameters(privateKey);
                return rsa.ToXmlString(includePrivateParameters: true);
            }
        }

        public static string FromPublicRSAParameters(this RSAParameters publicKey)
        {
            using (var rsa = RSA.Create())
            {
                rsa.ImportParameters(publicKey);
                return rsa.ToXmlString(includePrivateParameters: false);
            }
        }

        public static RSAParameters ToPrivateRSAParameters(this string privateKeyXml)
        {
            using (var rsa = RSA.Create())
            {
                rsa.FromXmlString(privateKeyXml);
                return rsa.ExportParameters(includePrivateParameters: true);
            }
        }

        public static RSAParameters ToPublicRSAParameters(this string publicKeyXml)
        {
            using (var rsa = RSA.Create())
            {
                rsa.FromXmlString(publicKeyXml);
                return rsa.ExportParameters(includePrivateParameters: false);
            }
        }

        public static string ToPublicKeyXml(this RSAParameters publicKey)
        {
            using (var rsa = RSA.Create())
            {
                rsa.ImportParameters(publicKey);
                return rsa.ToXmlString(includePrivateParameters: false);
            }
        }

        public static RSAParameters ToPublicRsaParameters(this RSAParameters privateKey)
        {
            using (var rsa = RSA.Create())
            {
                rsa.ImportParameters(privateKey);
                return rsa.ExportParameters(includePrivateParameters: false);
            }
        }

        public static string ToPrivateKeyXml(this RSAParameters privateKey)
        {
            using (var rsa = RSA.Create())
            {
                rsa.ImportParameters(privateKey);
                return rsa.ToXmlString(includePrivateParameters: true);
            }
        }

        public static string Encrypt(this string text)
        {
            if (DefaultKeyPair != null)
                return Encrypt(text, DefaultKeyPair.PublicKey, KeyLength);

            throw new ArgumentNullException("DefaultKeyPair", "No KeyPair given for encryption in CryptUtils");
        }

        public static string Decrypt(this string text)
        {
            if (DefaultKeyPair != null)
                return Decrypt(text, DefaultKeyPair.PrivateKey, KeyLength);
            else
                throw new ArgumentNullException("DefaultKeyPair", "No KeyPair given for encryption in CryptUtils");
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
            using (var rsa = CreateRsa(rsaKeyLength))
            {
                rsa.FromXmlString(publicKeyXml);
                return rsa.Encrypt(bytes);
            }
        }

        public static byte[] Encrypt(byte[] bytes, RSAParameters publicKey, RsaKeyLengths rsaKeyLength = RsaKeyLengths.Bit2048)
        {
            using (var rsa = CreateRsa(rsaKeyLength))
            {
                rsa.ImportParameters(publicKey);
                return rsa.Encrypt(bytes);
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
            using (var rsa = CreateRsa(rsaKeyLength))
            {
                rsa.FromXmlString(privateKeyXml);
                byte[] bytes = rsa.Decrypt(encryptedBytes);
                return bytes;
            }
        }

        public static byte[] Decrypt(byte[] encryptedBytes, RSAParameters privateKey, RsaKeyLengths rsaKeyLength = RsaKeyLengths.Bit2048)
        {
            using (var rsa = CreateRsa(rsaKeyLength))
            {
                rsa.ImportParameters(privateKey);
                byte[] bytes = rsa.Decrypt(encryptedBytes);
                return bytes;
            }
        }

        public static byte[] Authenticate(byte[] dataToSign, RSAParameters privateKey, string hashAlgorithm = "SHA512", RsaKeyLengths rsaKeyLength = RsaKeyLengths.Bit2048)
        {
            using (var rsa = CreateRsa(rsaKeyLength))
            {
                rsa.ImportParameters(privateKey);

                //.NET 4.5 doesn't let you specify padding, defaults to PKCS#1 v1.5 padding
                var signature = rsa.SignData(dataToSign, hashAlgorithm);
                return signature;
            }
        }

        public static bool Verify(byte[] dataToVerify, byte[] signature, RSAParameters publicKey, string hashAlgorithm = "SHA512", RsaKeyLengths rsaKeyLength = RsaKeyLengths.Bit2048)
        {
            using (var rsa = CreateRsa(rsaKeyLength))
            {
                rsa.ImportParameters(publicKey);
                var verified = rsa.VerifyData(dataToVerify, signature, hashAlgorithm);
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
                    return SHA1.Create();
                case "SHA256":
                    return SHA256.Create();
                case "SHA512":
                    return SHA512.Create();
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
            var aes = Aes.Create();
            aes.KeySize = KeySize;
            aes.BlockSize = BlockSize;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;
            return aes;
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
                throw new ArgumentException($"AuthKey needs to be {KeySize} bits", nameof(authKey));

            if (authEncryptedBytes == null || authEncryptedBytes.Length == 0)
                throw new ArgumentException("Encrypted Message Required!", nameof(authEncryptedBytes));

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
                throw new ArgumentException($"CryptKey needs to be {KeySize} bits", nameof(cryptKey));

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

    public static class PlatformRsaUtils
    {
#if NETSTANDARD1_6
        public static void FromXmlString(this RSA rsa, string xml)
        {
            var csp = ExtractFromXml(xml);
            rsa.ImportParameters(csp);
        }

        public static string ToXmlString(this RSA rsa, bool includePrivateParameters)
        {
            return ExportToXml(rsa.ExportParameters(includePrivateParameters), includePrivateParameters);
        }

        public static HashAlgorithmName ToHashAlgorithmName(string hashAlgorithm)
        {
            switch (hashAlgorithm.ToUpper())
            {
                case "MD5":
                    return HashAlgorithmName.MD5;
                case "SHA1":
                     return HashAlgorithmName.SHA1;
                case "SHA256":
                     return HashAlgorithmName.SHA256;
                case "SHA384":
                     return HashAlgorithmName.SHA384;
                case "SHA512":
                     return HashAlgorithmName.SHA512;
                default:
                     throw new NotImplementedException(hashAlgorithm);
            }
        }
#endif

        public static byte[] Encrypt(this RSA rsa, byte[] bytes)
        {
#if !NETSTANDARD1_6
            return ((RSACryptoServiceProvider)rsa).Encrypt(bytes, RsaUtils.DoOAEPPadding);
#else
            return rsa.Encrypt(bytes, RSAEncryptionPadding.OaepSHA1);
#endif
        }

        public static byte[] Decrypt(this RSA rsa, byte[] bytes)
        {
#if !NETSTANDARD1_6
            return ((RSACryptoServiceProvider)rsa).Decrypt(bytes, RsaUtils.DoOAEPPadding);
#else
            return rsa.Decrypt(bytes, RSAEncryptionPadding.OaepSHA1);
#endif
        }

        public static byte[] SignData(this RSA rsa, byte[] bytes, string hashAlgorithm)
        {
#if !NETSTANDARD1_6
            return ((RSACryptoServiceProvider)rsa).SignData(bytes, hashAlgorithm);
#else
            return rsa.SignData(bytes, ToHashAlgorithmName(hashAlgorithm), RSASignaturePadding.Pkcs1);
#endif
        }

        public static bool VerifyData(this RSA rsa, byte[] bytes, byte[] signature, string hashAlgorithm)
        {
#if !NETSTANDARD1_6
            return ((RSACryptoServiceProvider)rsa).VerifyData(bytes, hashAlgorithm, signature);
#else
            return rsa.VerifyData(bytes, signature, ToHashAlgorithmName(hashAlgorithm), RSASignaturePadding.Pkcs1);
#endif
        }

        public static RSAParameters ExtractFromXml(string xml)
        {
            var csp = new RSAParameters();
            using (var reader = XmlReader.Create(new StringReader(xml)))
            {
                while (reader.Read())
                {
                    if (reader.NodeType != XmlNodeType.Element)
                        continue;

                    var elName = reader.Name;
                    if (elName == "RSAKeyValue")
                        continue;

                    do {
                        reader.Read();
                    } while (reader.NodeType != XmlNodeType.Text && reader.NodeType != XmlNodeType.EndElement);

                    if (reader.NodeType == XmlNodeType.EndElement)
                        continue;

                    var value = reader.Value;
                    switch (elName)
                    {
                        case "Modulus":
                            csp.Modulus = Convert.FromBase64String(value);
                            break;
                        case "Exponent":
                            csp.Exponent = Convert.FromBase64String(value);
                            break;
                        case "P":
                            csp.P = Convert.FromBase64String(value);
                            break;
                        case "Q":
                            csp.Q = Convert.FromBase64String(value);
                            break;
                        case "DP":
                            csp.DP = Convert.FromBase64String(value);
                            break;
                        case "DQ":
                            csp.DQ = Convert.FromBase64String(value);
                            break;
                        case "InverseQ":
                            csp.InverseQ = Convert.FromBase64String(value);
                            break;
                        case "D":
                            csp.D = Convert.FromBase64String(value);
                            break;
                    }
                }

                return csp;
            }
        }

        public static string ExportToXml(RSAParameters csp, bool includePrivateParameters)
        {
            var sb = StringBuilderCache.Allocate();
            sb.Append("<RSAKeyValue>");

            sb.Append("<Modulus>").Append(Convert.ToBase64String(csp.Modulus)).Append("</Modulus>");
            sb.Append("<Exponent>").Append(Convert.ToBase64String(csp.Exponent)).Append("</Exponent>");

            if (includePrivateParameters)
            {
                sb.Append("<P>").Append(Convert.ToBase64String(csp.P)).Append("</P>");
                sb.Append("<Q>").Append(Convert.ToBase64String(csp.Q)).Append("</Q>");
                sb.Append("<DP>").Append(Convert.ToBase64String(csp.DP)).Append("</DP>");
                sb.Append("<DQ>").Append(Convert.ToBase64String(csp.DQ)).Append("</DQ>");
                sb.Append("<InverseQ>").Append(Convert.ToBase64String(csp.Modulus)).Append("</InverseQ>");
                sb.Append("<D>").Append(Convert.ToBase64String(csp.Modulus)).Append("</D>");
            }

            sb.Append("</RSAKeyValue>");
            var xml = StringBuilderCache.ReturnAndFree(sb);
            return xml;
        }
    }
}
#endif