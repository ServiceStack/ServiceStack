// Copyright (c) Service Stack LLC. All Rights Reserved.
// License: https://raw.github.com/ServiceStack/ServiceStack/master/license.txt

#if !NETCORE
using System;
using System.ComponentModel;
using NUnit.Framework;
using ServiceStack.Text;

namespace ServiceStack.Common.Tests
{
    public interface INormalAttribute
    {
        string Name { get; set; }
    }

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
    public class NormalAttribute : Attribute, INormalAttribute
    {
        public string Name { get; set; }

        public NormalAttribute(string name)
        {
            Name = name;
        }
    }

    public interface IBaseAttribute
    {
        string Name { get; set; }
    }

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
    public class BaseAttribute : AttributeBase, IBaseAttribute
    {
        public string Name { get; set; }

        public BaseAttribute(string name)
        {
            Name = name;
        }
    }

    [Normal("a")]
    [Base("b")]
    public class SingleAttr {}

    [Normal("a1")]
    [Normal("a2")]
    [Base("b1")]
    [Base("b2")]
    public class ManyAttr { }

    [Normal("a1")]
    [Normal("a2")]
    public class RuntimeManyNormalAttr { }

    [Base("b1")]
    [Base("b2")]
    public class RuntimeManyBaseAttr { }
    

    [TestFixture]
    public class AttributeTests
    {
        [Test]
        public void Does_return_on_FirstAttribute()
        {
            var o = new SingleAttr();

            Assert.That(o.GetType().FirstAttribute<NormalAttribute>().Name, Is.EqualTo("a"));
            Assert.That(o.GetType().FirstAttribute<BaseAttribute>().Name, Is.EqualTo("b"));
            
            Assert.That(o.GetType().FirstAttribute<INormalAttribute>().Name, Is.EqualTo("a"));
            Assert.That(o.GetType().FirstAttribute<IBaseAttribute>().Name, Is.EqualTo("b"));
        }

        [Test]
        public void Normal_attribute_returns_all_in_AllAttributes()
        {
            var o = new ManyAttr();

            Assert.That(o.GetType().AllAttributes<NormalAttribute>().Length, Is.EqualTo(2));
            Assert.That(o.GetType().AllAttributes<INormalAttribute>().Length, Is.EqualTo(2));
        }

        [Test]
        public void AttributeBase_attribute_returns_all_in_AllAttributes()
        {
            var o = new ManyAttr();

            Assert.That(o.GetType().AllAttributes<BaseAttribute>().Length, Is.EqualTo(2));
            Assert.That(o.GetType().AllAttributes<IBaseAttribute>().Length, Is.EqualTo(2));
        }

        [Test]
        public void Can_add_attributes_at_runtime_to_BaseAttribute()
        {
            typeof(RuntimeManyBaseAttr).AddAttributes(new BaseAttribute("b3"));

            var o = new RuntimeManyBaseAttr();

            Assert.That(o.GetType().AllAttributes<BaseAttribute>().Length, Is.EqualTo(3));
            Assert.That(o.GetType().AllAttributes<IBaseAttribute>().Length, Is.EqualTo(3));
        }
    }
}
#endif