using System;
using System.Collections;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Reflection;

namespace ServiceStack.OrmLite.Oracle
{
    public class OracleDataReader : DbDataReader
    {
        private readonly DbDataReader _reader;
        private readonly Type _readerType;

        public OracleDataReader(IDataReader dbDataReader)
        {
            if (dbDataReader == null)   
                throw new ArgumentNullException("dbDataReader");

            _reader = (DbDataReader) dbDataReader;
            _readerType = _reader.GetType();
        }

        public override void Close()
        {
            _reader.Close();
        }

        public override DataTable GetSchemaTable()
        {
            return _reader.GetSchemaTable();
        }

        public override bool NextResult()
        {
            return _reader.NextResult();
        }

        public override bool Read()
        {
            return _reader.Read();
        }

        public override int Depth
        {
            get { return _reader.Depth; }
        }

        public override bool IsClosed
        {
            get { return _reader.IsClosed; }
        }

        public override int RecordsAffected
        {
            get { return _reader.RecordsAffected; }
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

        public override DateTime GetDateTime(int ordinal)
        {
            if (_reader.IsDBNull(ordinal))
                return DateTime.MinValue;

            return _reader.GetDateTime(ordinal);
        }

        public object GetOracleValue(int ordinal)
        {
            var value = GetOracleValueMethod != null
                ? GetOracleValueMethod.Invoke(_reader, new object[] {ordinal})
                : null;

            var oracleValue = new OracleValue(value);

            return oracleValue.IsNull() ? _reader.GetValue(ordinal) : oracleValue;
        }

        public override string GetString(int ordinal)
        {
            return _reader.GetString(ordinal);
        }

        private readonly string[] _oracleValueDataTypeNames = {"Decimal", "Int16", "Int64", "TimeStampTZ"};
        public override object GetValue(int ordinal)
        {
            object value = null;
            var dataTypeName = GetDataTypeName(ordinal);

            if (_oracleValueDataTypeNames.Contains(dataTypeName))
            {
                value = GetOracleValue(ordinal);
            }

            return value ?? _reader.GetValue(ordinal);
        }

        public override int GetValues(object[] values)
        {
            return _reader.GetValues(values);
        }

        public override bool IsDBNull(int ordinal)
        {
            return _reader.IsDBNull(ordinal);
        }

        public override int FieldCount
        {
            get { return _reader.FieldCount; }
        }

        public override object this[int ordinal]
        {
            get { return _reader[ordinal]; }
        }

        public override object this[string name]
        {
            get { return _reader[name]; }
        }

        public override bool HasRows
        {
            get { return _reader.HasRows; }
        }

        public override decimal GetDecimal(int ordinal)
        {
            return _reader.GetDecimal(ordinal);
        }

        public override double GetDouble(int ordinal)
        {
            var value = GetOracleValue(ordinal);
            return Convert.ToDouble(value);
        }

        private MethodInfo _getOracleValueMethod;
        private MethodInfo GetOracleValueMethod
        {
            get 
            {
                return _getOracleValueMethod ??
                       (_getOracleValueMethod = _readerType.GetMethod("GetOracleValue", BindingFlags.Public | BindingFlags.Instance));
            }
        }

        public override float GetFloat(int ordinal)
        {
            return _reader.GetFloat(ordinal);
        }

        public override string GetName(int ordinal)
        {
            return _reader.GetName(ordinal);
        }

        public override int GetOrdinal(string name)
        {
            return _reader.GetOrdinal(name);
        }

        public override string GetDataTypeName(int ordinal)
        {
            return _reader.GetDataTypeName(ordinal);
        }

        public override Type GetFieldType(int ordinal)
        {
            return _reader.GetFieldType(ordinal);
        }

        public override IEnumerator GetEnumerator()
        {
            return _reader.GetEnumerator();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _reader.Dispose();
            }

            base.Dispose(disposing);
        }
    }
}
