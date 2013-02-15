using System;
using NUnit.Framework;
using ServiceStack.Common.Tests.Models;
using ServiceStack.Common.Utils;
using ServiceStack.DataAnnotations;
using ServiceStack.DesignPatterns.Model;

namespace ServiceStack.Common.Tests
{
    [TestFixture]
    public class IdUtilsTests
    {
        private const int IntValue = 1;
        private const string StringValue = "A";

        public class HasIntId : IHasIntId
        {
            public int Id
            {
                get { return IntValue; }
            }
        }

        public class HasGenericIdInt : IHasId<int>
        {
            public int Id
            {
                get { return IntValue; }
            }
        }

        public class HasGenericIdString : IHasId<string>
        {
            public string Id
            {
                get { return StringValue; }
            }
        }

        public class HasIdProperty
        {
            public int Id
            {
                get { return IntValue; }
            }
        }

        public class HasIdStringProperty
        {
            public string Id
            {
                get { return StringValue; }
            }
        }

        public class HasIdCustomStringProperty
        {
            public string CustomId
            {
                get { return StringValue; }
            }
        }

        public class HasIdCustomIntProperty
        {
            public int CustomId
            {
                get { return IntValue; }
            }
        }

        public class HasPrimaryKeyAttribute
        {
            [PrimaryKey]
            public int CustomId
            {
                get { return IntValue; }
            }
        }

        [Test]
        public void Can_get_if_HasIntId()
        {
            Assert.That(new HasIntId().GetId(), Is.EqualTo(IntValue));
        }

        [Test]
        public void Can_get_if_HasGenericIdInt()
        {
            Assert.That(new HasGenericIdInt().GetId(), Is.EqualTo(IntValue));
        }

        [Test]
        public void Can_get_if_HasGenericIdString()
        {
            Assert.That(new HasGenericIdString().GetId(), Is.EqualTo(StringValue));
        }

        [Test]
        public void Can_get_if_HasIdProperty()
        {
            Assert.That(new HasIdProperty().GetId(), Is.EqualTo(IntValue));
        }

        [Test]
        public void Can_get_if_HasIdStringProperty()
        {
            Assert.That(new HasIdStringProperty().GetId(), Is.EqualTo(StringValue));
        }

        [Test]
        public void Can_get_if_HasIdCustomStringProperty()
        {
            ModelConfig<HasIdCustomStringProperty>.Id(x => x.CustomId);

            Assert.That(new HasIdCustomStringProperty().GetId(), Is.EqualTo(StringValue));
        }

        [Test]
        public void Can_get_if_HasIdCustomIntProperty()
        {
            ModelConfig<HasIdCustomIntProperty>.Id(x => x.CustomId);

            Assert.That(new HasIdCustomIntProperty().GetId(), Is.EqualTo(IntValue));
        }

        [Test]
        public void Can_get_if_HasPrimaryKeyAttribute()
        {
            Assert.That(new HasPrimaryKeyAttribute().GetId(), Is.EqualTo(IntValue));
        }

    }
}