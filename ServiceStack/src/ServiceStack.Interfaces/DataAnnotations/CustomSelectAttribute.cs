using System;

namespace ServiceStack.DataAnnotations
{
    /// <summary>
    /// Populate property with Custom SELECT expression, e.g. [CustomSelect("Width * Height")]
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class CustomSelectAttribute : AttributeBase
    {
        public string Sql { get; set; }
        public CustomSelectAttribute(string sql) => Sql = sql;
    }

    /// <summary>
    /// Populate INSERT parameter with Custom SQL expression, e.g. [CustomInsert("crypt({0}, gen_salt('bf'))")]
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class CustomInsertAttribute : AttributeBase
    {
        public string Sql { get; set; }
        public CustomInsertAttribute(string sql) => Sql = sql;
    }

    /// <summary>
    /// Populate UPDATE parameter with Custom SQL expression, e.g. [CustomUpdate("crypt({0}, gen_salt('bf'))")]
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class CustomUpdateAttribute : AttributeBase
    {
        public string Sql { get; set; }
        public CustomUpdateAttribute(string sql) => Sql = sql;
    }
}