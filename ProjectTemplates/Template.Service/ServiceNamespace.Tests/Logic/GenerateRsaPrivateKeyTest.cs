/*
// $Id$
//
// Revision      : $Revision$
// Modified Date : $LastChangedDate$
// Modified By   : $LastChangedBy$
//
// (c) Copyright 2008 Digital Distribution Networks Ltd
*/

using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Ddn.Common.Services.Crypto;
using Ddn.Logging.Log4Net;
using NUnit.Framework;
using NUnit.Framework.SyntaxHelpers;
using Platform.Text;

namespace @ServiceNamespace@.Tests.Logic
{
	[TestFixture]
	public class GenerateRsaPrivateKeyTest
	{
		[Test]
		public void GenerateRsaPrivateKey()
		{
			var rsaPrivateKey = new RsaPrivateKey(new Log4NetFactory(true), 512);

			var key = string.Format(
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

			Console.WriteLine("            Key        = {0}", key);
			Console.WriteLine("            PrivateKey = {0}", rsaPrivateKey.PrivateKeyXml);
			Console.WriteLine("            PublicKey  = {0}", rsaPrivateKey.PublicKeyXml);

			// Uncomment to dump XML private and public key string to text files
			//DumpText(rsaPrivateKey.PrivateKeyXml, "RsaPrivateKey.txt");
			//DumpText(rsaPrivateKey.PublicKeyXml, "RsaPublicKey.txt");

			var xmlFriendlyKey = MakeXmlKeyFriendly(key);
			var xmlFriendlyPrivateKey = MakeXmlKeyFriendly(rsaPrivateKey.PrivateKeyXml);
			var xmlFriendlyPublicKey = MakeXmlKeyFriendly(rsaPrivateKey.PublicKeyXml);

			Console.WriteLine("XmlFriendly Key        = {0}", xmlFriendlyKey);
			Console.WriteLine("XmlFriendly PrivateKey = {0}", xmlFriendlyPrivateKey);
			Console.WriteLine("XmlFriendly PublicKey  = {0}", xmlFriendlyPublicKey);

			// Uncomment to dump XML friendly private and public key string to text files
			//DumpText(xmlFriendlyPrivateKey, "XmlFriendlyRsaPrivateKey.txt");
			//DumpText(xmlFriendlyPublicKey, "XmlFriendlyRsaPublicKey.txt");

			Assert.That(string.IsNullOrEmpty(rsaPrivateKey.PrivateKeyXml), Is.False);
			Assert.That(string.IsNullOrEmpty(rsaPrivateKey.PublicKeyXml), Is.False);
		}

		[Test]
		public void Output()
		{
			var text = "password";
			var publicKeyFormat = "<RSAKeyValue><Modulus>szsz1a/03VzL4vmPzX1XEnwQbDlJOuR/Z/CrgwRYofigaoXWVjnmgTsqHbwy/G4h6RsiKYlcrcsP5dO9MudbFw==</Modulus><Exponent>AQAB</Exponent></RSAKeyValue>";

			for (int i = 0; i < 5; i++)
			{
				var rsa = new RSACryptoServiceProvider();
				rsa.FromXmlString(publicKeyFormat);
				var ps = rsa.ExportParameters(false);

				Console.WriteLine("Mod: {0}, Exp: {1}, Q:{2}",Convert.ToBase64String(ps.Modulus), Convert.ToBase64String(ps.Exponent)	, ps.Q);
				
				var encryptedText1 = rsa.Encrypt(Encoding.ASCII.GetBytes(text), false);

				Console.WriteLine("{0} = {1}", "AQAB", Convert.ToBase64String(encryptedText1))	;
			}
				
		}

		private static string MakeXmlKeyFriendly(string xml)
		{
			return xml.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;").Replace("\"", "&quot;").Replace("'", "&apos;");
		}

		private static void DumpText(string keyXml, string fileName)
		{
			var fileDumpName = Environment.CurrentDirectory + "\\" + fileName;
			using (TextWriter writer = new StreamWriter(fileDumpName, false))
			{
				writer.Write(keyXml);
			}
		}
	}
}