using System;
using Northwind.Common.DataModel;
using NUnit.Framework;
using NUnit.Framework.SyntaxHelpers;
using ServiceStack.Common.Extensions;
using ServiceStack.Common.Text;

namespace ServiceStack.Common.Tests.Text
{
	[TestFixture]
	public class CsvStreamTests
	{
		protected void Log(string fmt, params object[] args)
		{
			Console.WriteLine("{0}", String.Format(fmt, args).Trim());
		}

		[Test]
		public void Can_create_csv_from_Customers()
		{
			NorthwindData.LoadData(false);
			var csv = TypeSerializer.SerializeToCsv(NorthwindData.Customers);
			Log(csv);
			Assert.That(csv, Is.Not.Null);
		}

		[Test]
		public void Can_create_csv_from_Categories()
		{
			NorthwindData.LoadData(false);
			var category = NorthwindFactory.Category(1, "between \"quotes\" here", "with, comma", null);
			var categories =  new[] { category, category };
			var csv = TypeSerializer.SerializeToCsv(categories);
			Log(csv);
			Assert.That(csv, Is.EqualTo("Id,CategoryName,Description,Picture\r\n1,\"between \"\"quotes\"\" here\",\"with, comma\",\r\n1,\"between \"\"quotes\"\" here\",\"with, comma\",\r\n"));
		}

		[Test]
		public void Can_generate_csv_with_invalid_chars()
		{
			var fields = new[] { "1", "2", "3\"", "4", "5\"five,six\"", "7,7.1", "\"7,7.1\"", "8" };
			var csv = TypeSerializer.SerializeToCsv(fields);
			Log(csv);
			Assert.That(csv, Is.EqualTo("1,2,\"3\"\"\",4,\"5\"\"five,six\"\"\",\"7,7.1\",\"\"\"7,7.1\"\"\",8\r\n"));
		}

		[Test]
		public void Can_convert_to_csv_field()
		{
			Assert.That("1".ToCsvField(), Is.EqualTo("1"));
			Assert.That("3\"".ToCsvField(), Is.EqualTo("\"3\"\"\""));
			Assert.That("5\"five,six\"".ToCsvField(), Is.EqualTo("\"5\"\"five,six\"\"\""));
			Assert.That("7,7.1".ToCsvField(), Is.EqualTo("\"7,7.1\""));
			Assert.That("\"7,7.1\"".ToCsvField(), Is.EqualTo("\"\"\"7,7.1\"\"\""));
		}

		[Test]
		public void Can_convert_from_csv_field()
		{
			Assert.That("1".FromCsvField(), Is.EqualTo("1"));
			Assert.That("\"3\"\"\"".FromCsvField(), Is.EqualTo("3\""));
			Assert.That("\"5\"\"five,six\"\"\"".FromCsvField(), Is.EqualTo("5\"five,six\""));
			Assert.That("\"7,7.1\"".FromCsvField(), Is.EqualTo("7,7.1"));
			Assert.That("\"\"\"7,7.1\"\"\"".FromCsvField(), Is.EqualTo("\"7,7.1\""));
		}

	}
}