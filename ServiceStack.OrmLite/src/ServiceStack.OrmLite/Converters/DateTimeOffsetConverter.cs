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
            if (value is string strValue)
            {
                var moment = DateTimeOffset.Parse(strValue, null, DateTimeStyles.RoundtripKind);
                return moment;
            }
            if (value.GetType() == fieldType)
                return value;
            if (value is DateTime dateTime)
                return new DateTimeOffset(dateTime);

            var convertedValue = DialectProvider.StringSerializer.DeserializeFromString(value.ToString(), fieldType);
            return convertedValue;
        }
    }
}