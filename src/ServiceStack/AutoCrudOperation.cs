using System;
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