using System;
using System.Security.Cryptography;
using System.Xml;
using System.Xml.Linq;
using NUnit.Framework;
using ServiceStack.Text;

namespace ServiceStack.Common.Tests
{
    [TestFixture]
    public class RsaUtilTests
    {
        const string PrivateKeyXml = @"<RSAKeyValue>
<Modulus>sO2GRzjw6Kx9d2+RzsaH+vWINhuB6+zIQ2KKH39ZvV19AMvxmhRyqyYoTYjm7v7P0vNlpqeYYPqDx2sba+rD9GwarBGhG1ZJ2gmB24rGxLJ7G0tATKLWULs558tOjcoS5bVTxoS1XmxVjUw47RiafMSpe0B61cceSadV9LEHkrs=</Modulus>
<Exponent>AQAB</Exponent>
<P>3JLEo1LdH+CVYQHvSNHcob8lkmEWT+pBWqsz90Hk9Fy75fVxTVgDYryvm/SAZoq4HiSDFK8vha6GtaJMXfdjIw==</P>
<Q>zVgzfOKGoXCttLgR7+aQJc47bQe05nE8QcDfmu1RJFMJGzPJokTd6kGjAqZZZIARb25h+q/RvirsaCaQ9j03iQ==</Q>
<DP>XRmd4goBx4i1xGJaq3PZGnRh2W0dS9Hmj+yfXIf1qabSsHduwWSa2TwnKz6CS8XVfPOQWFSxTE2kElpUvXzD3Q==</DP>
<DQ>Bx5nqoyv3ijp3LoE5Sw5ExZzOPRrcRG75QuqtNRFW90FE8xX0ShSCSz9WboqnzFRaWuKOgaeXtleGL49iEvXAQ==</DQ>
<InverseQ>SgkSPIM/CXU//ndT+5XT+IaVeXQa8HMrIPhbvsKsq3v6D7p4yPwOEvMRsRZfFpzGrIO3iRsCBpob2nyHbr4qJw==</InverseQ>
<D>BS4/U1CQhU+fsOKcc2CO1MNhxKvThxP83TRCdR+mggv9wAs4vNlCbk6EuZh7op3lefjUjie0J4rOVwWE3QkXycDz4qH8FHkROmJTBMqITvy7D0xvOAP0KMBrKH6vs0Knc2qzIDkyaV+Ej1xMF6aawZWoXKd9eCCL4HhQoFGAf/E=</D>
</RSAKeyValue>";

        const string PublicKeyXml = @"<RSAKeyValue>
<Modulus>sO2GRzjw6Kx9d2+RzsaH+vWINhuB6+zIQ2KKH39ZvV19AMvxmhRyqyYoTYjm7v7P0vNlpqeYYPqDx2sba+rD9GwarBGhG1ZJ2gmB24rGxLJ7G0tATKLWULs558tOjcoS5bVTxoS1XmxVjUw47RiafMSpe0B61cceSadV9LEHkrs=</Modulus>
<Exponent>AQAB</Exponent>
</RSAKeyValue>";

        const string PublicKeyXmlWithSpaces = @"<RSAKeyValue>
<Modulus>
sO2GRzjw6Kx9d2+RzsaH+vWINhuB6+zIQ2KKH39ZvV19AMvxmhRyqyYoTYjm7v7P0vNlpqeYYPqDx2sba+rD9GwarBGhG1ZJ2gmB24rGxLJ7G0tATKLWULs558tOjcoS5bVTxoS1XmxVjUw47RiafMSpe0B61cceSadV9LEHkrs=
</Modulus><Exponent> AQAB </Exponent>
</RSAKeyValue>";

        [Test]
        public void Export_PrivateKey_to_xml()
        {
            var privateKey = RsaUtils.CreatePrivateKeyParams();
            "PRIVATE KEY".Print();
            privateKey.ToPrivateKeyXml().Print();

            "PUBLIC KEY".Print();
            privateKey.ToPublicKeyXml().Print();
        }

        [Test]
        public void Can_parse_private_key_xml()
        {
            var pk1 = PlatformRsaUtils.ExtractFromXml(PrivateKeyXml);
            using (var rsa = RSA.Create())
            {
                rsa.FromXmlString(PrivateKeyXml);

                var pk2 = rsa.ExportParameters(includePrivateParameters: true);

                Assert.That(pk1.Modulus, Is.EqualTo(pk2.Modulus));
                Assert.That(pk1.Exponent, Is.EqualTo(pk2.Exponent));
                Assert.That(pk1.P, Is.EqualTo(pk2.P));
                Assert.That(pk1.Q, Is.EqualTo(pk2.Q));
                Assert.That(pk1.DP, Is.EqualTo(pk2.DP));
                Assert.That(pk1.DQ, Is.EqualTo(pk2.DQ));
                Assert.That(pk1.InverseQ, Is.EqualTo(pk2.InverseQ));
                Assert.That(pk1.D, Is.EqualTo(pk2.D));
            }
        }

        [Test]
        public void Can_parse_public_key_xml()
        {
            var pk1 = PlatformRsaUtils.ExtractFromXml(PublicKeyXml);
            using (var rsa = RSA.Create())
            {
                rsa.FromXmlString(PublicKeyXml);

                var pk2 = rsa.ExportParameters(includePrivateParameters: false);

                Assert.That(pk1.Modulus, Is.EqualTo(pk2.Modulus));
                Assert.That(pk1.Exponent, Is.EqualTo(pk2.Exponent));
                Assert.That(pk1.P, Is.EqualTo(pk2.P));
                Assert.That(pk1.Q, Is.EqualTo(pk2.Q));
                Assert.That(pk1.DP, Is.EqualTo(pk2.DP));
                Assert.That(pk1.DQ, Is.EqualTo(pk2.DQ));
                Assert.That(pk1.InverseQ, Is.EqualTo(pk2.InverseQ));
                Assert.That(pk1.D, Is.EqualTo(pk2.D));
            }
        }

        [Test]
        public void Can_parse_public_key_xml_with_different_whitespace()
        {
            var pk1 = PlatformRsaUtils.ExtractFromXml(PublicKeyXml);
            var pk2 = PlatformRsaUtils.ExtractFromXml(PublicKeyXmlWithSpaces);
            Assert.That(pk1.Modulus, Is.EqualTo(pk2.Modulus));
            Assert.That(pk1.Exponent, Is.EqualTo(pk2.Exponent));
        }

        private static RSAParameters ExtractRsaParameters(string xml)
        {
            var doc = XDocument.Parse(xml);
            var csp = new RSAParameters();
            var node = ((XElement) doc.FirstNode).FirstNode;
            do
            {
                var el = node as XElement;
                if (el != null)
                {
                    switch (el.Name.LocalName)
                    {
                        case "Modulus":
                            csp.Modulus = Convert.FromBase64String(el.Value);
                            break;
                        case "Exponent":
                            csp.Exponent = Convert.FromBase64String(el.Value);
                            break;
                        case "P":
                            csp.P = Convert.FromBase64String(el.Value);
                            break;
                        case "Q":
                            csp.Q = Convert.FromBase64String(el.Value);
                            break;
                        case "DP":
                            csp.DP = Convert.FromBase64String(el.Value);
                            break;
                        case "DQ":
                            csp.DQ = Convert.FromBase64String(el.Value);
                            break;
                        case "InverseQ":
                            csp.InverseQ = Convert.FromBase64String(el.Value);
                            break;
                        case "D":
                            csp.D = Convert.FromBase64String(el.Value);
                            break;
                    }
                }
            } while ((node = node.NextNode) != null);

            return csp;
        }
    }
}