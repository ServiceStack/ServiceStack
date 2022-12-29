using System;
using System.Data;
using ServiceStack.OrmLite.Converters;

namespace ServiceStack.OrmLite.PostgreSQL.Converters
{
    public class PostrgreSqlFloatConverter : FloatConverter
    {
        public override string ColumnDefinition => "DOUBLE PRECISION";
    }

    public class PostrgreSqlDoubleConverter : DoubleConverter
    {
        public override string ColumnDefinition => "DOUBLE PRECISION";
    }

    public class PostrgreSqlDecimalConverter : DecimalConverter
    {
        public PostrgreSqlDecimalConverter() 
            : base(38, 6) {}
        
        public override object GetValue(IDataReader reader, int columnIndex, object[] values)
        {
            try
            {
                return base.GetValue(reader, columnIndex, values);
            }
            catch (OverflowException)
            {
                return decimal.MaxValue;
            }
        }
    }
}