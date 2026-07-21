using System;
using ServiceStack.OrmLite.Converters;

namespace ServiceStack.OrmLite.Firebird.Converters
{
    public class FirebirdGuidConverter : GuidConverter
    {
        public override string ColumnDefinition
        {
            get { return "VARCHAR(37)"; }
        }

        public override string ToQuotedString(Type fieldType, object value)
        {
            return string.Format("CAST('{0}' AS {1})", (Guid)value, ColumnDefinition);  // TODO : check this !!!
        }

        public override object FromDbValue(Type fieldType, object value)
        {
            return new Guid(value.ToString());
        }

        public override object ToDbValue(Type fieldType, object value)
        {
            return base.ToDbValue(fieldType, value);
        }
    }

    public class FirebirdCompactGuidConverter : GuidConverter
    {
        public override string ColumnDefinition
        {
            get { return "CHAR(16) CHARACTER SET OCTETS"; }
        }

        // Literal must match the parameterized octet order (Guid.ToByteArray()), else it disagrees with ToDbValue.
        public override string ToQuotedString(Type fieldType, object value)
        {
            return "X'" + BitConverter.ToString(((Guid)value).ToByteArray()).Replace("-", "") + "'";
        }

        // Previous impl applied a NetworkToHostOrder swap on READ that the WRITE side never applied -> the returned
        // Guid didn't match the stored one (round-trip mismatch). The client already round-trips CHAR(16) OCTETS
        // symmetrically via Guid.ToByteArray()/new Guid(bytes), so just pass the value through.
        public override object FromDbValue(Type fieldType, object value) => value switch
        {
            Guid g => g,
            byte[] b when b.Length == 16 => new Guid(b),
            string s => new Guid(s),
            _ => base.FromDbValue(fieldType, value),
        };
    }
}