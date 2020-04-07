using System;
using System.Collections;
using System.Collections.Generic;

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

        public static List<string> Default { get; } = new List<string> {
            Query,
            Create,
            Update,
            Patch,
            Delete,
        };

        public static HashSet<string> All { get; } = new HashSet<string> {
            Query,
            Create,
            Update,
            Patch,
            Delete,
            Save,
        };

        public static List<string> Read { get; } = new List<string> {
            Query,
        };

        private static string[] readInterfaces;
        public static string[] ReadInterfaces => readInterfaces ??= CrudInterfaceMetadataNames(Read).ToArray();

        public static List<string> Write { get; } = new List<string> {
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

        public static AutoCrudDtoType? GetCrudGenericDefTypes(Type requestType, Type crudType)
        {
            var genericDef = requestType.GetTypeWithGenericTypeDefinitionOf(crudType);
            if (genericDef != null)
                return new AutoCrudDtoType(genericDef, crudType);
            return null;
        }

        public static AutoCrudDtoType? GetAutoCrudDtoType(Type requestType)
        {
            var crudTypes = GetCrudGenericDefTypes(requestType, typeof(ICreateDb<>))
                ?? GetCrudGenericDefTypes(requestType, typeof(IUpdateDb<>))
                ?? GetCrudGenericDefTypes(requestType, typeof(IDeleteDb<>))
                ?? GetCrudGenericDefTypes(requestType, typeof(IPatchDb<>))
                ?? GetCrudGenericDefTypes(requestType, typeof(ISaveDb<>));
            return crudTypes;
        }

        public static AutoCrudDtoType AssertAutoCrudDtoType(Type requestType) =>
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
            return crudTypes?.GenericDef.GenericTypeArguments[0];
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
    }
    
    public struct AutoCrudDtoType
    {
        public Type GenericDef { get; }
        public Type ModelType { get; }
        public AutoCrudDtoType(Type genericDef, Type modelType)
        {
            GenericDef = genericDef;
            ModelType = modelType;
        }
    }
    
}