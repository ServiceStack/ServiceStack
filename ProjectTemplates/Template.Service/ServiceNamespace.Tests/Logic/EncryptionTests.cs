using System;
using System.Security.Cryptography;
using System.Text;
using NUnit.Framework;
using Platform.Text;

namespace @ServiceNamespace@.Tests.Logic
{
    [TestFixture]
    public class EncryptionTests 
    {
		 public static RSACryptoServiceProvider TestPrivateKey
		 {
			 get
			 {
				 var privateKeyXml = string.Format(
					  "<RSAKeyValue><Modulus>{0}</Modulus><Exponent>{1}</Exponent><P>{2}</P><Q>{3}</Q><DP>{4}</DP><DQ>{5}</DQ><InverseQ>{6}</InverseQ><D>{7}</D></RSAKeyValue>",
					  "dUWKXF2sihVNwLhnMK5zxaGKBh4KZqmyiww1a3bbHzSinP+bPFKROvtap44IzVe+1sjzc8xDRkcp9gMXx2+gfw==", // modulus
					  "AQAB", // exponent
					  "uB1BBJoV7GWl7igS4+X5etWFJVhOq+yEaxVBfr9FxV0=", // P
					  "ow8s1vWkpXD3mN98R3xz8xvzy/HHs0km3ctDZHP844s=", // Q
					  "D6DUEwrtT3q1YgjeyZ+M1MNpIOllDCzwdJKCU7rytjU=", // DP
					  "EAr+LmgcuupScggLAj2Mau7lHbu8GjeoS0okZ03CI+E=", // DQ
					  "iQKvrfCEepEgDp5KXwUpPii+N6i3ournLkdgbKjd/fE=", // InverseQ
					  "TtOTkUqV86smPGi3VA2vXCSdNkdzDlb64GQwdC/MUkiRuKT/hfAVh01aNq2F/5vJDJqfGZJd3pwoy92tUfx34Q==" // D
					  );
				 var rsa = new RSACryptoServiceProvider();
				 rsa.FromXmlString(privateKeyXml);
				 return rsa;
			 }
		 }

        [Test]
        public void create_encryption_cyphers_hex()
        {
            var text = "password";
            var data = Encoding.ASCII.GetBytes(text);
            var privateKey = TestPrivateKey;
            for (int i = 0; i < 5; i++)
            {
                var encryptedBytes = privateKey.Encrypt(data, false);
                Console.WriteLine("{0}: {1}", i, TextConversion.ToHexString(encryptedBytes));
            }
        }

        [Test]
        public void decrypt_text()
        {
            var encryptedHex = "29db439f948e6b256abca595f4d09a1871bf75440c8452f9535f53be492e317553ce754510e041e60b4c1d29b1ac90453f928d5c9e7853750e6c1e9c7e0d3374";
            var data = TextConversion.FromHexString(encryptedHex);
            var privateKey = TestPrivateKey;
            var result = Encoding.ASCII.GetString(privateKey.Decrypt(data, false));
            Console.WriteLine(result);
        }

    }
}