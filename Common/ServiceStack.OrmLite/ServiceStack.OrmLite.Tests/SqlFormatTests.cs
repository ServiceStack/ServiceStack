using System.Collections.Generic;
using NUnit.Framework;

namespace ServiceStack.OrmLite.Tests
{
	[TestFixture]
	public class SqlFormatTests
		: OrmLiteTestBase
	{
		[Test]
		public void SqlJoin_joins_int_ids()
		{
			var ids = new List<int> { 1, 2, 3 };
			Assert.That(ids.SqlJoin(), Is.EqualTo("1,2,3"));
		}

		[Test]
		public void SqlJoin_joins_string_ids()
		{
			var ids = new List<string> { "1", "2", "3" };
			Assert.That(ids.SqlJoin(), Is.EqualTo("'1','2','3'"));
		}

		[Test]
		public void SqlFormat_can_handle_null_args()
		{
			const string sql = "SELECT Id FROM FOO WHERE Bar = {0}";
			var sqlFormat = sql.SqlFormat(1, null);

			Assert.That(sqlFormat, Is.EqualTo("SELECT Id FROM FOO WHERE Bar = 1"));
		}

	}
}