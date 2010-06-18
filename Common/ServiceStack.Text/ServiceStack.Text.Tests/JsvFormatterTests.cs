using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using NUnit.Framework;
using ServiceStack.Common.Tests.Models;

namespace ServiceStack.Text.Tests
{
	[TestFixture]
	public class JsvFormatterTests
	{
		[Test]
		public void Can_PrettyFormat_generic_type()
		{
			var model = new ModelWithIdAndName { Id = 1, Name = "Name" };
			var modelStr = model.Dump();

			Assert.That(modelStr, Is.EqualTo("{\r\n\tId: 1,\r\n\tName: Name\r\n}"));
		}

		[Test]
		public void Can_PrettyFormat_object()
		{
			object model = new ModelWithIdAndName { Id = 1, Name = "Name" };
			var modelStr = model.Dump();

			Assert.That(modelStr, Is.EqualTo("{\r\n\tId: 1,\r\n\tName: Name\r\n}"));
		}

		internal class TestModel
		{
			public TestModel()
			{
				this.Int = 1;
				this.String = "One";
				this.DateTime = DateTime.UtcNow.Date;
				this.Guid = Guid.NewGuid();
				this.EmptyIntList = new List<int>();
				this.IntList = new List<int> { 1, 2, 3 };
				this.StringList = new List<string> { "one", "two", "three" };
				this.StringIntMap = new Dictionary<string, int>
            		{
            			{"a", 1},{"b", 2},{"c", 3},
            		};
			}

			public int Int { get; set; }
			public string String { get; set; }
			public DateTime DateTime { get; set; }
			public Guid Guid { get; set; }
			public List<int> EmptyIntList { get; set; }
			public List<int> IntList { get; set; }
			public List<string> StringList { get; set; }
			public Dictionary<string, int> StringIntMap { get; set; }
		}

		[Test]
		public void Can_DumpModel()
		{
			var model = new TestModel();
			Console.WriteLine(model.Dump());
		}

	}

}
