using System;
using System.Linq;
using NUnit.Framework;
using ServiceStack;
using System.Runtime.Serialization;
using System.Reflection;

namespace ServiceStack.Text.Tests
{
    using System.Collections.Generic;

    [TestFixture]
    public class AttributeTests
    {
        [Test]
        public void Does_get_Single_Default_Attribute()
        {
            var attrs = typeof(DefaultWithSingleAttribute).AllAttributes<RouteDefaultAttribute>();
            Assert.That(attrs[0].ToString(), Is.EqualTo("/path:"));

            var attr = typeof(DefaultWithSingleAttribute).FirstAttribute<RouteDefaultAttribute>();
            Assert.That(attr.ToString(), Is.EqualTo("/path:"));
        }

        [Test]
        public void Does_get_Single_TypeId_Attribute()
        {
            var attrs = typeof(TypeIdWithSingleAttribute).AllAttributes<RouteTypeIdAttribute>();
            Assert.That(attrs[0].ToString(), Is.EqualTo("/path:"));

            var attr = typeof(TypeIdWithSingleAttribute).FirstAttribute<RouteTypeIdAttribute>();
            Assert.That(attr.ToString(), Is.EqualTo("/path:"));
        }

        [Test]
        public void Does_get_Multiple_RouteDefault_Attributes()
        {
            // AllAttributes<T>() makes this call to get attrs
#if !NETFRAMEWORK
            var referenceGeneric =
                typeof(DefaultWithMultipleAttributes).GetTypeInfo().GetCustomAttributes(typeof(RouteDefaultAttribute), true)
                    .OfType<RouteDefaultAttribute>();
#else
            var referenceGeneric =
                typeof(DefaultWithMultipleAttributes).GetCustomAttributes(typeof(RouteDefaultAttribute), true)
                    .OfType<RouteDefaultAttribute>();
#endif
            // Attribute inheritance hierarchies (InheritedRouteAttribute) are returned in results
            Assert.That(referenceGeneric.Count(), Is.EqualTo(4));

            // AllAttributes() makes this call to get attrs
#if !NETFRAMEWORK
            var reference =
                typeof(DefaultWithMultipleAttributes).GetTypeInfo().GetCustomAttributes(typeof(RouteDefaultAttribute), true);
#else
            var reference =
                typeof(DefaultWithMultipleAttributes).GetCustomAttributes(typeof(RouteDefaultAttribute), true);
#endif
            // Attribute inheritance hierarchies (InheritedRouteAttribute) are returned in results
            Assert.That(reference.Count(), Is.EqualTo(4));

            // Loses one of the attrs with inheritence when union
            var referenceUnion = referenceGeneric.Union(new List<RouteDefaultAttribute>());
            Assert.That(referenceUnion.Count(), Is.EqualTo(4));

            // Keeps all items when concat
            var referenceConcat = referenceGeneric.Concat(new List<RouteDefaultAttribute>());
            Assert.That(referenceConcat.Count(), Is.EqualTo(4));

            var attrsGeneric = typeof(DefaultWithMultipleAttributes).AllAttributes<RouteDefaultAttribute>();

            // Attribute inheritance hierarchies (InheritedRouteAttribute) are NOT ALL returned in results
            Assert.That(attrsGeneric.Length, Is.EqualTo(4)); // union loses one

            var attrs = typeof(DefaultWithMultipleAttributes).AllAttributes(typeof(RouteDefaultAttribute));

            // Attribute inheritance hierarchies (InheritedRouteAttribute) are NOT ALL returned in results
            Assert.That(attrs.Length, Is.EqualTo(4)); // union loses one

            var values = attrs.ToList().ConvertAll(x => x.ToString());

            Assert.That(values, Is.EquivalentTo(new[] {
                "/path:", "/path/2:", "/path:GET", "/path:POST", 
            }));

            var objAttrs = typeof(DefaultWithMultipleAttributes).AllAttributes();
            values = objAttrs.ToList().ConvertAll(x => x.ToString());

            Assert.That(values, Is.EquivalentTo(new[] {
                "/path:", "/path/2:", "/path:GET", "/path:POST", 
            }));

            objAttrs = typeof(DefaultWithMultipleAttributes).AllAttributes(typeof(RouteDefaultAttribute));
            values = objAttrs.ToList().ConvertAll(x => x.ToString());

            Assert.That(values, Is.EquivalentTo(new[] {
                "/path:", "/path/2:", "/path:GET", "/path:POST", 
            }));
        }

        [Test]
        public void Does_get_Multiple_Route_Attributes()
        {
            var routeAttrs = typeof(DefaultWithMultipleRouteAttributes)
                .AllAttributes<RouteAttribute>();

            Assert.That(routeAttrs.Length, Is.EqualTo(4));

            var values = routeAttrs.ToList().ConvertAll(x => "{0}:{1}".Fmt(x.Path, x.Verbs));

            Assert.That(values, Is.EquivalentTo(new[] {
                "/path:", "/path/2:", "/path:GET", "/path:POST", 
            }));

            var inheritedRouteAttrs = typeof(DefaultWithMultipleRouteAttributes)
                .AllAttributes<InheritedRouteAttribute>();

            Assert.That(inheritedRouteAttrs.Length, Is.EqualTo(2));
        }

        [Test]
        public void Does_get_Multiple_TypeId_Attributes()
        {
            var attrs = typeof(TypeIdWithMultipleAttributes).AllAttributes<RouteTypeIdAttribute>();
            Assert.That(attrs.Length, Is.EqualTo(4));

            var values = attrs.ToList().ConvertAll(x => x.ToString());

            Assert.That(values, Is.EquivalentTo(new[] {
                "/path:", "/path/2:", "/path:GET", "/path:POST", 
            }));

            var objAttrs = typeof(TypeIdWithMultipleAttributes).AllAttributes();
            values = objAttrs.ToList().ConvertAll(x => x.ToString());

            Assert.That(values, Is.EquivalentTo(new[] {
                "/path:", "/path/2:", "/path:GET", "/path:POST", 
            }));

            objAttrs = typeof(TypeIdWithMultipleAttributes).AllAttributes(typeof(RouteTypeIdAttribute));
            values = objAttrs.ToList().ConvertAll(x => x.ToString());

            Assert.That(values, Is.EquivalentTo(new[] {
                "/path:", "/path/2:", "/path:GET", "/path:POST", 
            }));
        }
    }

    [TestFixture]
    public class RuntimeAttributesTests
    {
        [Test]
        public void Can_add_to_Multiple_Default_Attributes()
        {
            typeof (DefaultWithMultipleAttributes).AddAttributes(
                new RouteDefaultAttribute("/path-add"),
                new RouteDefaultAttribute("/path-add", "GET"));

            var attrs = typeof(DefaultWithMultipleAttributes).AllAttributes<RouteDefaultAttribute>();
            Assert.That(attrs.Length, Is.EqualTo(6));

            var values = attrs.ToList().ConvertAll(x => x.ToString());

            Assert.That(values, Is.EquivalentTo(new[] {
                "/path:", "/path/2:", "/path:GET", "/path:POST", 
                "/path-add:", "/path-add:GET",
            }));

            var objAttrs = typeof(DefaultWithMultipleAttributes).AllAttributes();
            values = objAttrs.ToList().ConvertAll(x => x.ToString());

            Assert.That(values, Is.EquivalentTo(new[] {
                "/path:", "/path/2:", "/path:GET", "/path:POST", 
                "/path-add:", "/path-add:GET",
            }));

            objAttrs = typeof(DefaultWithMultipleAttributes).AllAttributes(typeof(RouteDefaultAttribute));
            values = objAttrs.ToList().ConvertAll(x => x.ToString());

            Assert.That(values, Is.EquivalentTo(new[] {
                "/path:", "/path/2:", "/path:GET", "/path:POST", 
                "/path-add:", "/path-add:GET",
            }));
        }

        [Test]
        public void Does_get_Multiple_TypeId_Attributes()
        {
            typeof(TypeIdWithMultipleAttributes).AddAttributes(
                new RouteTypeIdAttribute("/path-add"),
                new RouteTypeIdAttribute("/path-add", "GET"));

            var attrs = typeof(TypeIdWithMultipleAttributes).AllAttributes<RouteTypeIdAttribute>();
            Assert.That(attrs.Length, Is.EqualTo(6));

            var values = attrs.ToList().ConvertAll(x => x.ToString());

            Assert.That(values, Is.EquivalentTo(new[] {
                "/path:", "/path/2:", "/path:GET", "/path:POST", 
                "/path-add:", "/path-add:GET",
            }));

            var objAttrs = typeof(TypeIdWithMultipleAttributes).AllAttributes();
            values = objAttrs.ToList().ConvertAll(x => x.ToString());

            Assert.That(values, Is.EquivalentTo(new[] {
                "/path:", "/path/2:", "/path:GET", "/path:POST", 
                "/path-add:", "/path-add:GET",
            }));

            objAttrs = typeof(TypeIdWithMultipleAttributes).AllAttributes(typeof(RouteTypeIdAttribute));
            values = objAttrs.ToList().ConvertAll(x => x.ToString());

            Assert.That(values, Is.EquivalentTo(new[] {
                "/path:", "/path/2:", "/path:GET", "/path:POST", 
                "/path-add:", "/path-add:GET",
            }));
        }
    }

    [RouteTypeId("/path")]
    public class TypeIdWithSingleAttribute { }

    [RouteTypeId("/path")]
    [RouteTypeId("/path/2")]
    [RouteTypeId("/path", "GET")]
    [RouteTypeId("/path", "POST")]
    public class TypeIdWithMultipleAttributes { }

    [RouteDefault("/path")]
    public class DefaultWithSingleAttribute { }

    [RouteDefault("/path")]
    [RouteDefault("/path/2")]
    [InheritedRouteDefault("/path", "GET")]
    [InheritedRouteDefault("/path", "POST")]
    public class DefaultWithMultipleAttributes { }

    [Route("/path")]
    [Route("/path/2")]
    [InheritedRoute("/path", "GET")]
    [InheritedRoute("/path", "POST")]
    public class DefaultWithMultipleRouteAttributes { }

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
    public class RouteTypeIdAttribute : Attribute
    {
        public RouteTypeIdAttribute(string path) : this(path, null) {}
        public RouteTypeIdAttribute(string path, string verbs)
        {
            Path = path;
            Verbs = verbs;
        }

        public string Path { get; set; }
        public string Verbs { get; set; }

        public override object TypeId
        {
            get
            {
                return (Path ?? "")
                    + (Verbs ?? "");
            }
        }

        public override string ToString()
        {
            return "{0}:{1}".Fmt(Path, Verbs);
        }
    }

    public class InheritedRouteDefaultAttribute : RouteDefaultAttribute {
        public InheritedRouteDefaultAttribute(string path)
            : base(path)
        {
        }

        public InheritedRouteDefaultAttribute(string path, string verbs)
            : base(path, verbs)
        {
        }
    }

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
    public class RouteDefaultAttribute : Attribute
    {
        public RouteDefaultAttribute(string path) : this(path, null) {}
        public RouteDefaultAttribute(string path, string verbs)
        {
            Path = path;
            Verbs = verbs;
        }

        public string Path { get; set; }
        public string Verbs { get; set; }

        public override string ToString()
        {
            return "{0}:{1}".Fmt(Path, Verbs);
        }

        protected bool Equals(RouteDefaultAttribute other)
        {
            return base.Equals(other) && string.Equals(Path, other.Path) && string.Equals(Verbs, other.Verbs);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((RouteDefaultAttribute) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = base.GetHashCode();
                hashCode = (hashCode*397) ^ (Path != null ? Path.GetHashCode() : 0);
                hashCode = (hashCode*397) ^ (Verbs != null ? Verbs.GetHashCode() : 0);
                return hashCode;
            }
        }
    }

    public class InheritedRouteAttribute : RouteAttribute
    {
        public InheritedRouteAttribute(string path)
            : base(path)
        {
        }

        public InheritedRouteAttribute(string path, string verbs)
            : base(path, verbs)
        {
        }

        public string Custom { get; set; }
    }

}