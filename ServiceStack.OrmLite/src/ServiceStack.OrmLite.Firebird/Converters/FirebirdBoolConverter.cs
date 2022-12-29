using System;
using ServiceStack.OrmLite.Converters;

namespace ServiceStack.OrmLite.Firebird.Converters
{
    public class FirebirdBoolConverter : BoolAsIntConverter
    {
        public override string ColumnDefinition
        {
            get { return "INTEGER"; }
        }
    }
}