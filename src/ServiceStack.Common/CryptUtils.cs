using System;
using System.Collections;
using System.Security.Cryptography;
using System.Text;

namespace ServiceStack.Common
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
    /// http://andrewlocatelliwoodcock.com/2011/08/01/implementing-rsa-asymmetric-public-private-key-encryption-in-c-encrypting-under-the-public-key/
    /// </summary>
    public static class CryptUtils
    {

        public static string Encrypt(this string data)
        {
            if (KeyPair != null)
                return Encrypt(KeyPair.PublicKey, data, Length);
            else throw new ArgumentNullException("No KeyPair given for encryption in CryptUtils");
        }

        public static string Decrypt(this string data)
        {
            if (KeyPair !=null)
                return Decrypt(KeyPair.PrivateKey, data, Length);
            else throw new ArgumentNullException("No KeyPair given for encryption in CryptUtils");
        }

        public static RsaKeyLengths Length;
        public static RsaKeyPair KeyPair;
        

        /// <summary>
        /// Encrypt an arbitrary string of data under the supplied public key
        /// </summary>
        /// <param name="publicKey">The public key to encrypt under</param>
        /// <param name="data">The data to encrypt</param>
        /// <param name="length">The bit length or strength of the public key: 1024, 2048 or 4096 bits. This must match the 
        /// value actually used to create the publicKey</param>
        /// <returns></returns>
        public static string Encrypt(string publicKey, string data, RsaKeyLengths length = RsaKeyLengths.Bit2048)
        {
            // full array of bytes to encrypt
            byte[] bytesToEncrypt;

            // worker byte array
            byte[] block;

            // encrypted bytes
            byte[] encryptedBytes;

            // length of bytesToEncrypt
            var dataLength = 0;

            // number of bytes in key                
            var keySize = 0;

            // maximum block length to encrypt          
            var maxLength = 0;

            // how many blocks must we encrypt to encrypt entire message?
            var iterations = 0;

            // the encrypted data
            var encryptedData = new StringBuilder();

            // instantiate the crypto provider with the correct key length
            var rsaCryptoServiceProvider = new RSACryptoServiceProvider((int)length);

            // initialize the RSA object from the given public key
            rsaCryptoServiceProvider.FromXmlString(publicKey);

            // convert data to byte array
            bytesToEncrypt = Encoding.Unicode.GetBytes(data);

            // get length of byte array
            dataLength = bytesToEncrypt.Length;

            // convert length of key from bits to bytes
            keySize = (int)length / 8;

            // .NET RSACryptoServiceProvider uses SHA1 Hash function
            // use this to work out the maximum length to encrypt per block
            maxLength = ((keySize - 2) - (2 * SHA1.Create().ComputeHash(bytesToEncrypt).Length));

            // how many blocks do we need to encrypt?
            iterations = dataLength / maxLength;

            // encrypt block by block
            for (int index = 0; index <= iterations; index++)
            {
                // is there more than one full block of data left to encrypt?
                if ((dataLength - maxLength * index) > maxLength)
                {
                    block = new byte[maxLength];
                }
                else
                {
                    block = new byte[dataLength - maxLength * index];
                }

                // copy the required number of bytes from the array of bytes to encrypt to our worker array
                Buffer.BlockCopy(bytesToEncrypt, maxLength * index, block, 0, block.Length);

                // encrypt the current worker array block of bytes
                encryptedBytes = rsaCryptoServiceProvider.Encrypt(block, true);

                // RSACryptoServiceProvider reverses the order of encrypted bytesToEncrypt after encryption and before decryption.
                // Undo this reversal for compatibility with other implementations
                Array.Reverse(encryptedBytes);

                // convert to base 64 string
                encryptedData.Append(Convert.ToBase64String(encryptedBytes));
            }

            return encryptedData.ToString();
        }

        public static string Decrypt(string privateKey, string data, RsaKeyLengths length = RsaKeyLengths.Bit2048)
        {
            var dwKeySize = (int)length;
            // TODO: Add Proper Exception Handlers
            var rsaCryptoServiceProvider = new RSACryptoServiceProvider(dwKeySize);
            rsaCryptoServiceProvider.FromXmlString(privateKey);

            int base64BlockSize = ((dwKeySize / 8) % 3 != 0) ?
              (((dwKeySize / 8) / 3) * 4) + 4 : ((dwKeySize / 8) / 3) * 4;

            int iterations = data.Length / base64BlockSize;

            var arrayList = new ArrayList();
            for (int i = 0; i < iterations; i++)
            {
                byte[] encryptedBytes = Convert.FromBase64String(
                     data.Substring(base64BlockSize * i, base64BlockSize));
                // Be aware the RSACryptoServiceProvider reverses the order of 
                // encrypted bytes after encryption and before decryption.
                // If you do not require compatibility with Microsoft Cryptographic 
                // API (CAPI) and/or other vendors.
                // Comment out the next line and the corresponding one in the 
                // EncryptString function.
                Array.Reverse(encryptedBytes);
                arrayList.AddRange(rsaCryptoServiceProvider.Decrypt(encryptedBytes, true));
            }

            return Encoding.Unicode.GetString(arrayList.ToArray(typeof(byte)) as byte[]);
        }

        public static RsaKeyPair CreatePublicAndPrivateKeyPair(RsaKeyLengths length = RsaKeyLengths.Bit2048)
        {
            var rsaProvider = new RSACryptoServiceProvider((int)length);
            return new RsaKeyPair
            {
                PrivateKey = rsaProvider.ToXmlString(true),
                PublicKey = rsaProvider.ToXmlString(false),
            };
        }

        /// <summary>
        /// Create Public and Private Key Pair based on settings already in static class.
        /// </summary>        
        /// <returns>RsaKeyPair</returns>
        public static RsaKeyPair CreatePublicAndPrivateKeyPair()
        {
            var rsaProvider = new RSACryptoServiceProvider((int)Length);            
            return new RsaKeyPair
            {
                PrivateKey = rsaProvider.ToXmlString(true),
                PublicKey = rsaProvider.ToXmlString(false),
            };
        }
    }
}