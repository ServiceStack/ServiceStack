using System;
using System.Data;
using ServiceStack.Logging;
using ServiceStack.OrmLite.Converters;

namespace ServiceStack.OrmLite.Kingbase.Converters
{
    public class KingbaseSqlDecimalConverter : DecimalConverter
    {
        public KingbaseSqlDecimalConverter() : base(38, 6) { }

        public override string GetColumnDefinition(int? precision, int? scale)
        {
            return $"NUMERIC({precision.GetValueOrDefault(Precision)},{scale.GetValueOrDefault(Scale)})";
        }

        public override object GetValue(IDataReader reader, int columnIndex, object[] values)
        {
            try
            {
                return base.GetValue(reader, columnIndex, values);
            }
            catch (Exception e)
            {
                LogManager.GetLogger(GetType()).Error(e.Message, e);
                throw;
            }
        }
    }
}