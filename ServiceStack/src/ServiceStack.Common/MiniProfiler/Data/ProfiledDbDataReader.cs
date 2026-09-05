using System;
using System.Data.Common;
using System.Data;

#pragma warning disable 1591 // xml doc comments warnings

namespace ServiceStack.MiniProfiler.Data
{

    public class ProfiledDbDataReader : DbDataReader
    {
        private DbConnection db;
        private readonly DbDataReader reader;
        private readonly IDbProfiler profiler;

        public ProfiledDbDataReader(DbDataReader reader, DbConnection connection, IDbProfiler profiler)
        {
            this.reader = reader;
            db = connection;

            if (profiler != null)
            {
                this.profiler = profiler;
            }
        }


        public override int Depth => reader.Depth;

        public override int FieldCount => reader.FieldCount;

        public override bool HasRows => reader.HasRows;

        public override bool IsClosed => reader.IsClosed;

        public override int RecordsAffected => reader.RecordsAffected;

        public override object this[string name] => reader[name];

        public override object this[int ordinal] => reader[ordinal];

        private int isClosed;

        public override void Close()
        {
            // this can occur when we're not profiling, but we've inherited from ProfiledDbCommand and are returning a
            // an unwrapped reader from the base command
            if (System.Threading.Interlocked.Exchange(ref isClosed, 1) == 0)
            {
                reader?.Close();
                profiler?.ReaderFinish(this);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                Close();
            }
            base.Dispose(disposing);
        }

#if NETSTANDARD2_1_OR_GREATER || NET6_0_OR_GREATER
        public override System.Threading.Tasks.Task CloseAsync()
        {
            if (System.Threading.Interlocked.Exchange(ref isClosed, 1) == 0)
            {
                profiler?.ReaderFinish(this);
                return reader != null ? reader.CloseAsync() : System.Threading.Tasks.Task.CompletedTask;
            }
            return System.Threading.Tasks.Task.CompletedTask;
        }

        public override async System.Threading.Tasks.ValueTask DisposeAsync()
        {
            if (System.Threading.Interlocked.Exchange(ref isClosed, 1) == 0)
            {
                profiler?.ReaderFinish(this);
                if (reader != null)
                {
                    await reader.DisposeAsync().ConfigureAwait(false);
                }
            }
            await base.DisposeAsync().ConfigureAwait(false);
        }
#endif

        public override bool GetBoolean(int ordinal)
        {
            return reader.GetBoolean(ordinal);
        }

        public override byte GetByte(int ordinal)
        {
            return reader.GetByte(ordinal);
        }

        public override long GetBytes(int ordinal, long dataOffset, byte[] buffer, int bufferOffset, int length)
        {
            return reader.GetBytes(ordinal, dataOffset, buffer, bufferOffset, length);
        }

        public override char GetChar(int ordinal)
        {
            return reader.GetChar(ordinal);
        }

        public override long GetChars(int ordinal, long dataOffset, char[] buffer, int bufferOffset, int length)
        {
            return reader.GetChars(ordinal, dataOffset, buffer, bufferOffset, length);
        }

        public override string GetDataTypeName(int ordinal)
        {
            return reader.GetDataTypeName(ordinal);
        }

        public override DateTime GetDateTime(int ordinal)
        {
            return reader.GetDateTime(ordinal);
        }

        public override decimal GetDecimal(int ordinal)
        {
            return reader.GetDecimal(ordinal);
        }

        public override double GetDouble(int ordinal)
        {
            return reader.GetDouble(ordinal);
        }

        public override System.Collections.IEnumerator GetEnumerator()
        {
            return ((System.Collections.IEnumerable)reader).GetEnumerator();
        }

        public override Type GetFieldType(int ordinal)
        {
            return reader.GetFieldType(ordinal);
        }

        public override float GetFloat(int ordinal)
        {
            return reader.GetFloat(ordinal);
        }

        public override Guid GetGuid(int ordinal)
        {
            return reader.GetGuid(ordinal);
        }

        public override short GetInt16(int ordinal)
        {
            return reader.GetInt16(ordinal);
        }

        public override int GetInt32(int ordinal)
        {
            return reader.GetInt32(ordinal);
        }

        public override long GetInt64(int ordinal)
        {
            return reader.GetInt64(ordinal);
        }

        public override string GetName(int ordinal)
        {
            return reader.GetName(ordinal);
        }

        public override int GetOrdinal(string name)
        {
            return reader.GetOrdinal(name);
        }

        public override DataTable GetSchemaTable()
        {
            return reader.GetSchemaTable();
        }

        public override string GetString(int ordinal)
        {
            return reader.GetString(ordinal);
        }

        public override object GetValue(int ordinal)
        {
            return reader.GetValue(ordinal);
        }

        public override int GetValues(object[] values)
        {
            return reader.GetValues(values);
        }

        public override bool IsDBNull(int ordinal)
        {
            return reader.IsDBNull(ordinal);
        }

        public override System.Threading.Tasks.Task<bool> IsDBNullAsync(int ordinal, System.Threading.CancellationToken cancellationToken)
        {
            return reader.IsDBNullAsync(ordinal, cancellationToken);
        }

        public override bool NextResult()
        {
            return reader.NextResult();
        }

        public override System.Threading.Tasks.Task<bool> NextResultAsync(System.Threading.CancellationToken cancellationToken)
        {
            return reader.NextResultAsync(cancellationToken);
        }

        public override bool Read()
        {
            return reader.Read();
        }

        public override System.Threading.Tasks.Task<bool> ReadAsync(System.Threading.CancellationToken cancellationToken)
        {
            return reader.ReadAsync(cancellationToken);
        }

        public override T GetFieldValue<T>(int ordinal)
        {
            return reader.GetFieldValue<T>(ordinal);
        }

        public override System.Threading.Tasks.Task<T> GetFieldValueAsync<T>(int ordinal, System.Threading.CancellationToken cancellationToken)
        {
            return reader.GetFieldValueAsync<T>(ordinal, cancellationToken);
        }

        public override System.IO.Stream GetStream(int ordinal)
        {
            return reader.GetStream(ordinal);
        }

        public override System.IO.TextReader GetTextReader(int ordinal)
        {
            return reader.GetTextReader(ordinal);
        }
    }
}

#pragma warning restore 1591 // xml doc comments warnings