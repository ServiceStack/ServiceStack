using NUnit.Framework;
using ServiceStack.Messaging;

namespace ServiceStack.Server.Tests.Messaging
{
    [TestFixture]
    public class MqQueueNamesTests
    {
        [Test]
        public void Does_resolve_the_same_default_QueueNames()
        {
            Assert.That(new QueueNames(typeof(HelloIntro)).In, Is.EqualTo("mq:Hello.inq"));
            Assert.That(QueueNames<HelloIntro>.In, Is.EqualTo("mq:Hello.inq"));
        }

        public class TestPrefix { }

        [Test]
        public void Does_resolve_QueueNames_using_QueuePrefix()
        {
            QueueNames.SetQueuePrefix("site1.");

            Assert.That(new QueueNames(typeof(TestPrefix)).In, Is.EqualTo("site1.mq:TestPrefix.inq"));
            Assert.That(QueueNames<TestPrefix>.In, Is.EqualTo("site1.mq:TestPrefix.inq"));

            QueueNames.SetQueuePrefix("");
        }

        public class TestFilter { }

        [Test]
        public void Does_resolve_QueueNames_using_Custom_Filter()
        {
            QueueNames.ResolveQueueNameFn = (typeName, suffix) =>
                "SITE.{0}{1}".Fmt(typeName, suffix.ToUpper());

            Assert.That(new QueueNames(typeof(TestFilter)).In, Is.EqualTo("SITE.TestFilter.INQ"));
            Assert.That(QueueNames<TestFilter>.In, Is.EqualTo("SITE.TestFilter.INQ"));

            QueueNames.ResolveQueueNameFn = QueueNames.ResolveQueueName;
        }

        [Test]
        public void Can_determine_TempQueue()
        {
            var tmpName = QueueNames.GetTempQueueName();
            Assert.That(QueueNames.IsTempQueue(tmpName), Is.True);
        }

        [Test]
        public void Can_determine_TempQueue_with_Custom_QueuePrefix()
        {
            QueueNames.SetQueuePrefix("site1.");

            var tmpName = QueueNames.GetTempQueueName();
            Assert.That(QueueNames.IsTempQueue(tmpName), Is.True);

            QueueNames.SetQueuePrefix("");
        }

        [Test]
        public void Can_determine_TempQueue_with_Custom_QueueNameFm()
        {
            QueueNames.ResolveQueueNameFn = (typeName, suffix) =>
                "SITE.{0}{1}".Fmt(typeName, suffix.ToUpper());

            var tmpName = QueueNames.GetTempQueueName();
            Assert.That(QueueNames.IsTempQueue(tmpName), Is.True);

            QueueNames.ResolveQueueNameFn = QueueNames.ResolveQueueName;
        }
    }
}