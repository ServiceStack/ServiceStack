using System;
using System.Collections.Generic;
using NUnit.Framework;
using NUnit.Framework.SyntaxHelpers;
using ServiceStack.LogicFacade;

namespace ServiceStack.ServiceInterface.Tests
{
	[TestFixture]
	public class CreateFromLargestConstructorTypeFactoryTests
	{
		public class ClassWithNoContructors
		{
			public int IntValue { get; set; }
		}

		private static CreateFromLargestConstructorTypeFactory CreateWithNoFactory()
		{
			return new CreateFromLargestConstructorTypeFactory(new FactoryProvider());
		}

		private static CreateFromLargestConstructorTypeFactory CreateWithFactory(FactoryProvider factoryProvider)
		{
			return new CreateFromLargestConstructorTypeFactory(factoryProvider);
		}

		[Test]
		public void Can_create_ClassWithNoContructors()
		{
			var typeFactory = CreateWithNoFactory();

			var result = typeFactory.Create(typeof(ClassWithNoContructors));

			var newInstance = result as ClassWithNoContructors;
			Assert.That(newInstance, Is.Not.Null);
			Assert.That(newInstance.IntValue, Is.EqualTo(default(int)));
		}

		[Test]
		public void Can_create_ClassWithNoContructors_multipleTimes()
		{
			var typeFactory = CreateWithNoFactory();

			var results = new List<ClassWithNoContructors>
          	{
          		(ClassWithNoContructors) typeFactory.Create(typeof (ClassWithNoContructors)),
          		(ClassWithNoContructors) typeFactory.Create(typeof (ClassWithNoContructors)),
          		(ClassWithNoContructors) typeFactory.Create(typeof (ClassWithNoContructors))
          	};

			foreach (var newInstance in results)
			{
				Assert.That(newInstance.IntValue, Is.EqualTo(default(int)));
			}
		}

		private class ClassWithIntConstructor
		{
			public int IntValue { get; set; }

			public ClassWithIntConstructor(int intValue)
			{
				IntValue = intValue;
			}
		}


		[Test]
		public void Can_create_ClassWithIntConstructor()
		{
			var typeFactory = CreateWithNoFactory();

			var result = typeFactory.Create(typeof(ClassWithIntConstructor));

			var newInstance = result as ClassWithIntConstructor;
			Assert.That(newInstance, Is.Not.Null);
			Assert.That(newInstance.IntValue, Is.EqualTo(default(int)));
		}

		private class ClassWithIntAndStringConstructor
		{
			public int IntValue { get; set; }

			public string StringValue { get; set; }

			public ClassWithIntAndStringConstructor(int intValue, string stringValue)
			{
				IntValue = intValue;
				StringValue = stringValue;
			}
		}

		[Test]
		public void Can_create_ClassWithIntAndStringConstructor()
		{
			var typeFactory = CreateWithNoFactory();

			var result = typeFactory.Create(typeof(ClassWithIntAndStringConstructor));

			var newInstance = result as ClassWithIntAndStringConstructor;
			Assert.That(newInstance, Is.Not.Null);
			Assert.That(newInstance.IntValue, Is.EqualTo(default(int)));
			Assert.That(newInstance.StringValue, Is.Null);
		}


		private class ClassWithAResolvableContructors
		{
			public IRequestContext RequestContext { get; set; }
			public IApplicationContext ApplicationContext { get; set; }

			public ClassWithAResolvableContructors(IRequestContext requestContext, IApplicationContext applicationContext)
			{
				RequestContext = requestContext;
				ApplicationContext = applicationContext;
			}
		}

		[Test]
		public void Can_create_ClassWithAResolvableContructors()
		{
			var request = new ClassWithIntConstructor(1);
			
			var factory = new FactoryProvider();
			factory.Register(new RequestContext(request, null));
			factory.Register(new BasicApplicationContext(null, null, null));

			var typeFactory = CreateWithFactory(factory);

			var result = typeFactory.Create(typeof(ClassWithAResolvableContructors));

			var newInstance = result as ClassWithAResolvableContructors;
			Assert.That(newInstance, Is.Not.Null);

			Assert.That(newInstance.RequestContext, Is.Not.Null);
			Assert.That(newInstance.ApplicationContext, Is.Not.Null);

			var resolvedRequest = newInstance.RequestContext.Dto as ClassWithIntConstructor;
			Assert.That(resolvedRequest, Is.Not.Null);
			Assert.That(resolvedRequest.IntValue, Is.EqualTo(1));
		}


		private class ClassWithResolvableAndMultipleContructors : IService
		{
			public int IntValue { get; set; }
			public IRequestContext RequestContext { get; set; }
			public IApplicationContext ApplicationContext { get; set; }

			public ClassWithResolvableAndMultipleContructors(int intValue)
			{
				IntValue = 2;
			}

			public ClassWithResolvableAndMultipleContructors(IRequestContext requestContext, IApplicationContext applicationContext)
			{
				RequestContext = requestContext;
				ApplicationContext = applicationContext;
			}

			public object Execute(IOperationContext context)
			{
				throw new NotImplementedException();
			}
		}

		[Test]
		public void ClassWithResolvableAndMultipleContructors_should_pick_contructor_with_most_params()
		{
			var request = new ClassWithIntConstructor(1);

			var factory = new FactoryProvider();
			factory.Register(new RequestContext(request, null));
			factory.Register(new BasicApplicationContext(null, null, null));

			var typeFactory = CreateWithFactory(factory);

			var result = typeFactory.Create(typeof(ClassWithResolvableAndMultipleContructors));

			var newInstance = result as ClassWithResolvableAndMultipleContructors;
			Assert.That(newInstance, Is.Not.Null);

			Assert.That(newInstance.RequestContext, Is.Not.Null);
			Assert.That(newInstance.ApplicationContext, Is.Not.Null);
			Assert.That(newInstance.IntValue, Is.EqualTo(default(int)));

			var resolvedRequest = newInstance.RequestContext.Dto as ClassWithIntConstructor;
			Assert.That(resolvedRequest, Is.Not.Null);
			Assert.That(resolvedRequest.IntValue, Is.EqualTo(1));
		}

	}
}