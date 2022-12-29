using System.Collections.Generic;
using System.Data;
using System.Linq;
using NUnit.Framework;
using ServiceStack.Text;

namespace ServiceStack.OrmLite.Tests
{
    public class DummyTable
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }

    [TestFixtureOrmLiteDialects(Dialect.AnySqlServer)]
    public class SqlServerProviderTests : OrmLiteProvidersTestBase
    {
        public SqlServerProviderTests(DialectContext context) : base(context) {}

        private IDbConnection db;

        [OneTimeSetUp]
        public void TestFixtureSetUp()
        {
            db = OpenDbConnection();
        }

        [OneTimeTearDown]
        public void TearDown()
        {
            db.Dispose();
        }

        [Test]
        public void Can_SqlList_StoredProc_returning_Table()
        {
            var sql = @"CREATE PROCEDURE dbo.DummyTable
    @Times integer
AS
BEGIN
    SET NOCOUNT ON;
 
    CREATE TABLE #Temp
    (
        Id   integer NOT NULL,
        Name nvarchar(50) COLLATE DATABASE_DEFAULT NOT NULL
    );
 
	declare @i int
	set @i=1
	WHILE @i < @Times
	BEGIN
	    INSERT INTO #Temp (Id, Name) VALUES (@i, CAST(@i as nvarchar))
		SET @i = @i + 1
	END

	SELECT * FROM #Temp;
	 
    DROP TABLE #Temp;
END;";
            db.ExecuteSql("IF OBJECT_ID('DummyTable') IS NOT NULL DROP PROC DummyTable");
            db.ExecuteSql(sql);

            var expected = 0;
            10.Times(i => expected += i);

            var results = db.SqlList<DummyTable>("EXEC DummyTable @Times", new { Times = 10 });
            results.PrintDump();
            Assert.That(results.Sum(x => x.Id), Is.EqualTo(expected));

            results = db.SqlList<DummyTable>("EXEC DummyTable 10");
            Assert.That(results.Sum(x => x.Id), Is.EqualTo(expected));

            results = db.SqlList<DummyTable>("EXEC DummyTable @Times", new Dictionary<string, object> { { "Times", 10 } });
            Assert.That(results.Sum(x => x.Id), Is.EqualTo(expected));
        }

        [Test]
        public void Can_SqlColumn_StoredProc_returning_Column()
        {
            var sql = @"CREATE PROCEDURE dbo.DummyColumn
    @Times integer
AS
BEGIN
    SET NOCOUNT ON;
 
    CREATE TABLE #Temp
    (
        Id   integer NOT NULL,
    );
 
	declare @i int
	set @i=1
	WHILE @i < @Times
	BEGIN
	    INSERT INTO #Temp (Id) VALUES (@i)
		SET @i = @i + 1
	END

	SELECT * FROM #Temp;
	 
    DROP TABLE #Temp;
END;";
            db.ExecuteSql("IF OBJECT_ID('DummyColumn') IS NOT NULL DROP PROC DummyColumn");
            db.ExecuteSql(sql);

            var expected = 0;
            10.Times(i => expected += i);

            var results = db.SqlColumn<int>("EXEC DummyColumn @Times", new { Times = 10 });
            results.PrintDump();
            Assert.That(results.Sum(), Is.EqualTo(expected));

            results = db.SqlColumn<int>("EXEC DummyColumn 10");
            Assert.That(results.Sum(), Is.EqualTo(expected));

            results = db.SqlColumn<int>("EXEC DummyColumn @Times", new Dictionary<string, object> { { "Times", 10 } });
            Assert.That(results.Sum(), Is.EqualTo(expected));
        }

        [Test]
        public void Can_SqlColumn_StoredProc_returning_StringColumn()
        {
            var sql = @"CREATE PROCEDURE dbo.DummyColumn
    @Times integer
AS
BEGIN
    SET NOCOUNT ON;
 
    CREATE TABLE #Temp
    (
        Name nvarchar(50) not null
    );
 
	declare @i int
	set @i=0
	WHILE @i < @Times
	BEGIN
	    INSERT INTO #Temp (Name) VALUES (CAST(NEWID() AS nvarchar(50)))
		SET @i = @i + 1
	END

	SELECT * FROM #Temp;
	 
    DROP TABLE #Temp;
END;";
            db.ExecuteSql("IF OBJECT_ID('DummyColumn') IS NOT NULL DROP PROC DummyColumn");
            db.ExecuteSql(sql);

	    // This produces a compiler error
            var results = db.SqlColumn<string>("EXEC DummyColumn @Times", new { Times = 10 });
            results.PrintDump();
            Assert.That(results.Count, Is.EqualTo(10));
        }

        [Test]
        public void Can_SqlScalar_StoredProc_returning_Scalar()
        {
            var sql = @"CREATE PROCEDURE dbo.DummyScalar
    @Times integer
AS
BEGIN
    SET NOCOUNT ON;

	SELECT @Times AS Id
END;";
            db.ExecuteSql("IF OBJECT_ID('DummyScalar') IS NOT NULL DROP PROC DummyScalar");
            db.ExecuteSql(sql);

            const int expected = 10;

            var result = db.SqlScalar<int>("EXEC DummyScalar @Times", new { Times = 10 });
            result.PrintDump();
            Assert.That(result, Is.EqualTo(expected));

            result = db.SqlScalar<int>("EXEC DummyScalar 10");
            Assert.That(result, Is.EqualTo(expected));

            result = db.SqlScalar<int>("EXEC DummyScalar @Times", new Dictionary<string, object> { { "Times", 10 } });
            Assert.That(result, Is.EqualTo(expected));

            result = db.SqlScalar<int>("SELECT 10");
            Assert.That(result, Is.EqualTo(expected));
        }

        [Test]
        public void Can_SqlScalar_StoredProc_passing_null_parameter()
        {
            const string sql = @"CREATE PROCEDURE dbo.DummyScalar
    @Times integer
AS
BEGIN
    SET NOCOUNT ON;

	SELECT @Times AS Id
END;";
            db.ExecuteSql("IF OBJECT_ID('DummyScalar') IS NOT NULL DROP PROC DummyScalar");
            db.ExecuteSql(sql);

            var result = db.SqlScalar<int?>("EXEC DummyScalar @Times", new { Times = (int?)null });
            Assert.That(result, Is.Null);

            result = db.SqlScalar<int?>("EXEC DummyScalar NULL");
            Assert.That(result, Is.Null);

            result = db.SqlScalar<int?>("EXEC DummyScalar @Times", new Dictionary<string, object> { { "Times", null } });
            Assert.That(result, Is.Null);

            result = db.SqlScalar<int?>("SELECT NULL");
            Assert.That(result, Is.Null);
        }

        [Test]
        public void Can_SqlList_StoredProc_passing_null_parameter()
        {
            const string sql = @"CREATE PROCEDURE dbo.DummyProc
    @Name nvarchar(50)
AS
BEGIN
    SET NOCOUNT ON;

	SELECT 1 AS Id, 'Name_1' AS Name WHERE @Name IS NULL
    UNION ALL
	SELECT 2 AS Id, 'Name_2' AS Name WHERE @Name IS NOT NULL
    UNION ALL
	SELECT 3 AS Id, 'Name_3' AS Name WHERE @Name IS NULL

END;";
            db.ExecuteSql("IF OBJECT_ID('DummyProc') IS NOT NULL DROP PROC DummyProc");
            db.ExecuteSql(sql);

            var results = db.SqlColumn<DummyTable>("EXEC DummyProc @Name", new { Name = (string)null });
            Assert.That(results.Count, Is.EqualTo(2));
            Assert.That(results[0].Name, Is.EqualTo("Name_1"));
            Assert.That(results[1].Name, Is.EqualTo("Name_3"));
        }

        [Test]
        public void Can_SqlList_StoredProc_receiving_only_first_column_and_null()
        {
            const string sql = @"CREATE PROCEDURE dbo.DummyScalar
AS
BEGIN
    SET NOCOUNT ON;

	SELECT NULL AS Id, 'Name_1' AS Name
    UNION ALL
	SELECT NULL AS Id, 'Name_2' AS Name
END;";
            db.ExecuteSql("IF OBJECT_ID('DummyScalar') IS NOT NULL DROP PROC DummyScalar");
            db.ExecuteSql(sql);

            var results = db.SqlColumn<int?>("EXEC DummyScalar");
            Assert.That(results.Count, Is.EqualTo(2));
            Assert.That(results[0], Is.Null);
            Assert.That(results[1], Is.Null);
        }
    }
}
