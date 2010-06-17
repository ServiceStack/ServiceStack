using System;
using System.Collections.Generic;
using NUnit.Framework;
using NUnit.Framework.SyntaxHelpers;

namespace ServiceStack.Text.Tests
{
	[TestFixture]
	public class ReportedIssues
	{

		public T Serialize<T>(T model)
		{
			var strModel = TypeSerializer.SerializeToString(model);
			Console.WriteLine("Len: " + strModel.Length + ", " + strModel);
			var toModel = TypeSerializer.DeserializeFromString<T>(strModel);
			return toModel;
		}


		[Test]
		public void Issue5_Can_serialize_Dictionary_with_null_value()
		{
			var map = new Dictionary<string, string> {
				{"p1","v1"},{"p2","v2"},{"p3",null},
			};

			Serialize(map);
		}

		public abstract class CorrelativeDataBase
		{
			protected CorrelativeDataBase()
			{
				CorrelationIdentifier = GetNextId();
			}

			public Guid CorrelationIdentifier { get; set; }

			protected static Guid GetNextId()
			{
				return Guid.NewGuid();
			}
		}

		public sealed class TestObject : CorrelativeDataBase
		{
			public Type SomeType { get; set; }
			public string SomeString { get; set; }
			public IEnumerable<Type> SomeTypeList { get; set; }
			public IEnumerable<Type> SomeTypeList2 { get; set; }
			public IEnumerable<object> SomeObjectList { get; set; }
		}

		[Test]
		public void Serialize_object_with_type_field()
		{
			var obj = new TestObject
			{
				SomeType = typeof(string),
				SomeString = "Test",
				SomeObjectList = new object[0]
			};

			Serialize(obj);
		}


	}
}