using System;

namespace ServiceStack.DataAnnotations
{
    [AttributeUsage(AttributeTargets.Property)]
    public class CustomSelectAttribute : AttributeBase
    {
        public string Sql { get; set; }
        public CustomSelectAttribute(string sql) => Sql = sql;
    }

    [AttributeUsage(AttributeTargets.Property)]
    public class CustomInsertAttribute : AttributeBase
    {
        public string Sql { get; set; }
        public CustomInsertAttribute(string sql) => Sql = sql;
    }

    [AttributeUsage(AttributeTargets.Property)]
    public class CustomUpdateAttribute : AttributeBase
    {
        public string Sql { get; set; }
        public CustomUpdateAttribute(string sql) => Sql = sql;
    }
}