using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using ServiceStack.Data;
using ServiceStack.NativeTypes;
using ServiceStack.OrmLite;
using ServiceStack.Web;

namespace ServiceStack;

public class AutoGenContext(CrudCodeGenTypes instruction, string tableName, TableSchema tableSchema)
{
    /// <summary>
    /// AutoGen Request DTO Instruction
    /// </summary>
    public CrudCodeGenTypes Instruction { get; set; } = instruction;

    /// <summary>
    /// Original Table Name
    /// </summary>
    public string TableName { get; } = tableName;

    /// <summary>
    /// RDBMS TableSchema
    /// </summary>
    public TableSchema TableSchema { get; } = tableSchema;

    /// <summary>
    /// Generated DataModel Name to use 
    /// </summary>
    public string DataModelName { get; set; }
        
    /// <summary>
    /// Generated DataModel Name to use for Query Services 
    /// </summary>
    public string PluralDataModelName { get; set; }

    /// <summary>
    /// Generated Route Path base to use 
    /// </summary>
    public string RoutePathBase { get; set; }
        
    /// <summary>
    /// Generated Request DTO Name to use per operation: Query, Create, Update, Patch, Delete
    /// </summary>
    public Dictionary<string,string> OperationNames { get; set; } = new();
        
    /// <summary>
    /// RDBMS Dialect
    /// </summary>
    public IOrmLiteDialectProvider Dialect { get; set; }
        
    /// <summary>
    /// Return what table [Alias] name should be used (if any)
    /// </summary>
    public Func<string> GetTableAlias { get; set; }
        
    /// <summary>
    /// Return what table column [Alias] name should be used (if any)
    /// </summary>
    public Func<MetadataPropertyType, ColumnSchema, string> GetColumnAlias { get; set; }
}
    
public interface IGenerateCrudServices
{
    List<string> IncludeCrudOperations { get; set; }
    List<string> ExcludeTables { get; set; }

    /// <summary>
    /// Generate services 
    /// </summary>
    List<CreateCrudServices> CreateServices { get; set; }
    Action<AutoGenContext> GenerateOperationsFilter { get; set; }
    Func<ColumnSchema, IOrmLiteDialectProvider, Type> ResolveColumnType { get; set; }
    Action<MetadataTypes, MetadataTypesConfig, IRequest> MetadataTypesFilter { get; set; }
    Action<MetadataType, IRequest> TypeFilter { get; set; }
    Action<MetadataOperationType, IRequest> ServiceFilter { get; set; }
    Func<MetadataType, bool> IncludeType { get; set; }
    Func<MetadataOperationType, bool> IncludeService { get; set; }
    bool AddDataContractAttributes { get; set; }
    bool AddIndexesToDataMembers { get; set; }
    string AccessRole { get; set; }
    DbSchema GetCachedDbSchema(IDbConnectionFactory dbFactory, string schema = null, string namedConnection = null,
        List<string> includeTables = null, List<string> excludeTables = null);
    void Configure(IServiceCollection services);
    
    /// <summary>
    /// Generate AutoQuery DTOs for specified RDBMS Tables
    /// </summary>
    /// <param name="feature"></param>
    /// <param name="requestTypes"></param>
    /// <returns>New AutoQuery Request DTOs</returns>
    List<Type> GenerateMissingServices(AutoQueryFeature feature, HashSet<Type> requestTypes);
    
    Action<List<TableSchema>> TableSchemasFilter { get; set; }

    /// <summary>
    /// Override which tables to generate APIs for from a 'schema'  
    /// </summary>
    GetTableNamesDelegate GetTableNames { get; set; }

    /// <summary>
    /// Override which table columns to generate APIs for from a 'table' and 'schema'  
    /// </summary>
    GetTableColumnsDelegate GetTableColumns { get; set; }
}

public delegate List<string> GetTableNamesDelegate(IDbConnection db, string table);
public delegate ColumnSchema[] GetTableColumnsDelegate(IDbConnection db, string table, string schema);

public static class CrudUtils
{
    public static MetadataAttribute ToAttribute(string name, Dictionary<string, object> args = null, Attribute attr = null) => new() {
        Name = name,
        Attribute = attr,
        Args = args?.Map(x => new MetadataPropertyType {
            Name = x.Key,
            Value = x.Value?.ToString(),
            Type = x.Value?.GetType().Name,
        })
    };

    public static MetadataType AddAttribute(this MetadataType type, string name,
        Dictionary<string, object> args = null, Attribute attr = null)
    {
        var metaAttr = ToAttribute(name, args, attr);
        type.Attributes ??= [];
        type.Attributes.Add(metaAttr);
        return type;
    }

    public static MetadataType AddAttribute(this MetadataType type, Attribute attr)
    {
        var nativeTypesGen = HostContext.AssertPlugin<NativeTypesFeature>().DefaultGenerator;
        var metaAttr = nativeTypesGen.ToMetadataAttribute(attr);
        type.Attributes ??= [];
        type.Attributes.Add(metaAttr);
        return type;
    }

    public static MetadataType AddAttributeIfNotExists<T>(this MetadataType type, T attr, Func<T, bool> test=null)
        where T : Attribute
    {
        return type.Attributes?.Any(x => x.Attribute is T t && (test == null || test(t))) == true 
            ? type 
            : AddAttribute(type, attr);
    }

    public static MetadataPropertyType AddAttribute(this MetadataPropertyType propType, Attribute attr)
    {
        var nativeTypesGen = HostContext.AssertPlugin<NativeTypesFeature>().DefaultGenerator;
        var metaAttr = nativeTypesGen.ToMetadataAttribute(attr);
        propType.Attributes ??= [];
        propType.Attributes.Add(metaAttr);
        return propType;
    }

    public static MetadataPropertyType AddAttributeIfNotExists<T>(this MetadataPropertyType propType, T attr, Func<T, bool> test=null)
        where T : Attribute
    {
        return propType.Attributes?.Any(x => x.Attribute is T t && (test == null || test(t))) == true 
            ? propType 
            : AddAttribute(propType, attr);
    }
}


/// <summary>
/// Instruction for which AutoCrud Services to generate
/// </summary>
public class CreateCrudServices : IMeta
{
    /// <summary>
    /// Which AutoCrud Operations to include:
    /// - Query
    /// - Create
    /// - Update
    /// - Patch
    /// - Delete
    /// </summary>
    public List<string> IncludeCrudOperations { get; set; }

    /// <summary>
    /// The RDBMS Schema you want AutoQuery Services generated for
    /// </summary>
    public string Schema { get; set; }

    /// <summary>
    /// The NamedConnection you want AutoQuery Services generated for
    /// </summary>
    public string NamedConnection { get; set; }

    /// <summary>
    /// Include additional C# namespaces
    /// </summary>
    public List<string> AddNamespaces { get; set; }

    /// <summary>
    /// Allow List to specify only the tables you would like to have code-generated
    /// </summary>
    public List<string> IncludeTables { get; set; }

    /// <summary>
    /// Block list to specify which tables you would like excluded from being generated
    /// </summary>
    public List<string> ExcludeTables { get; set; }

    /// <summary>
    /// Allow List to specify only the types you would like to have code-generated, see:
    /// https://docs.servicestack.net/csharp-add-servicestack-reference#includetypes
    /// </summary>
    public List<string> IncludeTypes { get; set; }

    /// <summary>
    /// Block list to specify which types you would like excluded from being generated. see:
    /// https://docs.servicestack.net/csharp-add-servicestack-reference#excludetypes
    /// </summary>
    public List<string> ExcludeTypes { get; set; }

    public Dictionary<string, string> Meta { get; set; }
}

public class CrudCodeGenTypes : NativeTypesBase, IMeta, IReturn<string>
{
    /// <summary>
    /// Either 'all' to include all AutoQuery Services or 'new' to include only missing Services and Types
    /// </summary>
    public string Include { get; set; }

    /// <summary>
    /// The language you want
    ///  csharp
    ///  typescript
    ///  java
    ///  kotlin
    ///  swift
    ///  dart
    ///  vbnet
    ///  fsharp
    ///  typescript.d
    /// </summary>
    public string Lang { get; set; }

    /// <summary>
    /// Which AutoCrud Operations to include:
    /// - Query
    /// - Create
    /// - Update
    /// - Patch
    /// - Delete
    /// </summary>
    public List<string> IncludeCrudOperations { get; set; }

    /// <summary>
    /// The RDBMS Schema you want AutoQuery Services generated for
    /// </summary>
    public string Schema { get; set; }

    /// <summary>
    /// The NamedConnection you want AutoQuery Services generated for
    /// </summary>
    public string NamedConnection { get; set; }

    /// <summary>
    /// The Admin AuthSecret to access Service in Release mode
    /// </summary>
    public string AuthSecret { get; set; }

    /// <summary>
    /// Do not use cached DB Table Schemas, re-fetch latest 
    /// </summary>
    public bool? NoCache { get; set; }

    /// <summary>
    /// Allow List to specify only the tables you would like to have code-generated
    /// </summary>
    public List<string> IncludeTables { get; set; }

    /// <summary>
    /// Block list to specify which tables you would like excluded from being generated
    /// </summary>
    public List<string> ExcludeTables { get; set; }

    public Dictionary<string, string> Meta { get; set; }
}

public class CrudTables : IReturn<AutoCodeSchemaResponse>
{
    public string Schema { get; set; }
    public string NamedConnection { get; set; }
    public string AuthSecret { get; set; }

    /// <summary>
    /// Allow List to specify only the tables you would like to have code-generated
    /// </summary>
    public List<string> IncludeTables { get; set; }

    /// <summary>
    /// Block list to specify which tables you would like excluded from being generated
    /// </summary>
    public List<string> ExcludeTables { get; set; }

    public bool? NoCache { get; set; }
}

public class AutoCodeSchemaResponse
{
    public List<TableSchema> Results { get; set; }

    public ResponseStatus ResponseStatus { get; set; }
}

public class DbSchema
{
    public string Schema { get; set; }
    public string NamedConnection { get; set; }

    public List<TableSchema> Tables { get; set; } = [];
}

public class TableSchema
{
    public string Name { get; set; }

    public ColumnSchema[] Columns { get; set; }

    public string ErrorType { get; set; }
    public string ErrorMessage { get; set; }
}