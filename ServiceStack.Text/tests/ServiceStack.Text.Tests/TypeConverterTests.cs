using System;
using System.ComponentModel;
using NUnit.Framework;
#if !NETCORE
using System.Security.Policy;
#endif

namespace ServiceStack.Text.Tests
{
    [TestFixture]
    public class TypeConverterTests
    {
        public class CustomException
            : Exception
        {
            public CustomException(string message) : base(message)
            {
                this.CustomMessage = "Custom" + message;
            }

            public string CustomMessage { get; set; }
        }

        [Test]
        public void View_TypeConverter_outputs()
        {
#if !NETCORE
            var converter1 = TypeDescriptor.GetConverter(typeof(Url));
            Console.WriteLine(converter1.ConvertToString(new Url("http://io/")));
#endif

            var converter2 = TypeDescriptor.GetConverter(typeof(Type));
            Console.WriteLine(converter2.ConvertToString(typeof(TypeConverterTests)));

            var converter3 = TypeDescriptor.GetConverter(typeof(CustomException));
            var string3 = converter3.ConvertToString(new Exception("Test 123"));
            Console.WriteLine(string3);
            //var value3 = converter3.ConvertFromString(string3);
            //Console.WriteLine(value3.Dump());
        }
    }
}