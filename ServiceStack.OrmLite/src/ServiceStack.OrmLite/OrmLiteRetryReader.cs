#nullable enable

using System;
using System.Collections;
using System.Data;
using System.Data.Common;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using ServiceStack.Text;

namespace ServiceStack.OrmLite;

/// <summary>
/// The rows of a query that's run again when reading its first row fails with a temporary error, which some
/// databases report after the query has started, e.g. a deadlock or a lost connection. Once a row has been read,
/// the query isn't run again as its rows have been returned.
/// </summary>
internal sealed class OrmLiteRetryReader : DbDataReader
{
    private DbDataReader reader;
    private readonly IDbCommand dbCmd;
    private readonly IDbCommand runCmd;
    private readonly IOrmLiteDialectProvider dialect;
    private readonly OrmLiteRetryPolicy policy;
    private readonly CommandBehavior behavior;
    private bool started;

    public OrmLiteRetryReader(DbDataReader reader, IDbCommand dbCmd, IOrmLiteDialectProvider dialect,
        OrmLiteRetryPolicy policy, CommandBehavior behavior)
    {
        this.reader = reader;
        this.dbCmd = dbCmd;
        this.runCmd = dbCmd.ToDbCommand(); // runs the query without retrying it again
        this.dialect = dialect;
        this.policy = policy;
        this.behavior = behavior;
    }

    public override bool Read()
    {
        if (started)
            return reader.Read();

        for (var retry = 1; ; retry++)
        {
            try
            {
                var ret = reader.Read();
                started = true;
                return ret;
            }
            catch (Exception ex) when (OrmLiteRetryExec.ShouldRetryStatement(policy, dialect, dbCmd, ex, retry, out var delay))
            {
                DisposeQuietly(reader);
                Thread.Sleep(delay);
            }
            OrmLiteRetryExec.Reopen(dbCmd);
            reader = (DbDataReader)runCmd.ExecuteReader(behavior);
        }
    }

    public override async Task<bool> ReadAsync(CancellationToken token)
    {
        if (started)
            return await dialect.ReadAsync(reader, token).ConfigAwait();

        for (var retry = 1; ; retry++)
        {
            TimeSpan delay;
            try
            {
                var ret = await dialect.ReadAsync(reader, token).ConfigAwait();
                started = true;
                return ret;
            }
            catch (Exception ex) when (OrmLiteRetryExec.ShouldRetryStatement(policy, dialect, dbCmd, ex, retry, out delay))
            {
                DisposeQuietly(reader);
            }
            await Task.Delay(delay, token).ConfigAwait();
            await OrmLiteRetryExec.ReopenAsync(dbCmd, token).ConfigAwait();
            reader = (DbDataReader)await dialect.ExecuteReaderAsync(runCmd, token).ConfigAwait();
        }
    }

    // The reader of a query that failed, whose connection may have been lost
    private static void DisposeQuietly(DbDataReader reader)
    {
        try
        {
            reader.Dispose();
        }
        catch (Exception e)
        {
            OrmLiteLog.Log.Warn("Failed to close the reader of a query before retrying it", e);
        }
    }

    public override bool NextResult()
    {
        started = true;
        return reader.NextResult();
    }

    public override Task<bool> NextResultAsync(CancellationToken token)
    {
        started = true;
        return reader.NextResultAsync(token);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
            reader.Dispose();
        base.Dispose(disposing);
    }

    public override void Close() => reader.Close();
    public override bool HasRows => reader.HasRows;
    public override DataTable? GetSchemaTable() => reader.GetSchemaTable();
    public override int Depth => reader.Depth;
    public override bool IsClosed => reader.IsClosed;
    public override int RecordsAffected => reader.RecordsAffected;
    public override int FieldCount => reader.FieldCount;
    public override int VisibleFieldCount => reader.VisibleFieldCount;
    public override bool GetBoolean(int i) => reader.GetBoolean(i);
    public override byte GetByte(int i) => reader.GetByte(i);
    public override long GetBytes(int i, long fieldOffset, byte[]? buffer, int bufferOffset, int length) =>
        reader.GetBytes(i, fieldOffset, buffer, bufferOffset, length);
    public override char GetChar(int i) => reader.GetChar(i);
    public override long GetChars(int i, long fieldOffset, char[]? buffer, int bufferOffset, int length) =>
        reader.GetChars(i, fieldOffset, buffer, bufferOffset, length);
    public override string GetDataTypeName(int i) => reader.GetDataTypeName(i);
    public override DateTime GetDateTime(int i) => reader.GetDateTime(i);
    public override decimal GetDecimal(int i) => reader.GetDecimal(i);
    public override double GetDouble(int i) => reader.GetDouble(i);
    public override Type GetFieldType(int i) => reader.GetFieldType(i);
    public override float GetFloat(int i) => reader.GetFloat(i);
    public override Guid GetGuid(int i) => reader.GetGuid(i);
    public override short GetInt16(int i) => reader.GetInt16(i);
    public override int GetInt32(int i) => reader.GetInt32(i);
    public override long GetInt64(int i) => reader.GetInt64(i);
    public override string GetName(int i) => reader.GetName(i);
    public override int GetOrdinal(string name) => reader.GetOrdinal(name);
    public override string GetString(int i) => reader.GetString(i);
    public override object GetValue(int i) => reader.GetValue(i);
    public override int GetValues(object[] values) => reader.GetValues(values);
    public override bool IsDBNull(int i) => reader.IsDBNull(i);
    public override object this[string name] => reader[name];
    public override object this[int i] => reader[i];
    public override T GetFieldValue<T>(int i) => reader.GetFieldValue<T>(i);
    public override Task<T> GetFieldValueAsync<T>(int i, CancellationToken token) => reader.GetFieldValueAsync<T>(i, token);
    public override Task<bool> IsDBNullAsync(int i, CancellationToken token) => reader.IsDBNullAsync(i, token);
    public override IEnumerator GetEnumerator() => reader.GetEnumerator();
    public override Type GetProviderSpecificFieldType(int i) => reader.GetProviderSpecificFieldType(i);
    public override object GetProviderSpecificValue(int i) => reader.GetProviderSpecificValue(i);
    public override int GetProviderSpecificValues(object[] values) => reader.GetProviderSpecificValues(values);
    public override Stream GetStream(int i) => reader.GetStream(i);
    public override TextReader GetTextReader(int i) => reader.GetTextReader(i);
    protected override DbDataReader GetDbDataReader(int i) =>
        ((IDataReader)reader).GetData(i) as DbDataReader ?? throw new NotSupportedException();
}
