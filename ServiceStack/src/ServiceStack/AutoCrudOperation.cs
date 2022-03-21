using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace ServiceStack
{
    public static class AutoCrudOperation
    {
        public const string Query = nameof(Query);
        public const string Create = nameof(Create);
        public const string Update = nameof(Update);
        public const string Patch = nameof(Patch);
        public const string Delete = nameof(Delete);
        public const string Save = nameof(Save);

        public static List<string> Default { get; } = new() {
            Query,
            Create,
            Update,
            Patch,
            Delete,
        };

        public static HashSet<string> All { get; } = new() {
            Query,
            Create,
            Update,
            Patch,
            Delete,
            Save,
        };

        public static List<string> Read { get; } = new() {
            Query,
        };

        private static string[] readInterfaces;
        public static string[] ReadInterfaces => readInterfaces ??= CrudInterfaceMetadataNames(Read).ToArray();

        public static List<string> Write { get; } = new() {
            Create,
            Update,
            Patch,
            Delete,
            Save,
        };

        private static string[] writeInterfaces;
        public static string[] WriteInterfaces => writeInterfaces ??= CrudInterfaceMetadataNames(Write).ToArray();

        public static bool IsOperation(string operation) => All.Contains(operation);

        public static string ToHttpMethod(string operation) => operation switch {
            Create => HttpMethods.Post,
            Update => HttpMethods.Put,
            Patch  => HttpMethods.Patch,
            Delete => HttpMethods.Delete,
            Save   => HttpMethods.Post,
            _ => throw new ArgumentOutOfRangeException(nameof(operation), operation, null)
        };

        public static string ToHttpMethod(Type requestType)
        {
            if (requestType.IsOrHasGenericInterfaceTypeOf(typeof(ICreateDb<>)))
                return HttpMethods.Post;
            if (requestType.IsOrHasGenericInterfaceTypeOf(typeof(IUpdateDb<>)))
                return HttpMethods.Put;
            if (requestType.IsOrHasGenericInterfaceTypeOf(typeof(IDeleteDb<>)))
                return HttpMethods.Delete;
            if (requestType.IsOrHasGenericInterfaceTypeOf(typeof(IPatchDb<>)))
                return HttpMethods.Patch;
            if (requestType.IsOrHasGenericInterfaceTypeOf(typeof(ISaveDb<>)))
                return HttpMethods.Post;
            if (typeof(IQueryDb).IsAssignableFrom(requestType))
                return HttpMethods.Get;

            return null;
        }

        public static string ToOperation(Type genericDef)
        {
            return (genericDef == typeof(IQueryDb<>) || genericDef == typeof(IQueryDb<,>))
                ? Query
                : genericDef == typeof(ICreateDb<>)
                    ? Create
                    : genericDef == typeof(IUpdateDb<>)
                        ? Update
                        : genericDef == typeof(IPatchDb<>)
                            ? Patch
                            : genericDef == typeof(IDeleteDb<>)
                                ? Delete
                                : genericDef == typeof(ISaveDb<>)
                                    ? Save
                                    : null;
        }

        public static AutoQueryDtoType? GetAutoQueryGenericDefTypes(Type requestType, Type opType)
        {
            var genericType = requestType.GetTypeWithGenericTypeDefinitionOf(opType);
            if (genericType != null)
                return new AutoQueryDtoType(genericType, opType);
            return null;
        }

        public static AutoQueryDtoType? GetAutoQueryDtoType(Type requestType) =>
            GetAutoQueryGenericDefTypes(requestType, typeof(IQueryDb<>)) ??
            GetAutoQueryGenericDefTypes(requestType, typeof(IQueryDb<,>)) ??
            GetAutoCrudDtoType(requestType);
        
        public static AutoQueryDtoType? GetAutoCrudDtoType(Type requestType)
        {
            var crudTypes = GetAutoQueryGenericDefTypes(requestType, typeof(ICreateDb<>))
                ?? GetAutoQueryGenericDefTypes(requestType, typeof(IUpdateDb<>))
                ?? GetAutoQueryGenericDefTypes(requestType, typeof(IDeleteDb<>))
                ?? GetAutoQueryGenericDefTypes(requestType, typeof(IPatchDb<>))
                ?? GetAutoQueryGenericDefTypes(requestType, typeof(ISaveDb<>));
            return crudTypes;
        }

        public static AutoQueryDtoType AssertAutoCrudDtoType(Type requestType) =>
            GetAutoCrudDtoType(requestType) ??
            throw new NotSupportedException($"{requestType.Name} is not an ICrud Type");

        public static List<string> CrudInterfaceMetadataNames(List<string> operations = null) =>
            (operations ?? Write).Map(x => $"I{x}Db`1");

        public static Type GetModelType(Type requestType)
        {
            if (requestType == null)
                return null;
            
            var aqTypeDef = requestType.GetTypeWithGenericTypeDefinitionOf(typeof(IQueryDb<>))
                ?? requestType.GetTypeWithGenericTypeDefinitionOf(typeof(IQueryDb<,>));
            if (aqTypeDef != null)
            {
                var args = aqTypeDef.GetGenericArguments();
                return args[0];
            }
                
            var crudTypes = GetAutoCrudDtoType(requestType);
            return crudTypes?.GenericType.GenericTypeArguments[0];
        }

        public static Type GetViewModelType(Type requestType, Type responseType)
        {
            var intoTypeDef = requestType.GetTypeWithGenericTypeDefinitionOf(typeof(IQueryDb<,>));
            if (intoTypeDef != null)
                return intoTypeDef.GetGenericArguments()[1];
            
            var typeDef = requestType.GetTypeWithGenericTypeDefinitionOf(typeof(IQueryDb<>));
            if (typeDef != null)
                return typeDef.GetGenericArguments()[0];

            if (responseType != null)
            {
                var queryResponseDef = responseType.GetTypeWithGenericTypeDefinitionOf(typeof(QueryResponse<>));
                if (queryResponseDef != null)
                    return queryResponseDef.GetGenericArguments()[0];

                var responseProps = TypeProperties.Get(responseType);
                var resultProp = responseProps.GetPublicProperty("Result");
                if (resultProp != null)
                    return resultProp.PropertyType;

                var resultsProp = responseProps.GetPublicProperty("Results");
                if (resultsProp != null && typeof(IEnumerable).IsAssignableFrom(resultsProp.PropertyType))
                    return resultsProp.PropertyType.GetCollectionType();
            }

            return null;
        }

        private static List<string> crudWriteInterfaces;
        private static List<string> CrudWriteNames => crudWriteInterfaces ??= CrudInterfaceMetadataNames(Write); 

        /// <summary>
        /// Is AutoQuery or Crud Request API
        /// </summary>
        public static bool IsCrud(this MetadataOperationType op) => op.IsCrudRead() || op.IsCrudWrite();
        /// <summary>
        /// Is AutoQuery Request DTO 
        /// </summary>
        public static bool IsCrudRead(this MetadataOperationType op) => op.Request.IsCrudRead();
        /// <summary>
        /// Is Crud Request DTO 
        /// </summary>
        public static bool IsCrudWrite(this MetadataOperationType op) => op.Request.IsCrudWrite();
        /// <summary>
        /// Is AutoQuery or Crud Request DTO
        /// </summary>
        public static bool IsCrud(this MetadataType type) => type.IsCrudRead() || type.IsCrudWrite();
        /// <summary>
        /// Is AutoQuery Request DTO 
        /// </summary>
        public static bool IsCrudRead(this MetadataType type) => type.IsAutoQuery();
        /// <summary>
        /// Is AutoQuery Request DTO 
        /// </summary>
        public static bool IsAutoQuery(this MetadataType type) => 
            type.Inherits is { Name: "QueryDb`1" or "QueryDb`2" };
        /// <summary>
        /// Is AutoQuery Request DTO 
        /// </summary>
        public static bool IsAutoQueryData(this MetadataType type) => 
            type.Inherits is { Name: "QueryData`1" or "QueryData`2" };
        /// <summary>
        /// Is Crud Request DTO 
        /// </summary>
        public static bool IsCrudWrite(this MetadataType type) => 
            type.Implements?.Any(iface => CrudWriteNames.Contains(iface.Name)) == true;
        /// <summary>
        /// Is AutoQuery or Crud Request DTO for Data Model 
        /// </summary>
        public static bool IsCrud(this MetadataType type, string model) =>
            type.IsCrudRead(model) || type.IsCrudWrite(model);
        /// <summary>
        /// Is Crud Request DTO for Data Model 
        /// </summary>
        public static bool IsCrudWrite(this MetadataType type, string model) =>
            type.IsCrudWrite() && type.Implements.Any(x => CrudWriteNames.Contains(x.Name) && x.FirstGenericArg() == model);
        /// <summary>
        /// Is AutoQuery Request DTO for Data Model 
        /// </summary>
        public static bool IsCrudRead(this MetadataType type, string model) => type.IsAutoQuery(model);
        /// <summary>
        /// Is AutoQuery Request DTO for Data Model 
        /// </summary>
        public static bool IsAutoQuery(this MetadataType type, string model) => 
            type.IsAutoQuery() && type.Inherits.FirstGenericArg() == model;

        /// <summary>
        /// Is ICreateDb or ISaveDb Crud Request DTO 
        /// </summary>
        public static bool IsCrudCreate(this MetadataType type) => 
            type.Implements?.Any(iface => iface.Name is "ICreateDb`1" or "ISaveDb`1") == true;
        /// <summary>
        /// Is ICreateDb or ISaveDb Crud Request DTO for Data Model 
        /// </summary>
        public static bool IsCrudCreate(this MetadataType type, string model) => 
            type.Implements?.Any(iface => (iface.Name is "ICreateDb`1" or "ISaveDb`1") && iface.FirstGenericArg() == model) == true;
        /// <summary>
        /// Is IPatchDb, IUpdateDb or ISaveDb Crud Request DTO 
        /// </summary>
        public static bool IsCrudUpdate(this MetadataType type) => 
            type.Implements?.Any(iface => iface.Name is "IPatchDb`1" or "IUpdateDb`1" or "ISaveDb`1") == true;
        /// <summary>
        /// Is IPatchDb, IUpdateDb or ISaveDb Crud Request DTO for Data Model
        /// </summary>
        public static bool IsCrudUpdate(this MetadataType type, string model) => 
            type.Implements?.Any(iface => (iface.Name is "IPatchDb`1" or "IUpdateDb`1" or "ISaveDb`1") && iface.FirstGenericArg() == model) == true;
        /// <summary>
        /// Is ICreateDb, IPatchDb, IUpdateDb or ISaveDb Crud Request DTO 
        /// </summary>
        public static bool IsCrudCreateOrUpdate(this MetadataType type) => 
            type.Implements?.Any(iface => iface.Name is "ICreateDb`1" or "IPatchDb`1" or "IUpdateDb`1" or "ISaveDb`1") == true;
        /// <summary>
        /// Is ICreateDb, IPatchDb, IUpdateDb or ISaveDb Crud Request DTO for Data Model
        /// </summary>
        public static bool IsCrudCreateOrUpdate(this MetadataType type, string model) => 
            type.Implements?.Any(iface => (iface.Name is "ICreateDb`1" or "IPatchDb`1" or "IUpdateDb`1" or "ISaveDb`1") && iface.FirstGenericArg() == model) == true;
        /// <summary>
        /// Is IDeleteDb Crud Request DTO 
        /// </summary>
        public static bool IsCrudDelete(this MetadataType type) => 
            type.Implements?.Any(iface => iface.Name is "IDeleteDb`1") == true;
        /// <summary>
        /// Is IDeleteDb Crud Request DTO for Data Model 
        /// </summary>
        public static bool IsCrudDelete(this MetadataType type, string model) => 
            type.Implements?.Any(iface => iface.Name is "IDeleteDb`1" && iface.FirstGenericArg() == model) == true;

        /// <summary>
        /// Retrieve AutoQuery Data Model from AutoQuery CRUD APIs
        /// </summary>
        public static string CrudModel(this MetadataType type) => 
            type.Inherits is { Name: "QueryDb`1" or "QueryDb`2" } 
                ? type.Inherits.FirstGenericArg() 
                : type.Implements?.FirstOrDefault(iface => CrudWriteNames.Contains(iface.Name)).FirstGenericArg();

        public static string FirstGenericArg(this MetadataTypeName type) => type.GenericArgs?.Length > 0 ? type.GenericArgs[0] : null;
        
        public static bool IsRequestDto(this MetadataType type) => HostContext.AppHost?.Metadata.OperationNamesMap.ContainsKey(type.Name) == true
            || type.ImplementsAny(ApiInterfaces) || type.InheritsAny(ApiBaseTypes);

        private static Type[] AutoQueryInterfaceTypes = new[]
        {
            typeof(IQueryDb<>),
            typeof(IQueryDb<,>),
            typeof(ICreateDb<>),
            typeof(IUpdateDb<>),
            typeof(IPatchDb<>),
            typeof(IDeleteDb<>),
            typeof(ISaveDb<>),
        };
        public static bool HasNamedConnection(this MetadataType type, string name) => type.Type != null && 
            (type.Type.FirstAttribute<NamedConnectionAttribute>()?.Name == name || 
             type.Type.FirstAttribute<ConnectionInfo>()?.NamedConnection == name || 
             X.Map(type.Type.GetTypeWithGenericTypeDefinitionOfAny(AutoQueryInterfaceTypes), 
                 x => x.FirstGenericArg()?.FirstAttribute<NamedConnectionAttribute>()?.Name == name));

        public static string[] ApiMarkerInterfaces { get; } = {
            nameof(IGet),
            nameof(IPost),
            nameof(IPut),
            nameof(IDelete),
            nameof(IPatch),
            nameof(IOptions),
            nameof(IStream),
        };
        public static string[] ApiReturnInterfaces { get; } = {
            typeof(IReturn<>).Name,
            nameof(IReturnVoid),
        };
        public static string[] ApiCrudInterfaces { get; } = {
            typeof(ICreateDb<>).Name,
            typeof(IUpdateDb<>).Name,
            typeof(IPatchDb<>).Name,
            typeof(IDeleteDb<>).Name,
            typeof(ISaveDb<>).Name,
        };
        public static string[] ApiQueryBaseTypes { get; } = {
            typeof(QueryDb<>).Name,
            typeof(QueryDb<,>).Name,
            typeof(QueryData<>).Name,
            typeof(QueryData<,>).Name,
        };
        public static HashSet<string> ApiInterfaces { get; } = ApiMarkerInterfaces.CombineSet(ApiReturnInterfaces, ApiCrudInterfaces); 
        public static HashSet<string> ApiBaseTypes { get; } = ApiQueryBaseTypes.ToSet(); 
    }
    
    public struct AutoQueryDtoType
    {
        public Type GenericType { get; }
        public Type GenericDefType { get; }
        public Type ModelType { get; }
        public Type ModelIntoType { get; }
        public string Operation { get; }
        public bool IsRead => AutoCrudOperation.Read.Contains(Operation);
        public bool IsWrite => AutoCrudOperation.Write.Contains(Operation);
        
        public AutoQueryDtoType(Type genericType, Type genericDefType)
        {
            GenericType = genericType;
            GenericDefType = genericDefType;
            var genericArgs = GenericType.GetGenericArguments();
            ModelType = genericArgs[0];
            ModelIntoType = genericDefType == typeof(IQueryDb<,>) ? genericArgs[1] : null;
            Operation = AutoCrudOperation.ToOperation(GenericDefType)
                ?? throw new ArgumentException($"{GenericDefType.Name} is not an AutoQuery Generic Definition Type");
        }
    }
    
}