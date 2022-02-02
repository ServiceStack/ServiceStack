using System;
using System.Linq;
using System.Runtime.Serialization;
using NUnit.Framework;

namespace ServiceStack.Text.Tests
{
    [TestFixture]
    public class SerializationHookTests
        : TestBase
    {
        [OneTimeSetUp]
        public void SetUp()
        {
            AddSerializeHooksForType<HookTestClass>();
            AddSerializeHooksForType<HookTestSubClass>();
        }

        [Test]
        public void TypeSerializer_Serialize_hooks_on_base_class()
        {
            var original = new HookTestClass();
            var json = TypeSerializer.SerializeToString<HookTestClass>(original);

            Assert.That(original.OnSerializedTouched, Is.True);
            Assert.That(original.OnSerializingTouched, Is.True);
        }

        [Test]
        public void TypeSerializer_Serialize_hooks_on_sub_class()
        {
            var original = new HookTestSubClass();
            var json = TypeSerializer.SerializeToString<HookTestSubClass>(original);

            Assert.That(original.OnSerializedTouched, Is.True);
            Assert.That(original.OnSerializingTouched, Is.True);
        }

        [Test]
        public void TypeSerializer_Deserialize_hooks_on_base_class()
        {
            var original = new HookTestClass();

            var json = TypeSerializer.SerializeToString<HookTestClass>(original);
            var deserialized = TypeSerializer.DeserializeFromString<HookTestClass>(json);

            Assert.That(deserialized.OnDeserializedTouched, Is.True);
        }

        [Test]
        public void TypeSerializer_Deserialize_hooks_on_sub_class()
        {
            var original = new HookTestSubClass();

            var json = TypeSerializer.SerializeToString<HookTestSubClass>(original);
            var deserialized = TypeSerializer.DeserializeFromString<HookTestSubClass>(json);

            Assert.That(deserialized.OnDeserializedTouched, Is.True);
        }

        [Test]
        public void JsonSerializer_Serialize_hooks_on_base_class()
        {
            var original = new HookTestClass();
            var json = JsonSerializer.SerializeToString<HookTestClass>(original);

            Assert.That(original.OnSerializedTouched, Is.True);
            Assert.That(original.OnSerializingTouched, Is.True);
        }

        [Test]
        public void JsonSerializer_Serialize_hooks_on_sub_class()
        {
            var original = new HookTestSubClass();
            var json = JsonSerializer.SerializeToString<HookTestSubClass>(original);

            Assert.That(original.OnSerializedTouched, Is.True);
            Assert.That(original.OnSerializingTouched, Is.True);
        }

        [Test]
        public void JsonSerializer_Deserialize_hooks_on_base_class()
        {
            var original = new HookTestClass();

            var json = JsonSerializer.SerializeToString<HookTestClass>(original);
            var deserialized = JsonSerializer.DeserializeFromString<HookTestClass>(json);

            Assert.That(deserialized.OnDeserializedTouched, Is.True);
        }

        [Test]
        public void JsonSerializer_Deserialize_hooks_on_sub_class()
        {
            var original = new HookTestSubClass();

            var json = JsonSerializer.SerializeToString<HookTestSubClass>(original);
            var deserialized = JsonSerializer.DeserializeFromString<HookTestSubClass>(json);

            Assert.That(deserialized.OnDeserializedTouched, Is.True);
        }

        [Test]
        public void JsonSerializer_Deserialize_hooks_set_in_cctor()
        {
            // Deserialize without serializing first, so we don't call the static constructor of HookTestCctor, which sets its own OnDeserialized callback.

            //var original = new HookTestContainer { Child = new HookTestCctor() };
            //var json = JsonSerializer.SerializeToString(original);
            const string json = "{\"Child\":{\"OnDeserializingTouched\":false,\"OnDeserializedTouched\":false,\"OnSerializingTouched\":true,\"OnSerializedTouched\":false}}";

            var deserialized = JsonSerializer.DeserializeFromString<HookTestContainer>(json);
            Assert.That(deserialized.Child.OnDeserializedTouched, Is.True);
        }

        internal static void AddSerializeHooksForType<T>()
        {
            Type type = typeof(T);
            System.Reflection.MethodInfo[] typeMethods = type.GetMethods(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var onSerializingMethods = typeMethods.Where(m => m.GetCustomAttributes(typeof(OnSerializingAttribute), true).Length > 0);
            var OnDeserializedMethods = typeMethods.Where(m => m.GetCustomAttributes(typeof(OnDeserializedAttribute), true).Length > 0);
            var OnSerializedMethods = typeMethods.Where(m => m.GetCustomAttributes(typeof(OnSerializedAttribute), true).Length > 0);
            Object[] Parameters = { null };

            if (onSerializingMethods.Any())
            {
                ServiceStack.Text.JsConfig<T>.OnSerializingFn = s =>
                {
                    foreach (var method in onSerializingMethods)
                        method.Invoke(s, Parameters);

                    return s;
                };
            }

            if (OnSerializedMethods.Any())
            {
                ServiceStack.Text.JsConfig<T>.OnSerializedFn = s =>
                {
                    foreach (var method in OnSerializedMethods)
                        method.Invoke(s, Parameters);
                };
            }

            if (OnDeserializedMethods.Any())
            {
                ServiceStack.Text.JsConfig<T>.OnDeserializedFn = s =>
                {
                    foreach (var method in OnDeserializedMethods)
                        method.Invoke(s, Parameters);

                    return s;
                };
            }
        }

        public class HookTestClass
        {
            public bool OnDeserializingTouched { get; set; }
            public bool OnDeserializedTouched { get; set; }
            public bool OnSerializingTouched { get; set; }
            public bool OnSerializedTouched { get; set; }

            /// <summary>
            /// Will be executed when deserializing starts
            /// </summary>
            /// <param name="ctx"></param>
            [OnDeserializing]
            protected void OnDeserializing(StreamingContext ctx)
            {
                OnDeserializingTouched = true;
            }

            /// <summary>
            /// Will be executed when deserializing finished
            /// </summary>
            /// <param name="ctx"></param>
            [OnDeserialized]
            protected void OnDeserialized(StreamingContext ctx)
            {
                OnDeserializedTouched = true;
            }

            /// <summary>
            /// Will be executed when serializing starts
            /// </summary>
            /// <param name="ctx"></param>
            [OnSerializing]
            protected void OnSerializing(StreamingContext ctx)
            {
                OnSerializingTouched = true;
            }

            /// <summary>
            /// Will be executed when serializing finished
            /// </summary>
            /// <param name="ctx"></param>
            [OnSerialized]
            protected void OnSerialized(StreamingContext ctx)
            {
                OnSerializedTouched = true;
            }
        }

        public class HookTestSubClass : HookTestClass
        {
        }

        public class HookTestCctor : HookTestClass
        {
            static HookTestCctor()
            {
                AddSerializeHooksForType<HookTestCctor>();
            }
        }

        public class HookTestContainer
        {
            public HookTestCctor Child { get; set; }
        }
    }
}
