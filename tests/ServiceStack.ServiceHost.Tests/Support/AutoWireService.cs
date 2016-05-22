using System.Runtime.Serialization;

namespace ServiceStack.ServiceHost.Tests.Support
{
    [DataContract]
    public class AutoWire { }

    [DataContract]
    public class AutoWireResponse
    {
        public IFoo Foo { get; set; }
        public IBar Bar { get; set; }
    }

    public class AutoWireService : IService
    {
        private readonly IFoo foo;

        public IFoo Foo
        {
            get { return foo; }
        }

        public IBar Bar { get; set; }

        public int Count { get; set; }

        public AutoWireService(IFoo foo)
        {
            this.foo = foo;
        }

        public object Any(AutoWire request)
        {
            return new AutoWireResponse { Foo = foo, Bar = Bar };
        }
    }

    public class Foo : IFoo
    {
    }

    public class Foo2 : IFoo
    {
    }

    public interface IFoo
    {
    }

    public class Bar : IBar
    {
    }

    public class Bar2 : IBar
    {
    }

    public interface IBar
    {
    }
}