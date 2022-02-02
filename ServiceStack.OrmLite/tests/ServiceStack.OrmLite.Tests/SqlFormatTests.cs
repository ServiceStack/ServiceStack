using System;
using System.Collections.Generic;
using NUnit.Framework;
using ServiceStack.DataAnnotations;

namespace ServiceStack.OrmLite.Tests
{
	[TestFixture]
	public class SqlFormatTests 
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
			var sqlFormat = sql.SqlFmt(SqliteDialect.Provider, 1, null);

			Assert.That(sqlFormat, Is.EqualTo("SELECT Id FROM FOO WHERE Bar = 1"));
		}

	    [Test]
	    public void Can_strip_quoted_text_from_sql()
	    {
            Assert.That("SELECT * FROM 'DropTable' WHERE Field = 'selectValue'".StripQuotedStrings(),
                Is.EqualTo("SELECT * FROM  WHERE Field = "));
            Assert.That("SELECT * FROM \"DropTable\" WHERE Field = \"selectValue\"".StripQuotedStrings('"'),
                Is.EqualTo("SELECT * FROM  WHERE Field = "));
            Assert.That("SELECT * FROM 'DropTable' WHERE Field = \"selectValue\"".StripQuotedStrings('\'').StripQuotedStrings('"'),
                Is.EqualTo("SELECT * FROM  WHERE Field = "));
            Assert.That("SELECT * FROM 'Drop''Table' WHERE Field = \"select\"\"Value\"".StripQuotedStrings('\'').StripQuotedStrings('"'),
                Is.EqualTo("SELECT * FROM  WHERE Field = "));
        }

        [Test]
        public void SqlVerifyFragment_allows_legal_sql_fragments()
        {
            "Field = 'DropTable' OR Field = 'selectValue'".SqlVerifyFragment();
            "Field = \"DropTable\" OR Field = \"selectValue\"".SqlVerifyFragment();
            "Field = 'DropTable' OR Field = \"selectValue\"".SqlVerifyFragment();
            "Field = 'Drop''Table' OR Field = \"select\"\"Value\"".SqlVerifyFragment();
        }

        [Test]
        public void SqlVerifyFragment_throws_on_illegal_sql_fragments()
        {
            Assert.Throws<ArgumentException>(() =>
                "Field = 'Value';--'".SqlVerifyFragment());
            Assert.Throws<ArgumentException>(() =>
                "Field = 'Value';Drop Table;--'".SqlVerifyFragment());
            Assert.Throws<ArgumentException>(() =>
                "Field = 'Value';select Table, '' FROM A".SqlVerifyFragment());
            Assert.Throws<ArgumentException>(() =>
                "Field = 'Value';delete Table where '' = ''".SqlVerifyFragment());
        }

        [Test]
        public void SqlParam_sanitizes_param_values()
        {
            Assert.That("' or Field LIKE '%".SqlParam(), Is.EqualTo("'' or Field LIKE ''%"));
        }
        
        [Alias("profile_extended")]
        public class ProflieExtended
        {
            public int Id { get; set; }
        }

	    [Test]
	    public void Does_allow_illegal_tokens_in_quoted_MySql_table_names()
	    {
            var sql = "FROM `profile_extended`";
	        sql.SqlVerifyFragment();
	    }
    }
}