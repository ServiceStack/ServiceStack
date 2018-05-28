using NUnit.Framework;

namespace ServiceStack.Common.Tests.Reflection
{
    class TransformDouble
    {
        public double Target { get; }
        public TransformDouble(double target) => Target = target;

        public double Add(double value) => Target + value;
    }
    
    public class InvokerMethodTests
    {
        [Test]
        public void Can_use_MethodInvoker_to_call_Add_with_runtime_type()
        {
            var method = typeof(TransformDouble).GetMethod("Add");
            var invoker = method.GetInvoker();

            var instance = new TransformDouble(1.0);
            Assert.That(invoker(instance, 2.0), Is.EqualTo(3.0));
            
            Assert.That(invoker(instance, 2), Is.EqualTo(3.0));
            Assert.That(invoker(instance, "2"), Is.EqualTo(3.0));
        }

        [Test]
        public void Can_use_ObjectActivator_to_call_Add_with_runtime_type()
        {
            var ctor = typeof(TransformDouble).GetConstructors()[0];
            var activator = ctor.GetActivator();

            Assert.That(((TransformDouble)activator(1.0)).Target, Is.EqualTo(1.0));

            Assert.That(((TransformDouble)activator(1)).Target, Is.EqualTo(1.0));
            Assert.That(((TransformDouble)activator("1")).Target, Is.EqualTo(1.0));
        }

    }
}