using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using ServiceStack.Text.Controller;

namespace ServiceStack.Common.Tests.Support
{

	[TestFixture]
	public class InvokeSimpleCommandsTests
	{
		SimpleController simpleController;

		public class SimpleController 
		{
			public List<string> History = new List<string>();

			public void Test()
			{
				History.Add("Test()");
			}

			public void TestWithOneStringArg(string value)
			{
				AddFormat("TestWithOneStringArg({0})", value);
			}

			public void TestWithOneGuidArg(Guid value)
			{
				AddFormat("TestWithOneGuidArg({0})", value);
			}

			public void TestWithOneArrayOfStringArg(string[] values)
			{
				AddFormat("TestWithOneArrayOfStringArg({0})", string.Join(",", values));
			}

			public void TestWithOneListOfStringArg(List<string> values)
			{
				AddFormat("TestWithOneListOfStringArg({0})", string.Join(",", values.ToArray()));
			}

			public void OverloadedMethod(int value1)
			{
				AddFormat("OverloadedMethod({0})", value1);
			}

			public void OverloadedMethod(int value1, float value2)
			{
				AddFormat("OverloadedMethod({0},{1})", value1, value2);
			}

			public string HitoryJoined
			{
				get
				{
					return string.Join(",", this.History.ToArray());
				}
			}

			public void Add(string text)
			{
				this.History.Add(text);
			}

			public void AddFormat(string text, params object[] args)
			{
				this.History.Add(string.Format(text, args));
			}
		}

		private CommandProcessor CreateInvokerWithSimpleController()
		{
			simpleController = new SimpleController();
			var objects = new[] { simpleController };
			var actionInvoke = new CommandProcessor(objects);
			return actionInvoke;
		}


		[Test]
		public void Can_call_method_with_no_args()
		{

			try
			{
				var actionInvoke = CreateInvokerWithSimpleController();

				actionInvoke.Invoke(typeof(SimpleController).Name + "://Test");

				Assert.That(simpleController.HitoryJoined, Is.EqualTo("Test()"));
			}
			catch (Exception ex)
			{
				throw ex;
			}
		}

		[Test]
		public void Can_call_method_with_one_string_arg()
		{
			try
			{
				var actionInvoke = CreateInvokerWithSimpleController();

				actionInvoke.Invoke(typeof(SimpleController).Name + "://TestWithOneStringArg/arg1");

				Assert.That(simpleController.HitoryJoined, Is.EqualTo("TestWithOneStringArg(arg1)"));
			}
			catch (Exception ex)
			{
				throw ex;
			}
		}

		[Test]
		public void Can_call_method_with_one_guid_arg()
		{
			try
			{
				var guidValue = Guid.NewGuid();

				var actionInvoke = CreateInvokerWithSimpleController();

				actionInvoke.Invoke(typeof(SimpleController).Name
					+ "://TestWithOneGuidArg/" + guidValue);

				Assert.That(simpleController.HitoryJoined,
					Is.EqualTo("TestWithOneGuidArg(" + guidValue + ")"));
			}
			catch (Exception ex)
			{
				throw ex;
			}
		}

		[Test]
		public void Can_call_method_with_one_array_of_string_arg()
		{
			try
			{
				var arrayValue = new[] { "Hello", "World" };
				var valueString = string.Join(",", arrayValue);

				var actionInvoke = CreateInvokerWithSimpleController();

				actionInvoke.Invoke(typeof(SimpleController).Name
					+ "://TestWithOneArrayOfStringArg/" + valueString);

				Assert.That(simpleController.HitoryJoined,
					Is.EqualTo("TestWithOneArrayOfStringArg(" + valueString + ")"));
			}
			catch (Exception ex)
			{
				throw ex;
			}
		}

		[Test]
		public void Can_call_method_with_one_list_of_string_arg()
		{
			try
			{
				var listValue = new[] { "Hello", "World" }.ToList();
				var valueString = string.Join(",", listValue.ToArray());

				var actionInvoke = CreateInvokerWithSimpleController();

				actionInvoke.Invoke(typeof(SimpleController).Name
					+ "://TestWithOneListOfStringArg/" + valueString);

				Assert.That(simpleController.HitoryJoined,
					Is.EqualTo("TestWithOneListOfStringArg(" + valueString + ")"));
			}
			catch (Exception ex)
			{
				throw ex;
			}
		}

		[Test]
		public void Can_call_overloaded_method_with_one_arg()
		{
			try
			{
				const int intValue = 1;

				var actionInvoke = CreateInvokerWithSimpleController();

				actionInvoke.Invoke(typeof(SimpleController).Name
					+ "://OverloadedMethod/" + intValue);

				Assert.That(simpleController.HitoryJoined,
					Is.EqualTo("OverloadedMethod(" + intValue + ")"));
			}
			catch (Exception ex)
			{
				throw ex;
			}
		}

		[Test]
		public void Can_call_overloaded_method_with_two_args()
		{
			try
			{
				const int intValue = 1;
				const double floatValue = 2.2;

				var actionInvoke = CreateInvokerWithSimpleController();

				actionInvoke.Invoke(typeof(SimpleController).Name
					+ "://OverloadedMethod/" + intValue + "/" + floatValue);

				Assert.That(simpleController.HitoryJoined,
					Is.EqualTo("OverloadedMethod(" + intValue + "," + floatValue + ")"));
			}
			catch (Exception ex)
			{
				throw ex;
			}
		}

	}
}