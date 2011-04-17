using System;
using System.IO;
using NUnit.Framework;
using ServiceStack.Common.Utils;
using ServiceStack.Text;
using ServiceStack.WebHost.EndPoints.Formats;

namespace ServiceStack.Common.Tests.Formats
{
	[TestFixture]
	public class MarkdownFormatTests
	{
		public class Person
		{
			public string FirstName { get; set; }
			public string LastName { get; set; }
		}

		private MarkdownFormat markdownFormat;

		[SetUp]
		public void OnBeforeEachTest()
		{
			markdownFormat = new MarkdownFormat();
		}
		
		[Test]
		public void Can_load_all_markdown_files()
		{
			//var person = new Person { FirstName = "Demis", LastName = "Bellot" };

			markdownFormat.RegisterMarkdownPages("~/".MapAbsolutePath());

			Assert.That(markdownFormat.Pages.Count, Is.EqualTo(4));
			Assert.That(markdownFormat.PageTemplates.Count, Is.EqualTo(2));
		}

		[Test]
		public void Can_()
		{
			
		}
	}
}