using System;
using System.Data;
using System.Globalization;

namespace ServiceStack.OrmLite.Converters
{
    public class DateTimeOffsetConverter : OrmLiteConverter
    {
        public override string ColumnDefinition => "DATETIMEOFFSET";
        public override DbType DbType => DbType.DateTimeOffset;

        //From OrmLiteDialectProviderBase:
        public override object FromDbValue(Type fieldType, object value)
        {
            var strValue = value as string;
            if (strValue != null)
            {
                var moment = DateTimeOffset.Parse(strValue, null, DateTimeStyles.RoundtripKind);
                return moment;
            }
            if (value.GetType() == fieldType)
            {
                return value;
            }
            if (value is DateTime)
            {
                return new DateTimeOffset((DateTime)value);
            }
            var convertedValue = DialectProvider.StringSerializer.DeserializeFromString(value.ToString(), fieldType);
            return convertedValue;
        }
    }
}