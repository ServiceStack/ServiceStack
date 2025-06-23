using System;
using ServiceStack.OrmLite.Converters;

namespace ServiceStack.OrmLite.Kingbase.Converters
{
    public class KingbaseSqlRowVersionConverter : RowVersionConverter
    {
        public override object ToDbValue(Type fieldType, object value)
        {
            var ret = base.ToDbValue(fieldType, value);
            if (ret is ulong u)
                return (long) u;
            return ret;
        }

        public override object FromDbValue(Type fieldType, object value)
        {
            var ret = base.FromDbValue(fieldType, value);
            return ret;
        }
    }
}