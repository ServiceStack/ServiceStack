using System;
using System.Data.Common;
using System.Data;

#pragma warning disable 1591 // xml doc comments warnings

namespace MvcMiniProfiler.Data
{

    public class ProfiledDbDataReader : DbDataReader
    {

        private DbConnection _conn;
        private DbDataReader _reader;
        private IDbProfiler _profiler;


        public ProfiledDbDataReader(DbDataReader reader, DbConnection connection, IDbProfiler profiler)
        {
            _reader = reader;
            _conn = connection;

            if (profiler != null)
            {
                _profiler = profiler;
            }
        }


        public override int Depth
        {
            get { return _reader.Depth; }
        }

        public override int FieldCount
        {
            get { return _reader.FieldCount; }
        }

        public override bool HasRows
        {
            get { return _reader.HasRows; }
        }

        public override bool IsClosed
        {
            get { return _reader.IsClosed; }
        }

        public override int RecordsAffected
        {
            get { return _reader.RecordsAffected; }
        }

        public override object this[string name]
        {
            get { return _reader[name]; }
        }

        public override object this[int ordinal]
        {
            get { return _reader[ordinal]; }
        }

        public override void Close()
        {
            // this can occur when we're not profiling, but we've inherited from ProfiledDbCommand and are returning a
            // an unwrapped reader from the base command
            if (_reader != null)
            {
                _reader.Close();
            }

            if (_profiler != null)
            {
                _profiler.ReaderFinish(this);
            }
        }

        public override bool GetBoolean(int ordinal)
        {
            return _reader.GetBoolean(ordinal);
        }

        public override byte GetByte(int ordinal)
        {
            return _reader.GetByte(ordinal);
        }

        public override long GetBytes(int ordinal, long dataOffset, byte[] buffer, int bufferOffset, int length)
        {
            return _reader.GetBytes(ordinal, dataOffset, buffer, bufferOffset, length);
        }

        public override char GetChar(int ordinal)
        {
            return _reader.GetChar(ordinal);
        }

        public override long GetChars(int ordinal, long dataOffset, char[] buffer, int bufferOffset, int length)
        {
            return _reader.GetChars(ordinal, dataOffset, buffer, bufferOffset, length);
        }

        public override string GetDataTypeName(int ordinal)
        {
            return _reader.GetDataTypeName(ordinal);
        }

        public override DateTime GetDateTime(int ordinal)
        {
            return _reader.GetDateTime(ordinal);
        }

        public override decimal GetDecimal(int ordinal)
        {
            return _reader.GetDecimal(ordinal);
        }

        public override double GetDouble(int ordinal)
        {
            return _reader.GetDouble(ordinal);
        }

        public override System.Collections.IEnumerator GetEnumerator()
        {
            return ((System.Collections.IEnumerable)_reader).GetEnumerator();
        }

        public override Type GetFieldType(int ordinal)
        {
            return _reader.GetFieldType(ordinal);
        }

        public override float GetFloat(int ordinal)
        {
            return _reader.GetFloat(ordinal);
        }

        public override Guid GetGuid(int ordinal)
        {
            return _reader.GetGuid(ordinal);
        }

        public override short GetInt16(int ordinal)
        {
            return _reader.GetInt16(ordinal);
        }

        public override int GetInt32(int ordinal)
        {
            return _reader.GetInt32(ordinal);
        }

        public override long GetInt64(int ordinal)
        {
            return _reader.GetInt64(ordinal);
        }

        public override string GetName(int ordinal)
        {
            return _reader.GetName(ordinal);
        }

        public override int GetOrdinal(string name)
        {
            return _reader.GetOrdinal(name);
        }

        public override DataTable GetSchemaTable()
        {
            return _reader.GetSchemaTable();
        }

        public override string GetString(int ordinal)
        {
            return _reader.GetString(ordinal);
        }

        public override object GetValue(int ordinal)
        {
            return _reader.GetValue(ordinal);
        }

        public override int GetValues(object[] values)
        {
            return _reader.GetValues(values);
        }

        public override bool IsDBNull(int ordinal)
        {
            return _reader.IsDBNull(ordinal);
        }

        public override bool NextResult()
        {
            return _reader.NextResult();
        }

        public override bool Read()
        {
            return _reader.Read();
        }
    }
}

#pragma warning restore 1591 // xml doc comments warnings