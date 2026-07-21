using System;
using System.Data;
using ServiceStack.OrmLite.Converters;

namespace ServiceStack.OrmLite.Firebird.Converters
{
    // Firebird < 3 has no BOOLEAN type -> store bool as INTEGER (base/legacy dialect).
    public class FirebirdBoolConverter : BoolAsIntConverter
    {
        public override string ColumnDefinition
        {
            get { return "INTEGER"; }
        }
    }

    // Firebird 3+ has a native BOOLEAN type. The INTEGER converter binds a raw .NET bool into an INTEGER param,
    // which the FB client rejects (InvalidCast). Map BOOLEAN so the bool binds natively (used by Firebird4+ dialect).
    public class FirebirdBooleanConverter : BoolConverter
    {
        public override string ColumnDefinition => "BOOLEAN";
        public override DbType DbType => DbType.Boolean;

        public override object ToDbValue(Type fieldType, object value) => (bool)value;

        public override object FromDbValue(Type fieldType, object value)
            => value is bool b ? b : Convert.ToBoolean(value);

        public override string ToQuotedString(Type fieldType, object value)
            => (bool)value ? "TRUE" : "FALSE";
    }
}