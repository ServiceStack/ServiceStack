using System;
using System.Collections.Generic;
using NUnit.Framework;
using System.Diagnostics;

namespace ServiceStack.Text.Tests
{
    [TestFixture]
    public class SerializationDelegatePerformanceTests
        : TestBase
    {
        [OneTimeSetUp]
        public void SetUp()
        {
            AddSerializeHooksForType<PerformanceTestHookClass>();
        }

        [Test]
        public void TypeSerializer_Deserialize_Performance_WithoutHook()
        {
            var data = GenerateData<PerformanceTestClass>();

            var stringvalue = TypeSerializer.SerializeToString(data);

            Stopwatch watch = Stopwatch.StartNew();
            var deserializedData = TypeSerializer.DeserializeFromString<List<PerformanceTestClass>>(stringvalue);
            watch.Stop();

            Debug.WriteLine("Elapsed time: {0}ms", watch.ElapsedMilliseconds);

            // should be at least less than 200ms
            Assert.LessOrEqual(watch.ElapsedMilliseconds, 200);
        }

        [Test]
        public void TypeSerializer_Deserialize_Performance_WithHook()
        {
            var data = GenerateData<PerformanceTestHookClass>();

            var stringvalue = TypeSerializer.SerializeToString(data);

            Stopwatch watch = Stopwatch.StartNew();
            var deserializedData = TypeSerializer.DeserializeFromString<List<PerformanceTestHookClass>>(stringvalue);
            watch.Stop();

            Debug.WriteLine("Elapsed time: {0}ms", watch.ElapsedMilliseconds);

            // should be at least less than 600ms
            Assert.LessOrEqual(watch.ElapsedMilliseconds, 600);
        }

        [Test]
        public void TypeSerializer_Serialize_Performance_WithoutHook()
        {
            var data = GenerateData<PerformanceTestClass>();

            Stopwatch watch = Stopwatch.StartNew();
            var stringvalue = TypeSerializer.SerializeToString(data);
            watch.Stop();

            Debug.WriteLine("Elapsed time: {0}ms", watch.ElapsedMilliseconds);

            // should be at least less than 100ms
            Assert.LessOrEqual(watch.ElapsedMilliseconds, 100);
        }

        [Test]
        public void TypeSerializer_Serialize_Performance_WithHook()
        {
            var data = GenerateData<PerformanceTestHookClass>();

            Stopwatch watch = Stopwatch.StartNew();
            var stringvalue = TypeSerializer.SerializeToString(data);
            watch.Stop();

            Debug.WriteLine("Elapsed time: {0}ms", watch.ElapsedMilliseconds);

            // should be at least less than 100ms
            Assert.LessOrEqual(watch.ElapsedMilliseconds, 100);
        }

        private List<T> GenerateData<T>() where T : PerformanceTestClass, new()
        {
            List<T> result = new List<T>();

            for (int i = 0; i < 5000; i++)
            {
                T user = new T();
                user.FirstName = "Performance" + i;
                user.LastName = "Test";
                user.ID = i;
                user.Email = String.Format("mail{0}@test.com", i);
                user.UserName = "Test" + i;
                user.AddressID = i * 32;

                result.Add(user);
            }

            return result;
        }

        public static void AddSerializeHooksForType<T>()
        {

            JsConfig<T>.OnSerializingFn = s =>
            {
                return s;
            };

            JsConfig<T>.OnSerializedFn = s =>
            {

            };

            JsConfig<T>.OnDeserializedFn = s =>
            {
                return s;
            };

        }

        class PerformanceTestClass : SerializationHookTests.HookTestSubClass
        {
            public string FirstName { get; set; }
            public string LastName { get; set; }
            public int ID { get; set; }
            public string Email { get; set; }
            public string UserName { get; set; }
            public int AddressID { get; set; }
        }

        class PerformanceTestHookClass : PerformanceTestClass { }
    }
}
