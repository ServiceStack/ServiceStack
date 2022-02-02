using System;
using System.Globalization;
using NUnit.Framework;
using ServiceStack;
using ServiceStack.Text;

namespace ServiceStack.Text.Tests.Issues
{
    [TestFixture]
    public class JsConfigIssues
    {
        struct CustomFormatType
        {
            private int _value;

            public int Value
            {
                get { return _value; }
            }

            public CustomFormatType(int value)
            {
                _value = value;
            }

            public override string ToString()
            {
                return _value.ToString();
            }
        }

        class Dto
        {
            public CustomFormatType CustomFormatTypeProperty { get; set; }
        }

        [Test]
        public void CallReset_AfterSerializingOnce_WithCustomSerializationForProperty_DoesNotPickUpFurtherConfigChangesForPropertyType()
        {
            var dto = new Dto { CustomFormatTypeProperty = new CustomFormatType(12345) };

            ConfigureCustomFormatType();
            TestRoundTripValue(dto);

            JsConfig.Reset();
            
            ConfigureCustomFormatType();
            JsConfig<Dto>.RefreshRead();

            TestRoundTripValue(dto);
        }
        
        [Test]
        public void CallReset_AfterSerializingOnce_WithCustomSerializationForProperty_MustClearCustomSerialization()
        {
            var dto = new Dto { CustomFormatTypeProperty = new CustomFormatType(12345) };
            JsConfig<CustomFormatType>.DeSerializeFn = str => 
                new CustomFormatType(int.Parse(str));
            var json = dto.ToJson();
            JsConfig.Reset();
            var fromJson = json.FromJson<Dto>();
            Assert.That(fromJson.CustomFormatTypeProperty.Value, Is.EqualTo(0));
        }

        private static void ConfigureCustomFormatType()
        {
            JsConfig<CustomFormatType>.RawSerializeFn = value => 
                value.Value.ToString("x");

            JsConfig<CustomFormatType>.DeSerializeFn = str => 
                new CustomFormatType(int.Parse(str, NumberStyles.HexNumber));
        }

        private static void TestRoundTripValue(Dto dto)
        {
            var json = dto.ToJson();
            var fromJson = json.FromJson<Dto>();
            Assert.That(fromJson.CustomFormatTypeProperty.Value, Is.EqualTo(dto.CustomFormatTypeProperty.Value));
        }
    }
}