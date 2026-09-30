using System;
using System.Collections.Generic;
using NUnit.Framework;
using ServiceStack.DataAnnotations;

namespace ServiceStack.OrmLite.Tests;

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

	[Test]
	public void SqlVerifyFragment_throws_on_unclosed_quotes()
	{
		Assert.Throws<ArgumentException>(() =>
			"'; DROP TABLE Users; --".SqlVerifyFragment());
		Assert.Throws<ArgumentException>(() =>
			"Field = 'Value".SqlVerifyFragment());
		Assert.Throws<ArgumentException>(() =>
			"Field = \"Value".SqlVerifyFragment());
		Assert.Throws<ArgumentException>(() =>
			"Field = `Value".SqlVerifyFragment());
	}

	[Test]
	public void GetQuotedName_escapes_embedded_quotes()
	{
		var dialect = OrmLiteConfig.DialectProvider;
		Assert.That(dialect.GetQuotedName("my\"table"), Is.EqualTo("\"my\"\"table\""));
		Assert.That(dialect.GetQuotedName("simple"), Is.EqualTo("\"simple\""));
		Assert.That(dialect.GetQuotedName("\"already_quoted\""), Is.EqualTo("\"already_quoted\""));
	}

	[Test]
	public void ByteArrayConverter_formats_to_hex_string()
	{
		var converter = new ServiceStack.OrmLite.Converters.ByteArrayConverter();
		Assert.That(converter.ToQuotedString(typeof(byte[]), new byte[] { 0xDE, 0xAD, 0xBE, 0xEF }), Is.EqualTo("0xDEADBEEF"));
	}

	[Test]
	public void SqlVerifyFragment_throws_on_symbol_tokens_adjacent_to_identifiers()
	{
		Assert.Throws<ArgumentException>(() => "Id--".SqlVerifyFragment());
		Assert.Throws<ArgumentException>(() => "Id/*x*/".SqlVerifyFragment());
		Assert.Throws<ArgumentException>(() => "Id;TRUNCATE Users".SqlVerifyFragment());
		Assert.Throws<ArgumentException>(() => "Id;GRANT ALL ON x TO public".SqlVerifyFragment());
		Assert.Throws<ArgumentException>(() => "Id@@version".SqlVerifyFragment());
	}

	[Test]
	public void SqlVerifyFragment_does_not_concatenate_tokens_around_quoted_strings()
	{
		Assert.Throws<ArgumentException>(() => "Id IN (select'1'from Users)".SqlVerifyFragment());
		Assert.Throws<ArgumentException>(() => "1'a'drop".SqlVerifyFragment());
	}

	[Test]
	public void SqlVerifyFragment_throws_on_MySql_backslash_escaped_quotes()
	{
		Assert.Throws<ArgumentException>(() => "Name = '\\'' OR 1=1 -- '".SqlVerifyFragment());
		Assert.Throws<ArgumentException>(() => "Name = '\\''; DROP TABLE x; -- '".SqlVerifyFragment());
	}

	[Test]
	public void SqlVerifyFragment_allows_common_legal_fragments()
	{
		"Id".SqlVerifyFragment();
		"Id DESC, Name ASC".SqlVerifyFragment();
		"Id;".SqlVerifyFragment();
		"Id = @Id AND Name = @Name".SqlVerifyFragment();
		"Name LIKE 'A%'".SqlVerifyFragment();
		"Path = 'C:\\temp'".SqlVerifyFragment();
		"Field = 'a -- b; c /* d */'".SqlVerifyFragment();
		"Price * Qty".SqlVerifyFragment();
	}

	[Test]
	public void GetQuotedName_does_not_passthrough_malformed_quoted_names()
	{
		var dialect = SqliteDialect.Provider;
		Assert.That(dialect.GetQuotedName("\"a\"; DROP TABLE b; --\""),
			Is.EqualTo("\"\"\"a\"\"; DROP TABLE b; --\"\"\""));
		Assert.That(dialect.GetQuotedName("\"my\"\"table\""), Is.EqualTo("\"my\"\"table\""));
	}

	[Test]
	public void QuoteSchema_quotes_each_part_of_multi_part_schema()
	{
		var dialect = SqlServer2012Dialect.Provider;
		Assert.That(dialect.QuoteSchema("db.dbo", "Table"), Is.EqualTo("\"db\".\"dbo\".\"Table\""));
		Assert.That(dialect.QuoteSchema("dbo", "Table"), Is.EqualTo("\"dbo\".\"Table\""));
	}

	[Test]
	public void Does_expand_in_params_without_replacing_longer_param_names()
	{
		var dbFactory = new OrmLiteConnectionFactory(":memory:", SqliteDialect.Provider);
		using var db = dbFactory.OpenDbConnection();
		var results = db.SqlList<int>("SELECT 1 WHERE 1 IN (@Ids) AND @IdsCount = 2",
			new { Ids = new[] { 1, 2 }, IdsCount = 2 });
		Assert.That(results, Is.EquivalentTo(new[] { 1 }));

		results = db.SqlList<int>("SELECT 1 WHERE 1 IN (@Ids)", new { Ids = new int?[] { 1, null } });
		Assert.That(results, Is.EquivalentTo(new[] { 1 }));
	}

	[Test]
	public void From_quotes_single_table_names_containing_join()
	{
		var q = SqliteDialect.Provider.SqlExpression<Shared.Person>().From("Rejoinder");
		Assert.That(q.FromExpression, Does.Contain("\"Rejoinder\""));

		q = SqliteDialect.Provider.SqlExpression<Shared.Person>().UnsafeFrom("A JOIN B ON A.Id = B.AId");
		Assert.That(q.FromExpression, Does.Contain("FROM A JOIN B"));
	}
}