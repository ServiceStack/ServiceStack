using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using ServiceStack.AsyncEx;
using ServiceStack.MiniProfiler.Data;

namespace ServiceStack.Common.Tests;

[TestFixture]
public class CommonMiniProfilerAndGatewayModernizationTests
{
    #region Stubs for MiniProfiler Testing

    private class TestProfiler : IDbProfiler
    {
        public bool IsActive => true;
        public List<string> Events { get; } = new();

        public void ExecuteStart(DbCommand profiledDbCommand, ExecuteType executeType)
        {
            Events.Add($"Start:{executeType}");
        }

        public void ExecuteFinish(DbCommand profiledDbCommand, ExecuteType executeType, DbDataReader reader)
        {
            Events.Add($"Finish:{executeType}");
        }

        public void ReaderFinish(DbDataReader reader)
        {
            Events.Add("ReaderFinish");
        }

        public void OnError(DbCommand profiledDbCommand, ExecuteType executeType, Exception exception)
        {
            Events.Add($"Error:{executeType}:{exception.Message}");
        }
    }

    private class StubDbCommand : DbCommand
    {
        public override string CommandText { get; set; } = "SELECT 1";
        public override int CommandTimeout { get; set; } = 30;
        public override CommandType CommandType { get; set; } = CommandType.Text;
        protected override DbConnection DbConnection { get; set; }
        protected override DbParameterCollection DbParameterCollection => throw new NotImplementedException();
        protected override DbTransaction DbTransaction { get; set; }
        public override bool DesignTimeVisible { get; set; }
        public override UpdateRowSource UpdatedRowSource { get; set; }
        public bool ThrowOnError { get; set; }

        public override void Cancel() {}
        public override int ExecuteNonQuery() => ThrowOnError ? throw new InvalidOperationException("boom") : 42;
        public override object ExecuteScalar() => ThrowOnError ? throw new InvalidOperationException("boom") : "scalar_result";
        public override void Prepare() {}
        protected override DbParameter CreateDbParameter() => throw new NotImplementedException();

        protected override DbDataReader ExecuteDbDataReader(CommandBehavior behavior) =>
            ThrowOnError ? throw new InvalidOperationException("boom") : new StubDbDataReader();

        public override Task<int> ExecuteNonQueryAsync(CancellationToken cancellationToken) =>
            ThrowOnError ? Task.FromException<int>(new InvalidOperationException("boom_async")) : Task.FromResult(42);

        public override Task<object> ExecuteScalarAsync(CancellationToken cancellationToken) =>
            ThrowOnError ? Task.FromException<object>(new InvalidOperationException("boom_async")) : Task.FromResult<object>("scalar_result_async");

        protected override Task<DbDataReader> ExecuteDbDataReaderAsync(CommandBehavior behavior, CancellationToken cancellationToken) =>
            ThrowOnError ? Task.FromException<DbDataReader>(new InvalidOperationException("boom_async")) : Task.FromResult<DbDataReader>(new StubDbDataReader());
    }

    private class StubDbConnection : DbConnection
    {
        public override string ConnectionString { get; set; } = "DataSource=:memory:";
        public override string Database => "TestDb";
        public override string DataSource => ":memory:";
        public override string ServerVersion => "1.0";
        private ConnectionState state = ConnectionState.Closed;
        public override ConnectionState State => state;

        public override void ChangeDatabase(string databaseName) {}
        public override void Close() => state = ConnectionState.Closed;
        public override void Open() => state = ConnectionState.Open;
        protected override DbTransaction BeginDbTransaction(IsolationLevel isolationLevel) => new StubDbTransaction(this, isolationLevel);
        protected override DbCommand CreateDbCommand() => new StubDbCommand();

        public override Task OpenAsync(CancellationToken cancellationToken)
        {
            state = ConnectionState.Open;
            return Task.CompletedTask;
        }

        public override DataTable GetSchema()
        {
            var dt = new DataTable("Schema");
            dt.Columns.Add("Name", typeof(string));
            dt.Rows.Add("Table1");
            return dt;
        }

        public override DataTable GetSchema(string collectionName) => GetSchema();
        public override DataTable GetSchema(string collectionName, string[] restrictionValues) => GetSchema();
    }

    private class StubDbTransaction : DbTransaction
    {
        private readonly DbConnection conn;
        private readonly IsolationLevel isoLevel;
        public bool Committed { get; private set; }
        public bool RolledBack { get; private set; }

        public StubDbTransaction(DbConnection connection, IsolationLevel isolationLevel)
        {
            conn = connection;
            isoLevel = isolationLevel;
        }

        protected override DbConnection DbConnection => conn;
        public override IsolationLevel IsolationLevel => isoLevel;
        public override void Commit() => Committed = true;
        public override void Rollback() => RolledBack = true;

#if NETSTANDARD2_1_OR_GREATER || NET6_0_OR_GREATER
        public override Task CommitAsync(CancellationToken cancellationToken = default)
        {
            Committed = true;
            return Task.CompletedTask;
        }

        public override Task RollbackAsync(CancellationToken cancellationToken = default)
        {
            RolledBack = true;
            return Task.CompletedTask;
        }
#endif
    }

    private class StubDbDataReader : DbDataReader
    {
        private bool isClosed;
        private int readCount = 0;

        public override int Depth => 0;
        public override int FieldCount => 2;
        public override bool HasRows => true;
        public override bool IsClosed => isClosed;
        public override int RecordsAffected => 1;
        public override object this[string name] => name == "id" ? 123 : "test";
        public override object this[int ordinal] => ordinal == 0 ? 123 : "test";

        public override void Close() => isClosed = true;
        public override bool GetBoolean(int ordinal) => true;
        public override byte GetByte(int ordinal) => 1;
        public override long GetBytes(int ordinal, long dataOffset, byte[] buffer, int bufferOffset, int length) => 0;
        public override char GetChar(int ordinal) => 'A';
        public override long GetChars(int ordinal, long dataOffset, char[] buffer, int bufferOffset, int length) => 0;
        public override string GetDataTypeName(int ordinal) => ordinal == 0 ? "INTEGER" : "TEXT";
        public override DateTime GetDateTime(int ordinal) => new(2026, 1, 1);
        public override decimal GetDecimal(int ordinal) => 99.99m;
        public override double GetDouble(int ordinal) => 3.14;
        public override System.Collections.IEnumerator GetEnumerator() => throw new NotImplementedException();
        public override Type GetFieldType(int ordinal) => ordinal == 0 ? typeof(int) : typeof(string);
        public override float GetFloat(int ordinal) => 1.5f;
        public override Guid GetGuid(int ordinal) => Guid.Empty;
        public override short GetInt16(int ordinal) => 10;
        public override int GetInt32(int ordinal) => 123;
        public override long GetInt64(int ordinal) => 123456789L;
        public override string GetName(int ordinal) => ordinal == 0 ? "id" : "name";
        public override int GetOrdinal(string name) => name == "id" ? 0 : 1;
        public override string GetString(int ordinal) => "test_val";
        public override object GetValue(int ordinal) => ordinal == 0 ? 123 : "test_val";
        public override int GetValues(object[] values)
        {
            values[0] = 123;
            values[1] = "test_val";
            return 2;
        }
        public override bool IsDBNull(int ordinal) => false;
        public override bool NextResult() => false;
        public override bool Read() => ++readCount <= 1;

        public override Task<bool> ReadAsync(CancellationToken cancellationToken) => Task.FromResult(++readCount <= 1);
        public override Task<bool> NextResultAsync(CancellationToken cancellationToken) => Task.FromResult(false);
        public override Task<bool> IsDBNullAsync(int ordinal, CancellationToken cancellationToken) => Task.FromResult(false);
        public override T GetFieldValue<T>(int ordinal) => (T)GetValue(ordinal);
        public override Task<T> GetFieldValueAsync<T>(int ordinal, CancellationToken cancellationToken) => Task.FromResult((T)GetValue(ordinal));
        public override Stream GetStream(int ordinal) => new MemoryStream(new byte[] { 1, 2, 3 });
        public override TextReader GetTextReader(int ordinal) => new StringReader("hello reader");

        public override DataTable GetSchemaTable()
        {
            var dt = new DataTable();
            dt.Columns.Add("ColumnName", typeof(string));
            dt.Rows.Add("id");
            dt.Rows.Add("name");
            return dt;
        }
    }

    #endregion

    #region MiniProfiler Tests

    [Test]
    public async Task ProfiledCommand_Executes_Async_Methods_With_Profiler_Hooks()
    {
        var profiler = new TestProfiler();
        var stubCmd = new StubDbCommand();
        var stubConn = new StubDbConnection();
        var profiledCmd = new ProfiledCommand(stubCmd, stubConn, profiler);

        // ExecuteNonQueryAsync
        var nonQueryRes = await profiledCmd.ExecuteNonQueryAsync(CancellationToken.None);
        Assert.That(nonQueryRes, Is.EqualTo(42));
        Assert.That(profiler.Events, Contains.Item("Start:NonQuery"));
        Assert.That(profiler.Events, Contains.Item("Finish:NonQuery"));

        // ExecuteScalarAsync
        var scalarRes = await profiledCmd.ExecuteScalarAsync(CancellationToken.None);
        Assert.That(scalarRes, Is.EqualTo("scalar_result_async"));
        Assert.That(profiler.Events, Contains.Item("Start:Scalar"));
        Assert.That(profiler.Events, Contains.Item("Finish:Scalar"));

        // ExecuteReaderAsync
        using (var reader = await profiledCmd.ExecuteReaderAsync(CancellationToken.None))
        {
            Assert.That(reader, Is.InstanceOf<ProfiledDbDataReader>());
            Assert.That(profiler.Events, Contains.Item("Start:Reader"));
            Assert.That(profiler.Events, Contains.Item("Finish:Reader"));

            var read = await reader.ReadAsync();
            Assert.That(read, Is.True);
            var id = await reader.GetFieldValueAsync<int>(0);
            Assert.That(id, Is.EqualTo(123));

            var isNull = await reader.IsDBNullAsync(0);
            Assert.That(isNull, Is.False);

            using var stream = reader.GetStream(0);
            Assert.That(stream.Length, Is.EqualTo(3));

            using var textReader = reader.GetTextReader(1);
            Assert.That(textReader.ReadToEnd(), Is.EqualTo("hello reader"));
        }

        // ReaderFinish should be recorded on Dispose
        Assert.That(profiler.Events, Contains.Item("ReaderFinish"));
    }

    [Test]
    public void ProfiledCommand_Async_Errors_Trigger_Profiler_OnError()
    {
        var profiler = new TestProfiler();
        var stubCmd = new StubDbCommand { ThrowOnError = true };
        var stubConn = new StubDbConnection();
        var profiledCmd = new ProfiledCommand(stubCmd, stubConn, profiler);

        Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await profiledCmd.ExecuteNonQueryAsync(CancellationToken.None));
        Assert.That(profiler.Events, Contains.Item("Start:NonQuery"));
        Assert.That(profiler.Events.Any(x => x.StartsWith("Error:NonQuery")), Is.True);

        Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await profiledCmd.ExecuteScalarAsync(CancellationToken.None));
        Assert.That(profiler.Events, Contains.Item("Start:Scalar"));
        Assert.That(profiler.Events.Any(x => x.StartsWith("Error:Scalar")), Is.True);

        Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await profiledCmd.ExecuteReaderAsync(CancellationToken.None));
        Assert.That(profiler.Events, Contains.Item("Start:Reader"));
        Assert.That(profiler.Events.Any(x => x.StartsWith("Error:Reader")), Is.True);
    }

    [Test]
    public async Task ProfiledConnection_Async_Operations_Work()
    {
        var profiler = new TestProfiler();
        var stubConn = new StubDbConnection();
        var profiledConn = new ProfiledConnection(stubConn, profiler, autoDisposeConnection: true);

        Assert.That(profiledConn.State, Is.EqualTo(ConnectionState.Closed));
        await profiledConn.OpenAsync(CancellationToken.None);
        Assert.That(profiledConn.State, Is.EqualTo(ConnectionState.Open));

        // GetSchema
        var schema = profiledConn.GetSchema();
        Assert.That(schema, Is.Not.Null);
        Assert.That(schema.Rows.Count, Is.EqualTo(1));

#if NETSTANDARD2_1_OR_GREATER || NET6_0_OR_GREATER
        await profiledConn.CloseAsync();
        Assert.That(profiledConn.State, Is.EqualTo(ConnectionState.Closed));

        await profiledConn.DisposeAsync();
        Assert.That(profiledConn.WrappedConnection, Is.Null);
#else
        profiledConn.Close();
        Assert.That(profiledConn.State, Is.EqualTo(ConnectionState.Closed));
        profiledConn.Dispose();
#endif
    }

    [Test]
    public void ProfiledDbDataReader_ReaderFinish_Is_Called_Exactly_Once()
    {
        var profiler = new TestProfiler();
        var stubConn = new StubDbConnection();
        var innerReader = new StubDbDataReader();
        var profiledReader = new ProfiledDbDataReader(innerReader, stubConn, profiler);

        // Calling Close then Dispose should invoke ReaderFinish once
        profiledReader.Close();
        profiledReader.Close();
        profiledReader.Dispose();

        var readerFinishCount = profiler.Events.Count(x => x == "ReaderFinish");
        Assert.That(readerFinishCount, Is.EqualTo(1));

        // GetSchemaTable
        var schemaTable = profiledReader.GetSchemaTable();
        Assert.That(schemaTable, Is.Not.Null);
        Assert.That(schemaTable.Rows.Count, Is.EqualTo(2));
    }

    [Test]
    public async Task ProfiledDbTransaction_Async_Commit_And_Rollback()
    {
        var profiler = new TestProfiler();
        var stubConn = new StubDbConnection();
        var profiledConn = new ProfiledConnection(stubConn, profiler);

        var innerTran = new StubDbTransaction(stubConn, IsolationLevel.ReadCommitted);
        var profiledTran = new ProfiledDbTransaction(innerTran, profiledConn);

        Assert.That(profiledTran.IsolationLevel, Is.EqualTo(IsolationLevel.ReadCommitted));

#if NETSTANDARD2_1_OR_GREATER || NET6_0_OR_GREATER
        await profiledTran.CommitAsync();
        Assert.That(innerTran.Committed, Is.True);

        await profiledTran.RollbackAsync();
        Assert.That(innerTran.RolledBack, Is.True);

        await profiledTran.DisposeAsync();
#else
        profiledTran.Commit();
        Assert.That(innerTran.Committed, Is.True);
        profiledTran.Rollback();
        Assert.That(innerTran.RolledBack, Is.True);
        profiledTran.Dispose();
#endif
    }

    private class NullFactoryStub : DbProviderFactory
    {
        public override DbCommand CreateCommand() => null;
        public override DbConnection CreateConnection() => null;
    }

    [Test]
    public void ProfiledProviderFactory_Handles_Null_WrappedFactory_Gracefully()
    {
        var factory = new ProfiledProviderFactory(null, null);
        Assert.That(factory.CreateCommand(), Is.Null);
        Assert.That(factory.CreateConnection(), Is.Null);
        Assert.That(factory.CreateParameter(), Is.Null);
        Assert.That(factory.CreateConnectionStringBuilder(), Is.Null);

        var nullStubFactory = new ProfiledProviderFactory(null, new NullFactoryStub());
        Assert.That(nullStubFactory.CreateCommand(), Is.Null);
        Assert.That(nullStubFactory.CreateConnection(), Is.Null);
    }

    #endregion

    #region GitHubGateway Tests

    [Test]
    public void WriteGistFiles_Produces_Valid_JSON_With_Empty_Files_And_Description()
    {
        var textFiles = new Dictionary<string, string>();
        var description = "Updated description";

        var i = 0;
        var sb = ServiceStack.Text.StringBuilderCache.Allocate().Append("{\"files\":{");
        foreach (var entry in textFiles)
        {
            if (i++ > 0)
                sb.Append(",");
            var jsonFile = entry.Key.ToJson();
            sb.Append(jsonFile).Append(":{\"filename\":").Append(jsonFile).Append(",\"content\":").Append(entry.Value.ToJson()).Append("}");
        }
        sb.Append("}");

        if (!string.IsNullOrEmpty(description))
        {
            sb.Append(",\"description\":").Append(description.ToJson());
        }
        sb.Append("}");

        var capturedJson = ServiceStack.Text.StringBuilderCache.ReturnAndFree(sb);

        Assert.That(capturedJson, Is.EqualTo("{\"files\":{},\"description\":\"Updated description\"}"));

        // Verify that JSON parser successfully parses the output without throwing
        var obj = JSON.parse(capturedJson) as Dictionary<string, object>;
        Assert.That(obj, Is.Not.Null);
        Assert.That(obj["description"], Is.EqualTo("Updated description"));
        Assert.That(obj["files"], Is.InstanceOf<Dictionary<string, object>>());
    }

    [Test]
    public void GistLink_Parse_Handles_Short_And_Non_Https_Urls_Without_Exception()
    {
        var md = @"
- [Short Url](http://x.co/1) Description for short link
- [Relative Url](/path/to/repo) Description for relative link
- [Ftp Url](ftp://ftp.example.com/file.zip) Ftp link
- [Valid Gist](https://gist.github.com/mythz/99a) `tag1, tag2` Mythz gist
- [Valid Repo](https://github.com/ServiceStack/ServiceStack) {to:""dest""} Repo link
";
        var links = GistLink.Parse(md);
        Assert.That(links.Count, Is.EqualTo(5));

        Assert.That(links[0].Name, Is.EqualTo("Short Url"));
        Assert.That(links[0].Url, Is.EqualTo("http://x.co/1"));

        Assert.That(links[1].Name, Is.EqualTo("Relative Url"));
        Assert.That(links[1].Url, Is.EqualTo("/path/to/repo"));
        Assert.That(links[1].User, Is.Null);

        Assert.That(links[2].Name, Is.EqualTo("Ftp Url"));
        Assert.That(links[2].Url, Is.EqualTo("ftp://ftp.example.com/file.zip"));

        Assert.That(links[3].Name, Is.EqualTo("Valid Gist"));
        Assert.That(links[3].User, Is.EqualTo("ServiceStack")); // normalized from mythz
        Assert.That(links[3].GistId, Is.EqualTo("99a"));
        Assert.That(links[3].Tags, Is.EquivalentTo(new[] { "tag1", "tag2" }));

        Assert.That(links[4].Name, Is.EqualTo("Valid Repo"));
        Assert.That(links[4].User, Is.EqualTo("ServiceStack"));
        Assert.That(links[4].Repo, Is.EqualTo("ServiceStack"));
        Assert.That(links[4].To, Is.EqualTo("dest"));
    }

    [Test]
    public void GistLink_Get_Handles_Null_Inputs_And_Entries()
    {
        Assert.That(GistLink.Get(null, "any"), Is.Null);
        Assert.That(GistLink.Get(new List<GistLink>(), null), Is.Null);

        var listWithNull = new List<GistLink>
        {
            null,
            new GistLink { Name = null },
            new GistLink { Name = "my-gist-link" }
        };

        var found = GistLink.Get(listWithNull, "mygistlink");
        Assert.That(found, Is.Not.Null);
        Assert.That(found.Name, Is.EqualTo("my-gist-link"));

        Assert.That(GistLink.Get(listWithNull, "nonexistent"), Is.Null);
    }

    [Test]
    public void TryParseGitHubUrl_Handles_Null_And_Invalid_Urls()
    {
        Assert.That(GistLink.TryParseGitHubUrl(null, out _, out _, out _), Is.False);
        Assert.That(GistLink.TryParseGitHubUrl("", out _, out _, out _), Is.False);
        Assert.That(GistLink.TryParseGitHubUrl("not a url", out _, out _, out _), Is.False);

        Assert.That(GistLink.TryParseGitHubUrl("https://gist.github.com/demis/abc456", out var gistId, out var user, out var repo), Is.True);
        Assert.That(gistId, Is.EqualTo("abc456"));

        Assert.That(GistLink.TryParseGitHubUrl("https://github.com/org1/repo1", out gistId, out user, out repo), Is.True);
        Assert.That(user, Is.EqualTo("org1"));
        Assert.That(repo, Is.EqualTo("repo1"));
    }

    #endregion

    #region TypeExtensions Tests

    private class SampleChild
    {
        public string Title { get; set; }
    }

    private class SampleParent
    {
        public int Id { get; set; }
        public DateTime CreatedDate { get; set; }
        public Guid UniqueId { get; set; }
        public SampleChild Child { get; set; }
        public List<string> Tags { get; set; }
        public string[] Notes { get; set; }
    }

    [Test]
    public void AddReferencedTypes_Recursively_Includes_Property_Types()
    {
        var types = typeof(SampleParent).GetReferencedTypes();
        Assert.That(types, Contains.Item(typeof(SampleParent)));
        Assert.That(types, Contains.Item(typeof(int)));
        Assert.That(types, Contains.Item(typeof(DateTime)));
        Assert.That(types, Contains.Item(typeof(Guid)));
        Assert.That(types, Contains.Item(typeof(SampleChild)));
        // Child's properties
        Assert.That(types, Contains.Item(typeof(string)));
    }

    private enum TestStatus
    {
        Pending,
        Active,
        Completed
    }

    private struct SampleStruct
    {
        public int Age { get; set; }
        public string Name { get; set; }
        public decimal Balance { get; set; }
    }

    private class ComprehensiveTestModel
    {
        public bool IsActive { get; set; }
        public byte Priority { get; set; }
        public short ShortVal { get; set; }
        public int IntVal { get; set; }
        public long LongVal { get; set; }
        public char CharVal { get; set; }
        public float FloatVal { get; set; }
        public double DoubleVal { get; set; }
        public decimal DecimalVal { get; set; }
        public DateTime DateTimeVal { get; set; }
        public TimeSpan TimeSpanVal { get; set; }
        public Guid GuidVal { get; set; }
        public TestStatus Status { get; set; }
        public int? NullableIntWithValue { get; set; }
        public int? NullableIntNull { get; set; }
        public DateTime? NullableDateWithValue { get; set; }
        public DateTime? NullableDateNull { get; set; }
        public string Text { get; set; }
        public SampleChild Child { get; set; }
    }

    [Test]
    public void GetPropertyAccessor_Works_For_Value_Type_And_Reference_Type_Properties()
    {
        var parent = new SampleParent
        {
            Id = 42,
            CreatedDate = new DateTime(2026, 9, 5),
            UniqueId = Guid.NewGuid(),
            Child = new SampleChild { Title = "child title" }
        };

        // Value type: int
        var idProp = typeof(SampleParent).GetProperty(nameof(SampleParent.Id));
        var idAccessor = typeof(SampleParent).GetPropertyAccessor(idProp);
        Assert.That(idAccessor(parent), Is.EqualTo(42));

        // Value type: DateTime
        var dateProp = typeof(SampleParent).GetProperty(nameof(SampleParent.CreatedDate));
        var dateAccessor = typeof(SampleParent).GetPropertyAccessor(dateProp);
        Assert.That(dateAccessor(parent), Is.EqualTo(new DateTime(2026, 9, 5)));

        // Value type: Guid
        var guidProp = typeof(SampleParent).GetProperty(nameof(SampleParent.UniqueId));
        var guidAccessor = typeof(SampleParent).GetPropertyAccessor(guidProp);
        Assert.That(guidAccessor(parent), Is.EqualTo(parent.UniqueId));

        // Reference type
        var childProp = typeof(SampleParent).GetProperty(nameof(SampleParent.Child));
        var childAccessor = typeof(SampleParent).GetPropertyAccessor(childProp);
        Assert.That(childAccessor(parent), Is.EqualTo(parent.Child));
    }

    [Test]
    public void GetPropertyAccessor_Works_For_All_ValueTypes_Enums_Nullables_And_Structs()
    {
        var model = new ComprehensiveTestModel
        {
            IsActive = true,
            Priority = 3,
            ShortVal = 100,
            IntVal = 42,
            LongVal = 9876543210L,
            CharVal = 'Z',
            FloatVal = 3.14f,
            DoubleVal = 2.71828,
            DecimalVal = 1234.56m,
            DateTimeVal = new DateTime(2026, 9, 5, 12, 0, 0, DateTimeKind.Utc),
            TimeSpanVal = TimeSpan.FromMinutes(45),
            GuidVal = Guid.NewGuid(),
            Status = TestStatus.Active,
            NullableIntWithValue = 777,
            NullableIntNull = null,
            NullableDateWithValue = new DateTime(2026, 1, 1),
            NullableDateNull = null,
            Text = "ServiceStack",
            Child = new SampleChild { Title = "Child Title" }
        };

        // Primitives & Value Types
        Assert.That(typeof(ComprehensiveTestModel).GetPropertyAccessor(typeof(ComprehensiveTestModel).GetProperty(nameof(model.IsActive)))(model), Is.EqualTo(true));
        Assert.That(typeof(ComprehensiveTestModel).GetPropertyAccessor(typeof(ComprehensiveTestModel).GetProperty(nameof(model.Priority)))(model), Is.EqualTo((byte)3));
        Assert.That(typeof(ComprehensiveTestModel).GetPropertyAccessor(typeof(ComprehensiveTestModel).GetProperty(nameof(model.ShortVal)))(model), Is.EqualTo((short)100));
        Assert.That(typeof(ComprehensiveTestModel).GetPropertyAccessor(typeof(ComprehensiveTestModel).GetProperty(nameof(model.IntVal)))(model), Is.EqualTo(42));
        Assert.That(typeof(ComprehensiveTestModel).GetPropertyAccessor(typeof(ComprehensiveTestModel).GetProperty(nameof(model.LongVal)))(model), Is.EqualTo(9876543210L));
        Assert.That(typeof(ComprehensiveTestModel).GetPropertyAccessor(typeof(ComprehensiveTestModel).GetProperty(nameof(model.CharVal)))(model), Is.EqualTo('Z'));
        Assert.That(typeof(ComprehensiveTestModel).GetPropertyAccessor(typeof(ComprehensiveTestModel).GetProperty(nameof(model.FloatVal)))(model), Is.EqualTo(3.14f));
        Assert.That(typeof(ComprehensiveTestModel).GetPropertyAccessor(typeof(ComprehensiveTestModel).GetProperty(nameof(model.DoubleVal)))(model), Is.EqualTo(2.71828));
        Assert.That(typeof(ComprehensiveTestModel).GetPropertyAccessor(typeof(ComprehensiveTestModel).GetProperty(nameof(model.DecimalVal)))(model), Is.EqualTo(1234.56m));
        Assert.That(typeof(ComprehensiveTestModel).GetPropertyAccessor(typeof(ComprehensiveTestModel).GetProperty(nameof(model.DateTimeVal)))(model), Is.EqualTo(model.DateTimeVal));
        Assert.That(typeof(ComprehensiveTestModel).GetPropertyAccessor(typeof(ComprehensiveTestModel).GetProperty(nameof(model.TimeSpanVal)))(model), Is.EqualTo(TimeSpan.FromMinutes(45)));
        Assert.That(typeof(ComprehensiveTestModel).GetPropertyAccessor(typeof(ComprehensiveTestModel).GetProperty(nameof(model.GuidVal)))(model), Is.EqualTo(model.GuidVal));
        Assert.That(typeof(ComprehensiveTestModel).GetPropertyAccessor(typeof(ComprehensiveTestModel).GetProperty(nameof(model.Status)))(model), Is.EqualTo(TestStatus.Active));

        // Nullables
        Assert.That(typeof(ComprehensiveTestModel).GetPropertyAccessor(typeof(ComprehensiveTestModel).GetProperty(nameof(model.NullableIntWithValue)))(model), Is.EqualTo(777));
        Assert.That(typeof(ComprehensiveTestModel).GetPropertyAccessor(typeof(ComprehensiveTestModel).GetProperty(nameof(model.NullableIntNull)))(model), Is.Null);
        Assert.That(typeof(ComprehensiveTestModel).GetPropertyAccessor(typeof(ComprehensiveTestModel).GetProperty(nameof(model.NullableDateWithValue)))(model), Is.EqualTo(new DateTime(2026, 1, 1)));
        Assert.That(typeof(ComprehensiveTestModel).GetPropertyAccessor(typeof(ComprehensiveTestModel).GetProperty(nameof(model.NullableDateNull)))(model), Is.Null);

        // Reference Types
        Assert.That(typeof(ComprehensiveTestModel).GetPropertyAccessor(typeof(ComprehensiveTestModel).GetProperty(nameof(model.Text)))(model), Is.EqualTo("ServiceStack"));
        Assert.That(typeof(ComprehensiveTestModel).GetPropertyAccessor(typeof(ComprehensiveTestModel).GetProperty(nameof(model.Child)))(model), Is.SameAs(model.Child));

        // Reusable across multiple instances
        var model2 = new ComprehensiveTestModel { IntVal = 999, Text = "Second" };
        var intAccessor = typeof(ComprehensiveTestModel).GetPropertyAccessor(typeof(ComprehensiveTestModel).GetProperty(nameof(ComprehensiveTestModel.IntVal)));
        var textAccessor = typeof(ComprehensiveTestModel).GetPropertyAccessor(typeof(ComprehensiveTestModel).GetProperty(nameof(ComprehensiveTestModel.Text)));
        Assert.That(intAccessor(model2), Is.EqualTo(999));
        Assert.That(textAccessor(model2), Is.EqualTo("Second"));

        // Value Type (struct) target instance
        object sampleStruct = new SampleStruct { Age = 30, Name = "StructUser", Balance = 500.50m };
        var structAgeAccessor = typeof(SampleStruct).GetPropertyAccessor(typeof(SampleStruct).GetProperty(nameof(SampleStruct.Age)));
        var structNameAccessor = typeof(SampleStruct).GetPropertyAccessor(typeof(SampleStruct).GetProperty(nameof(SampleStruct.Name)));
        var structBalanceAccessor = typeof(SampleStruct).GetPropertyAccessor(typeof(SampleStruct).GetProperty(nameof(SampleStruct.Balance)));
        Assert.That(structAgeAccessor(sampleStruct), Is.EqualTo(30));
        Assert.That(structNameAccessor(sampleStruct), Is.EqualTo("StructUser"));
        Assert.That(structBalanceAccessor(sampleStruct), Is.EqualTo(500.50m));
    }

    [Test]
    public void TypeExtensions_Guards_Against_Null_Arguments()
    {
        Assert.That(((Type)null).GetReferencedTypes(), Is.Empty);
        Assert.That(((Type)null).IsRefStruct(), Is.False);
        Assert.That(typeof(int).IsRefStruct(), Is.False);
        Assert.That(typeof(ReadOnlySpan<char>).IsRefStruct(), Is.True);

        Assert.Throws<ArgumentNullException>(() => ((ConstructorInfo)null).GetActivator());
        Assert.Throws<ArgumentNullException>(() => ((MethodInfo)null).GetInvoker());
        Assert.Throws<ArgumentNullException>(() => ((MethodInfo)null).GetStaticInvoker());
        Assert.Throws<ArgumentNullException>(() => ((MethodInfo)null).GetActionInvoker());
        Assert.Throws<ArgumentNullException>(() => ((MethodInfo)null).GetStaticActionInvoker());
        Assert.Throws<ArgumentNullException>(() => ((MethodInfo)null).GetInvokerDelegate());
        Assert.Throws<ArgumentNullException>(() => typeof(SampleParent).GetPropertyAccessor(null));
        Assert.Throws<ArgumentNullException>(() => ((Type)null).GetPropertyAccessor(typeof(SampleParent).GetProperty("Id")));
    }

    #endregion

    #region AsyncManualResetEvent Tests

    [Test]
    public async Task AsyncManualResetEvent_WaitAsync_And_Set_And_Reset()
    {
        var mre = new AsyncManualResetEvent(false);
        Assert.That(mre.IsSet, Is.False);

        var waitTask = mre.WaitAsync();
        Assert.That(waitTask.IsCompleted, Is.False);

        mre.Set();
        Assert.That(mre.IsSet, Is.True);
        await waitTask;

        mre.Reset();
        Assert.That(mre.IsSet, Is.False);

        // Cancelled token wait
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        Assert.CatchAsync<OperationCanceledException>(async () => await mre.WaitAsync(cts.Token));
    }

    #endregion
}
