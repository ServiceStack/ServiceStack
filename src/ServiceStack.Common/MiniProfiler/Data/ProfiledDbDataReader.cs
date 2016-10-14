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

        public
#if !NETSTANDARD1_3
    	override
#endif 
    	void Close()
        {
            // this can occur when we're not profiling, but we've inherited from ProfiledDbCommand and are returning a
            // an unwrapped reader from the base command
            reader?.Close();
            profiler?.ReaderFinish(this);
        }

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

#if !NETSTANDARD1_3
        public override DataTable GetSchemaTable()
        {
            return reader.GetSchemaTable();
        }
#endif

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

        public override bool NextResult()
        {
            return reader.NextResult();
        }

        public override bool Read()
        {
            return reader.Read();
        }
    }
}

#pragma warning restore 1591 // xml doc comments warnings