using System;

namespace ServiceStack.OrmLite.Oracle.Converters
{
    public class OracleCharArrayConverter : OracleStringConverter
    {
        public OracleCharArrayConverter() : base(4000) {}

        public override object ToDbValue(Type fieldType, object value)
        {
            var chars = (char[])value;
            return new string(chars);
        }

        public override object FromDbValue(Type fieldType, object value)
        {
            if (value is char[])
                return value;

            var strValue = value as string;
            if (strValue != null)
                return strValue.ToCharArray();

            return (char[])value;
        }
    }
}