using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;

namespace ServiceStack.Text.Tests
{

	[TestFixture]
	public class ReflectionExtensionTests
		: TestBase
	{
		public class TestModel
		{
			public TestModel()
			{
				var i = 0;
				this.PublicInt = i++;
				this.PublicGetInt = i++;
				this.PublicSetInt = i++;
				this.PublicIntField = i++;
				this.PrivateInt = i++;
				this.ProtectedInt = i++;
			}

			public int PublicInt { get; set; }

			public int PublicGetInt { get; private set; }

			public int PublicSetInt { private get; set; }

			public int PublicIntField;
			
			private int PrivateInt { get; set; }
			
			protected int ProtectedInt { get; set; }
			
			public int IntMethod()
			{
				return this.PublicInt;
			}
		}

		[Test]
		public void Only_serializes_public_readable_properties()
		{
			var model = new TestModel();
			var modelStr = TypeSerializer.SerializeToString(model);

			Assert.That(modelStr, Is.EqualTo("{PublicInt:0,PublicGetInt:1}"));

			Serialize(model);
		}
	}
}
