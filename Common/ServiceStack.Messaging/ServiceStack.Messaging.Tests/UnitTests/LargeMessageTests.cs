using System.IO;
using NUnit.Framework;

namespace ServiceStack.Messaging.Tests.UnitTests
{
    [TestFixture]
    public class LargeMessageTests : UnitTestCaseBase
    {

        [Test]
        public void SendLargeXmlOneWayTest()
        {
            using (IConnection connection = CreateNewConnection())
            {
                using (IReplyClient client = connection.CreateReplyClient(DestinationQueue))
                {
                    ITextMessage message = connection.CreateTextMessage(File.ReadAllText(LargeXmlPath));
                    client.SendOneWay(message);
                    AssertNmsState(1, 1, 1, 0);
                    Assert.AreEqual(1, MockNmsSession.TextMessages.Count);
                    Assert.AreEqual(1, MockNmsProducer.SentMessages.Count);
                }
            }
            AssertAllResourcesDisposed(MockFactory);
        }

    }
}
