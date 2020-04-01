using System;
using System.Collections.Generic;
using System.Linq;
using ServiceStack.Data;
using ServiceStack.NativeTypes;
using ServiceStack.OrmLite;
using ServiceStack.Web;

namespace ServiceStack
{
    public interface IGenerateCrudServices
    {
        List<string> IncludeCrudOperations { get; set; }

        /// <summary>
        /// Generate services 
        /// </summary>
        List<CreateCrudServices> CreateServices { get; set; }
        Func<ColumnSchema, IOrmLiteDialectProvider, Type> ResolveColumnType { get; set; }
        Action<MetadataTypes, MetadataTypesConfig, IRequest> MetadataTypesFilter { get; set; }
        Action<MetadataType, IRequest> TypeFilter { get; set; }
        Action<MetadataOperationType, IRequest> ServiceFilter { get; set; }
        Func<MetadataType, bool> IncludeType { get; set; }
        Func<MetadataOperationType, bool> IncludeService { get; set; }
        string AccessRole { get; set; }
        Dictionary<Type, string[]> ServiceRoutes { get; set; }
        DbSchema GetCachedDbSchema(IDbConnectionFactory dbFactory, string schema = null, string namedConnection = null);
        void Register(IAppHost appHost);
        List<Type> GenerateMissingServices(AutoQueryFeature feature);
    }

    public static class CrudUtils
    {
        public static MetadataAttribute ToAttribute(string name, Dictionary<string, object> args = null,
            Attribute attr = null) =>
            new MetadataAttribute {
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
            type.Attributes ??= new List<MetadataAttribute>();
            type.Attributes.Add(metaAttr);
            return type;
        }

        public static MetadataType AddAttribute(this MetadataType type, Attribute attr)
        {
            var nativeTypesGen = HostContext.AssertPlugin<NativeTypesFeature>().DefaultGenerator;
            var metaAttr = nativeTypesGen.ToMetadataAttribute(attr);
            type.Attributes ??= new List<MetadataAttribute>();
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
            propType.Attributes ??= new List<MetadataAttribute>();
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
    public class CreateCrudServices
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
        /// Is used as a Whitelist to specify only the types you would like to have code-generated, see:
        /// https://docs.servicestack.net/csharp-add-servicestack-reference#includetypes
        /// </summary>
        public List<string> IncludeTypes { get; set; }

        /// <summary>
        /// Is used as a Blacklist to specify which types you would like excluded from being generated. see:
        /// https://docs.servicestack.net/csharp-add-servicestack-reference#excludetypes
        /// </summary>
        public List<string> ExcludeTypes { get; set; }
    }

    public class CrudCodeGenTypes : NativeTypesBase, IReturn<string>
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
    }

    public class CrudTables : IReturn<AutoCodeSchemaResponse>
    {
        public string Schema { get; set; }
        public string NamedConnection { get; set; }
        public string AuthSecret { get; set; }
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

        public List<TableSchema> Tables { get; set; } = new List<TableSchema>();
    }

    public class TableSchema
    {
        public string Name { get; set; }

        public ColumnSchema[] Columns { get; set; }

        public string ErrorType { get; set; }
        public string ErrorMessage { get; set; }
    }
}