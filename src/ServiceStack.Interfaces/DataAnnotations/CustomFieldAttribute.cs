using System;

namespace ServiceStack.DataAnnotations
{
    [AttributeUsage(AttributeTargets.Property)]
    public class CustomFieldAttribute : AttributeBase
    {
        public string Sql { get; set; }

        public CustomFieldAttribute(string sql)
        {
            Sql = sql;
        }
    }

    public class PgSqlJsonAttribute : CustomFieldAttribute
    {
        public PgSqlJsonAttribute() : base("json") { }
    }

    public class PgSqlJsonBAttribute : CustomFieldAttribute
    {
        public PgSqlJsonBAttribute() : base("jsonb") { }
    }

    public class PgSqlHStoreAttribute : CustomFieldAttribute
    {
        public PgSqlHStoreAttribute() : base("hstore") { }
    }

    public class PgSqlTextArrayAttribute : CustomFieldAttribute
    {
        public PgSqlTextArrayAttribute() : base("text[]") { }
    }

    public class PgSqlIntArrayAttribute : CustomFieldAttribute
    {
        public PgSqlIntArrayAttribute() : base("integer[]") { }
    }

    public class PgSqlBigIntArrayAttribute : PgSqlLongArrayAttribute { }
    public class PgSqlLongArrayAttribute : CustomFieldAttribute
    {
        public PgSqlLongArrayAttribute() : base("bigint[]") { }
    }

    public class PgSqlFloatArrayAttribute : CustomFieldAttribute
    {
        public PgSqlFloatArrayAttribute() : base("real[]") { }
    }

    public class PgSqlDoubleArrayAttribute : CustomFieldAttribute
    {
        public PgSqlDoubleArrayAttribute() : base("double precision[]") { }
    }

    public class PgSqlDecimalArrayAttribute : CustomFieldAttribute
    {
        public PgSqlDecimalArrayAttribute() : base("numeric[]") { }
    }

    public class PgSqlTimestampAttribute : CustomFieldAttribute
    {
        public PgSqlTimestampAttribute() : base("timestamp[]") { }
    }

    public class PgSqlTimestampTzAttribute : CustomFieldAttribute
    {
        public PgSqlTimestampTzAttribute() : base("timestamp with time zone[]") { }
    }
}