﻿using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace ServiceStack
{
    //Interfaces and DTO's used in AutoQuery
    public interface IQuery : IMeta
    {
        /// <summary>
        /// How many results to skip
        /// </summary>
        int? Skip { get; set; }

        /// <summary>
        /// How many results to return
        /// </summary>
        int? Take { get; set; }

        /// <summary>
        /// List of fields to sort by, can order by multiple fields and inverse order, e.g: Id,-Amount
        /// </summary>
        string OrderBy { get; set; }

        /// <summary>
        /// List of fields to sort by descending, can order by multiple fields and inverse order, e.g: -Id,Amount
        /// </summary>
        string OrderByDesc { get; set; }

        /// <summary>
        /// Include aggregate data like Total, COUNT(*), COUNT(DISTINCT Field), Sum(Amount), etc
        /// </summary>
        string Include { get; set; }

        /// <summary>
        /// The fields to return
        /// </summary>
        string Fields { get; set; }
    }

    public interface IQueryDb : IQuery { }
    public interface IQueryData : IQuery { }

    public interface IQueryDb<From> : IQueryDb { }
    public interface IQueryDb<From, Into> : IQueryDb { }

    public interface IQueryData<From> : IQueryData { }
    public interface IQueryData<From, Into> : IQueryData { }
    
    public interface ICrud {}
    public interface ICreateDb<Table> : ICrud {}
    public interface IUpdateDb<Table> : ICrud {}
    public interface IPatchDb<Table> : ICrud {}
    public interface IDeleteDb<Table> : ICrud {}
    public interface ISaveDb<Table> : ICrud {}
    
    public interface IJoin { }
    public interface IJoin<Source, Join1> : IJoin { }
    public interface IJoin<Source, Join1, Join2> : IJoin { }
    public interface IJoin<Source, Join1, Join2, Join3> : IJoin { }
    public interface IJoin<Source, Join1, Join2, Join3, Join4> : IJoin { }

    public interface ILeftJoin<Source, Join1> : IJoin { }
    public interface ILeftJoin<Source, Join1, Join2> : IJoin { }
    public interface ILeftJoin<Source, Join1, Join2, Join3> : IJoin { }
    public interface ILeftJoin<Source, Join1, Join2, Join3, Join4> : IJoin { }

    /// <summary>
    /// How the filter should be applied to the query
    /// </summary>
    public enum QueryTerm
    {
        /// <summary>
        /// Defaults to 'And'
        /// </summary>
        Default = 0,
        
        /// <summary>
        /// Apply filter to query using 'AND' to further filter resultset
        /// </summary>
        And = 1,
 
        /// <summary>
        /// Apply inclusive filter to query using 'OR' to further expand resultset
        /// </summary>
        Or = 2,
        
        /// <summary>
        /// Ensure filter is always applied even if other 'OR' filters are included (uses OrmLite's Ensure API)
        /// </summary>
        Ensure = 3,
    }
    
    /// <summary>
    /// Type of Value used in the SQL Template
    /// </summary>
    public enum ValueStyle
    {
        /// <summary>
        /// Standard SQL Condition, e.g: '{Field} = {Value}'
        /// </summary>
        Single = 0,
        
        /// <summary>
        /// SQL Template uses {ValueN} e.g. '{Field} BETWEEN {Value1} AND {Value2}'
        /// </summary>
        Multiple = 1,
        
        /// <summary>
        /// SQL Template uses collection parameter, e.g: '{Field} IN ({Values})'
        /// </summary>
        List = 2,
    }

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    public class QueryDbAttribute : AttributeBase
    {
        public QueryDbAttribute() { }

        public QueryDbAttribute(QueryTerm defaultTerm)
        {
            DefaultTerm = defaultTerm;
        }

        public QueryTerm DefaultTerm { get; set; }
    }

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    public class QueryDataAttribute : AttributeBase
    {
        public QueryDataAttribute() { }

        public QueryDataAttribute(QueryTerm defaultTerm)
        {
            DefaultTerm = defaultTerm;
        }

        public QueryTerm DefaultTerm { get; set; }
    }

    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public class QueryDbFieldAttribute : AttributeBase
    {
        /// <summary>
        /// Should this filter be applied with 'AND' or 'OR' or always filtered with 'Ensure' 
        /// </summary>
        public QueryTerm Term { get; set; }
        
        /// <summary>
        /// For Simple Filters to change Operand used in default Template, e.g. For Greater Than: Operand=">"
        /// </summary>
        public string Operand { get; set; }
        
        /// <summary>
        /// Use a Custom SQL Filter, Use <see cref="SqlTemplate"/> for common templates, e.g: Template=SqlTemplate.IsNotNull
        /// </summary>
        public string Template { get; set; }
        
        /// <summary>
        /// The name of the DB Field to query
        /// </summary>
        public string Field { get; set; }
        
        /// <summary>
        /// Value modifier, e.g. implement StartsWith with 'Name LIKE {Value}', ValueFormat="{0}%"
        /// </summary>
        public string ValueFormat { get; set; }
        
        /// <summary>
        /// Type of Value used in the SQL Template
        /// </summary>
        public ValueStyle ValueStyle { get; set; }
        
        public int ValueArity { get; set; }
    }
    
    /// <summary>
    /// Apply additional Filter to AutoQuery Requests
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = true)]
    public class AutoFilterAttribute : ScriptValueAttribute
    {
        /// <summary>
        /// Should this filter be applied with 'AND' or 'OR' or always filtered with 'Ensure' 
        /// </summary>
        public QueryTerm Term { get; set; }
        
        /// <summary>
        /// The name of the DB Field to query
        /// </summary>
        public string Field { get; set; }
        
        /// <summary>
        /// For Simple Filters to change Operand used in default Template, e.g. For Greater Than: Operand=">"
        /// </summary>
        public string Operand { get; set; }
        
        /// <summary>
        /// Use a Custom SQL Filter, Use <see cref="SqlTemplate"/> for common templates, e.g: Template=SqlTemplate.IsNotNull
        /// </summary>
        public string Template { get; set; }
        
        /// <summary>
        /// Value modifier, e.g. implement StartsWith with 'Name LIKE {Value}', ValueFormat="{0}%"
        /// </summary>
        public string ValueFormat { get; set; }

        public AutoFilterAttribute(string field) => Field = field ?? throw new ArgumentNullException(nameof(field));
        public AutoFilterAttribute(string field, string template)
        {
            Field = field;
            Template = template;
        }
        public AutoFilterAttribute(QueryTerm term, string field)
        {
            Term = term;
            Field = field;
        }
        public AutoFilterAttribute(QueryTerm term, string field, string template)
        {
            Term = term;
            Field = field;
            Template = template;
        }
    }

    /// <summary>
    /// Common AutoQuery SQL Filter Templates
    /// </summary>
    public static class SqlTemplate
    {
        public const string IsNull =             "{Field} IS NULL";
        public const string IsNotNull =          "{Field} IS NOT NULL";
        public const string GreaterThanOrEqual = "{Field} >= {Value}";
        public const string GreaterThan =        "{Field} > {Value}";
        public const string LessThan =           "{Field} < {Value}";
        public const string LessThanOrEqual =    "{Field} <= {Value}";
        public const string NotEqual =           "{Field} <> {Value}";
        public const string CaseSensitiveLike =  "{Field} LIKE {Value}";
        public const string CaseInsensitiveLike = "UPPER({Field}) LIKE UPPER({Value})";
    }

    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public class QueryDataFieldAttribute : AttributeBase
    {
        public QueryTerm Term { get; set; }
        public string Condition { get; set; }
        public string Field { get; set; }
    }

    [DataContract]
    public abstract class QueryBase : IQuery
    {
        [DataMember(Order = 1)]
        public virtual int? Skip { get; set; }

        [DataMember(Order = 2)]
        public virtual int? Take { get; set; }

        [DataMember(Order = 3)]
        public virtual string OrderBy { get; set; }

        [DataMember(Order = 4)]
        public virtual string OrderByDesc { get; set; }

        [DataMember(Order = 5)]
        public virtual string Include { get; set; }

        [DataMember(Order = 6)]
        public virtual string Fields { get; set; }

        [DataMember(Order = 7)]
        public virtual Dictionary<string, string> Meta { get; set; }

        // note: the number of fields here must fit inside the reserved chunk
        // from GrpcServiceClient; see CreateMetaType
    }

    public abstract class QueryDb<T> : QueryBase, IQueryDb<T>, IReturn<QueryResponse<T>> { }
    public abstract class QueryDb<From, Into> : QueryBase, IQueryDb<From, Into>, IReturn<QueryResponse<Into>> { }

    public abstract class QueryData<T> : QueryBase, IQueryData<T>, IReturn<QueryResponse<T>> { }
    public abstract class QueryData<From, Into> : QueryBase, IQueryData<From, Into>, IReturn<QueryResponse<Into>> { }

    public interface IQueryResponse : IHasResponseStatus, IMeta
    {
        int Offset { get; set; }
        int Total { get; set; }
    }

    [DataContract]
    public class QueryResponse<T> : IQueryResponse
    {
        [DataMember(Order = 1)]
        public virtual int Offset { get; set; }

        /// <summary>
        /// Populate with Include=Total or if registered with: AutoQueryFeature { IncludeTotal = true }
        /// </summary>
        [DataMember(Order = 2)]
        public virtual int Total { get; set; }

        [DataMember(Order = 3)]
        public virtual List<T> Results { get; set; }

        [DataMember(Order = 4)]
        public virtual Dictionary<string, string> Meta { get; set; }

        [DataMember(Order = 5)]
        public virtual ResponseStatus ResponseStatus { get; set; }
    }
    
    /* AutoCrud */
    public enum AutoUpdateStyle
    {
        Always,
        NonDefaults,
    }
    
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public class AutoUpdateAttribute : AttributeBase
    {
        public AutoUpdateStyle Style { get; set; }
        public AutoUpdateAttribute(AutoUpdateStyle style) => Style = style;
    }
    
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public class AutoDefaultAttribute : ScriptValueAttribute
    {
    }
    
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public class AutoMapAttribute : AttributeBase
    {
        public string To { get; set; }
        public AutoMapAttribute(string to) => To = to ?? throw new ArgumentNullException(nameof(to));
    }
    
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = true)]
    public class AutoPopulateAttribute : ScriptValueAttribute
    {
        /// <summary>
        /// Name of Class Property to Populate
        /// </summary>
        public string Field { get; set; }
        
        public AutoPopulateAttribute(string field) => Field = field ?? throw new ArgumentNullException(nameof(field));
    }
    
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public class AutoIgnoreAttribute : AttributeBase {}
}