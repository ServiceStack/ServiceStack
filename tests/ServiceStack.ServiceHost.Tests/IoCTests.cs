﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using ServiceStack.ServiceHost.Tests.Support;
using Funq;

namespace ServiceStack.ServiceHost.Tests
{
	[TestFixture]
	public class IoCTests
	{
		[Test]
		public void Can_AutoWire_types_dynamically_with_expressions()
		{
			var serviceType = typeof(AutoWireService);

			var container = new Container();
			container.Register<IFoo>(c => new Foo());
			container.Register<IBar>(c => new Bar());
			container.Register<int>(c => 100);

			var typeContainer = new AutoWireContainer(container);
			typeContainer.RegisterTypes(serviceType);

			var service = container.Resolve<AutoWireService>();

			Assert.That(service.Foo, Is.Not.Null);
			Assert.That(service.Bar, Is.Not.Null);
			Assert.That(service.Count, Is.EqualTo(0));
		}

		public class Test
		{
			public IFoo Foo { get; set; }
			public IBar Bar { get; set; }
			public IFoo Foo2 { get; set; }
			public IEnumerable<string> Names { get; set; }
			public int Age { get; set; }

			public Test()
			{
				this.Foo2 = new Foo2();
				this.Names = new List<string>() { "Steffen", "Demis" };
			}
		}

		[Test]
		public void Can_AutoWire_Existing_Instance()
		{
			var test = new Test();

			var container = new Container();
			container.Register<IFoo>(c => new Foo());
			container.Register<IBar>(c => new Bar());
			container.Register<int>(c => 10);

			container.AutoWire(test);

			Assert.That(test.Foo, Is.Not.Null);
			Assert.That(test.Bar, Is.Not.Null);
			Assert.That(test.Foo2 as Foo, Is.Null);
			Assert.That(test.Names, Is.Not.Null);
			Assert.That(test.Age, Is.EqualTo(0));
		}
	}
}
