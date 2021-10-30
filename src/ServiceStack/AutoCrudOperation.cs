using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

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

        public static bool IsCrud(this MetadataOperationType op) => op.IsCrudRead() || op.IsCrudWrite();

        public static bool IsCrudWrite(this MetadataOperationType op) => 
            op.Request.Implements?.Any(iface => CrudWriteNames.Contains(iface.Name)) == true;

        public static bool IsCrudRead(this MetadataOperationType op) => 
            op.Request.Inherits?.Name == typeof(QueryDb<>).Name || op.Request.Inherits?.Name == typeof(QueryDb<,>).Name;
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