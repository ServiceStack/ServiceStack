using ServiceStack.Messaging.ActiveMq.Support.Converters;
using NUnit.Framework;

namespace ServiceStack.Messaging.Tests.UnitTests
{
    [TestFixture]
    public class DestinationTypeConverterTests
    {
        [Test]
        public void DestinationTypeConverterTest()
        {
            Assert.AreEqual(DestinationType.Queue, DestinationTypeConverter.Parse(NMS.DestinationType.Queue));
            Assert.AreEqual(DestinationType.Queue, DestinationTypeConverter.Parse(NMS.DestinationType.TemporaryQueue));
            Assert.AreEqual(DestinationType.Topic, DestinationTypeConverter.Parse(NMS.DestinationType.Topic));
            Assert.AreEqual(DestinationType.Topic, DestinationTypeConverter.Parse(NMS.DestinationType.TemporaryTopic));

            Assert.AreEqual(NMS.DestinationType.Queue, DestinationTypeConverter.ToNmsDestinationType(DestinationType.Queue));
            Assert.AreEqual(NMS.DestinationType.Topic, DestinationTypeConverter.ToNmsDestinationType(DestinationType.Topic));
        }
    }
}