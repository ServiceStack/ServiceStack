using System;
using System.Runtime.Serialization;
using NUnit.Framework;

namespace ServiceStack.Text.Tests.JsonTests
{
    public class OnDeserializationErrorTests
    {
        [Test]
        public void Invokes_callback_on_protected_setter()
        {
            string json = @"{""idBadProt"":""value"", ""idGood"":""2"" }";

            AssertThatInvalidJsonInvokesExpectedCallback<TestDto>(json, "idBadProt", "value", typeof(int), "Input string was not in a correct format.");
        }

        [Test]
        public void Invokes_callback_on_incorrect_type()
        {
            string json = @"{""idBad"":""abc"", ""idGood"":""2"" }";
            AssertThatInvalidJsonInvokesExpectedCallback<TestDto>(json, "idBad", "abc", typeof(int), "Input string was not in a correct format.");
        }

        [Test]
        public void Invokes_callback_on_incorrect_type_with_data_set()
        {
            string json = @"{""idBad"":""abc"", ""idGood"":""2"" }";
            AssertThatInvalidJsonInvokesExpectedCallback<TestDto>(json, "idBad", "abc", typeof(int), "Input string was not in a correct format.");
        }

        [Test]
        public void Invokes_callback_on_value_out_of_range()
        {
            string json = @"{""idBad"":""4700000007"", ""idGood"":""2"" }";
            AssertThatInvalidJsonInvokesExpectedCallback<TestDto>(json, "idBad", "4700000007", typeof(int), "Value was either too large or too small for an Int32.");
        }

        [Test]
        public void Does_not_invoke_callback_on_valid_data()
        {
            JsConfig.Reset();
            JsConfig.OnDeserializationError = (o, s, s1, arg3, arg4) => Assert.Fail("For valida data this should not be invoked");

            var json = @"{""idBad"":""2"", ""idGood"":""2"" }";
            JsonSerializer.DeserializeFromString(json, typeof(TestDto));
        }

        [Test]
        public void TestReset()
        {
            JsConfig.Reset();
            Assert.IsNull(JsConfig.OnDeserializationError);
            JsConfig.OnDeserializationError = (o, s, s1, arg3, arg4) => { };
            Assert.IsNotNull(JsConfig.OnDeserializationError);
            JsConfig.Reset();
            Assert.IsNull(JsConfig.OnDeserializationError);
        }

        [Test]
        public void StrictMode_throws_Exception_on_array_with_missing_comma()
        {
            Env.StrictMode = true;
            string json = @"{""Values"": [ { ""Val"": ""a""} { ""Val"": ""b""}] }";
            
            Assert.Throws<SerializationException>(() => json.FromJson<TestDtoWithArray>());

            Env.StrictMode = false;
        }

        [Test]
        public void Does_not_invoke_callback_on_valid_data_with_array()
        {
            JsConfig.Reset();
            JsConfig.OnDeserializationError = (o, s, s1, arg3, arg4) => Assert.Fail("For valida data this should not be invoked");

            var json = @"{""Values"": [ { ""Val"": ""a""}, { ""Val"": ""b""}] }";
            JsonSerializer.DeserializeFromString(json, typeof(TestDtoWithArray));
        }

        [DataContract]
        class TestDtoWithArray
        {
            [DataMember]
            public TestChildDto[] Values { get; set; }
        }

        [DataContract]
        class TestChildDto
        {
            [DataMember]
            public string Val { get; set; }
        }

        [DataContract]
        class TestDto
        {
            [DataMember(Name = "idBadProt")]
            public int protId { get; protected set; }
            [DataMember(Name = "idGood")]
            public int IdGood { get; set; }
            [DataMember(Name = "idBad")]
            public int IdBad { get; set; }
        }

        private static void AssertThatInvalidJsonInvokesExpectedCallback<T>(string json, string expectedProperty, string expectedValue, Type expectedType, string expectedExceptionMessage)
        {
            string property = null, value = null;
            Type type = null;
            Exception ex = null;
            bool callbackInvoked = false;
            object deserialized = null;

            JsConfig.Reset();
            JsConfig.OnDeserializationError = (o, t, s, s1, arg4) =>
            {
                deserialized = o;
                type = t;
                property = s;
                value = s1;
                ex = arg4;
                callbackInvoked = true;
            };


            JsonSerializer.DeserializeFromString(json, typeof(T));

            Assert.IsTrue(callbackInvoked, "Callback should be invoked");
            Assert.AreEqual(expectedProperty, property);
            Assert.AreEqual(expectedValue, value);
            Assert.AreEqual(expectedType, type);
            if (expectedExceptionMessage != null)
            {
                Assert.AreEqual(expectedExceptionMessage, ex.Message);
            }
            Assert.IsNotNull(deserialized);
            Assert.IsInstanceOf<T>(deserialized);
        }
    }
}