using System;
using System.Linq.Expressions;
using NUnit.Framework;
using NUnit.Framework.SyntaxHelpers;
using ServiceStack.OrmLite.Sqlite;

namespace ServiceStack.OrmLite.Tests.Support
{
	[TestFixture]
	public class TestScripts
	{
		[Test]
		public void ParseExpression()
		{
			IPropertyInvoker invoker = ReflectionPropertyInvoker.Instance;

			//Expression<Action<object, object>> setPropertyFn =
			//    () => invoker.ConvertValueFn = null;

			//var foo = new Foo();
			//var a = () => foo.Bar = "Hello";
			var a = (Action<Foo, string>)delegate(Foo foo, string value) 
			{
				foo.Bar = "Hello";
			};
		}

		public class Foo
		{
			public string Bar { get; set; }
		}

		[Test]
		public void ExpressionPropertyInvoker_SetPropertyValue_Test()
		{
			var foo = new Foo();
			var propertyInfo = foo.GetType().GetProperty("Bar");

			ExpressionPropertyInvoker.Instance.ConvertValueFn
				= new SqliteOrmLiteDialectProvider().ConvertDbValue;

			ExpressionPropertyInvoker.Instance
				.SetPropertyValue(propertyInfo, typeof(string), foo, "Hello");

			Assert.That(foo.Bar, Is.EqualTo(foo.Bar));
		}

		[Test]
		public void ExpressionPropertyInvoker_GetPropertyValue_Test()
		{
			var foo = new Foo();
			var propertyInfo = foo.GetType().GetProperty("Bar");

			foo.Bar = "Hello";
			var result = ExpressionPropertyInvoker.Instance
				.GetPropertyValue(propertyInfo, foo);

			Assert.That(result, Is.EqualTo(foo.Bar));
		}
	}
}