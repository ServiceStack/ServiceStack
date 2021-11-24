using System;

namespace ServiceStack.DataAnnotations
{
    [AttributeUsage(AttributeTargets.Property)]
    public class CustomFieldAttribute : AttributeBase
    {
        public string Sql { get; set; }
        public int Order { get; set; }

        public CustomFieldAttribute() { }
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

    public class PgSqlShortArrayAttribute : CustomFieldAttribute
    {
        public PgSqlShortArrayAttribute() : base("short[]") { }
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

    public class PgSqlTimestampArrayAttribute : CustomFieldAttribute
    {
        public PgSqlTimestampArrayAttribute() : base("timestamp[]") { }
    }

    public class PgSqlTimestampTzArrayAttribute : CustomFieldAttribute
    {
        public PgSqlTimestampTzArrayAttribute() : base("timestamp with time zone[]") { }
    }

    [Obsolete("Use [PgSqlTimestampArray]")]
    public class PgSqlTimestampAttribute : CustomFieldAttribute
    {
        public PgSqlTimestampAttribute() : base("timestamp[]") { }
    }
    
    [Obsolete("Use [PgSqlTimestampTzArray]")]
    public class PgSqlTimestampTzAttribute : CustomFieldAttribute
    {
        public PgSqlTimestampTzAttribute() : base("timestamp with time zone[]") { }
    }
    
}