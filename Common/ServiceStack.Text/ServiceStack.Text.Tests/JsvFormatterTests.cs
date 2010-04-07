using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using NUnit.Framework;
using NUnit.Framework.SyntaxHelpers;
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

		internal class DumpModel
		{
			public DumpModel()
			{
				this.Int = 1;
				this.String = "One";
				this.EmptyIntList = new List<int>();
				this.IntList = new List<int> { 1, 2, 3 };
				this.StringList = new List<string> { "one", "two", "three" };
				this.DateTime = DateTime.UtcNow;
			}

			public int Int { get; set; }
			public string String { get; set; }
			public DateTime DateTime { get; set; }
			public List<int> EmptyIntList { get; set; }
			public List<int> IntList { get; set; }
			public List<string> StringList { get; set; }
		}

		[Test]
		public void Can_DumpModel()
		{
			var model = new DumpModel();
			var modelStr = model.Dump();
			Console.WriteLine(modelStr);
		}

	}

}
