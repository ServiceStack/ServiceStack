using ServiceStack.Messaging.ActiveMq.Support.Converters;
using NUnit.Framework;

namespace ServiceStack.Messaging.Tests.UnitTests
{
    [TestFixture]
    public class AcknowledgementModeConverterTests
    {
        [Test]
        public void AcknowledgementModeConverterTest()
        {
            Assert.AreEqual(AcknowledgementMode.AutoAcknowledge,
                            AcknowledgementModeConverter.Parse(NMS.AcknowledgementMode.AutoAcknowledge));
            Assert.AreEqual(AcknowledgementMode.ClientAcknowledge,
                    AcknowledgementModeConverter.Parse(NMS.AcknowledgementMode.ClientAcknowledge));
            Assert.AreEqual(AcknowledgementMode.DuplicatesOkAcknowledge,
                    AcknowledgementModeConverter.Parse(NMS.AcknowledgementMode.DupsOkAcknowledge));
            Assert.AreEqual(AcknowledgementMode.Transactional,
                    AcknowledgementModeConverter.Parse(NMS.AcknowledgementMode.Transactional));

            Assert.AreEqual(NMS.AcknowledgementMode.AutoAcknowledge,
                            AcknowledgementModeConverter.ToNmsAcknowledgementMode(AcknowledgementMode.AutoAcknowledge));
            Assert.AreEqual(NMS.AcknowledgementMode.ClientAcknowledge,
                            AcknowledgementModeConverter.ToNmsAcknowledgementMode(AcknowledgementMode.ClientAcknowledge));
            Assert.AreEqual(NMS.AcknowledgementMode.DupsOkAcknowledge,
                            AcknowledgementModeConverter.ToNmsAcknowledgementMode(AcknowledgementMode.DuplicatesOkAcknowledge));
            Assert.AreEqual(NMS.AcknowledgementMode.Transactional,
                            AcknowledgementModeConverter.ToNmsAcknowledgementMode(AcknowledgementMode.Transactional));
        }
    }
}