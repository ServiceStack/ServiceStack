using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using NUnit.Framework;
using ServiceStack.Text.Common;

namespace ServiceStack.Text.Tests.JsonTests
{
    [TestFixture]
    public class PolymorphicInstanceTest
    {

        [SetUp]
        public void SetUp()
        {
            JsConfig.Reset();
            JsConfig<ICat>.ExcludeTypeInfo = false;
        }

        [Test]
        public void Can_deserialise_polymorphic_dog_exact()
        {
            var dog =
                JsonSerializer.DeserializeFromString<Dog>(
                    @"{""__type"":"""
                    + typeof(Dog).ToTypeString()
                    + @""",""Name"":""Fido""}");

            Assert.That(dog.Name, Is.EqualTo(@"Fido"));

        }

        [Test]
        public void Can_deserialise_polymorphic_list_exact_with_no_side_effect_for_bad_type_position()
        {
            var dog =
                JsonSerializer.DeserializeFromString<Dog>(
                    @"{""Name"":""Fido"",""__type"":"""
                    + typeof(Dog).ToTypeString() + @"""}");

            Assert.That(dog.Name, Is.EqualTo(@"Fido"));

        }

        [Test]
        public void Should_not_instantiate_incorrect_type()
        {
            var json = @"{""__type"":"""
                + typeof(TestClass).ToTypeString() + @""", ""Name"":""Fido""}";

            var dog = JsonSerializer.DeserializeFromString<Dog>(json);

            Assert.IsFalse(TestClass.Called);
        }

        public class TestClass
        {
            public static bool Called { get; set; }

            public TestClass()
            {
                Called = true;
            }
        }

    }
}
