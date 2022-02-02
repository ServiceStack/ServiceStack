using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServiceStack.Text.Tests.JsonTests
{
    [TestFixture]
    class InheritanceTest
    {
        public class BaseClass
        {
            protected string base64Value = string.Empty;
            public string Value { get { return this.base64Value; } private set { this.base64Value = value; } }

            public void Set(string val)
            {
                this.base64Value = val;
            }
        }

        public class Derived : BaseClass
        {
        }

        [Test]
        public void Can_deserialize_private_set_in_base_class()
        {
            var derived = new Derived();
            derived.Set("test");

            string serialized = derived.ToJson();
            var deserialized = serialized.FromJson<Derived>();

            Assert.That(deserialized.Value, Is.EqualTo("test"));
        }
    }
}
