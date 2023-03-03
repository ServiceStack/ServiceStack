using ServiceStack;
using System.Collections.Concurrent;
using System.Reflection;

namespace ServiceStack.Blazor.Components;

public class Apis
{
    public Type? Query { get; set; }
    public Type? QueryInto { get; set; }
    public Type? Create { get; set; }
    public Type? Update { get; set; }
    public Type? Patch { get; set; }
    public Type? Delete { get; set; }
    public Type? Save { get; set; }

    static ConcurrentDictionary<string, Type> ApiTyesMap { get; set; } = new();
    static ConcurrentDictionary<Assembly, bool> loadedAssemblies = new();
    public static void Load(Assembly assembly)
    {
        if (loadedAssemblies.ContainsKey(assembly))
            return;
        loadedAssemblies[assembly] = true;

        var apiTypes = assembly.GetTypes().Where(x => x.HasInterface(typeof(IReturnVoid)) || x.IsOrHasGenericInterfaceTypeOf(typeof(IReturn<>)));
        foreach (var apiType in apiTypes)
        {
            ApiTyesMap[apiType.Name] = apiType;
        }
    }
    public static Type? Find(string typeName) => ApiTyesMap.TryGetValue(typeName, out var type) ? type : null;

    public Apis(Type[] types)
    {
        foreach (var type in types)
        {
            if (!ApiTyesMap.ContainsKey(type.Name))
            {
                Load(type.Assembly);
            }

            if (typeof(IQuery).IsAssignableFrom(type))
            {
                var genericDef = type.GetTypeWithGenericTypeDefinitionOf(typeof(IQueryDb<,>));
                if (genericDef != null)
                    QueryInto = type;
                else
                    Query = type;
            }
            if (type.IsOrHasGenericInterfaceTypeOf(typeof(ICreateDb<>)))
                Create = type;
            else if (type.IsOrHasGenericInterfaceTypeOf(typeof(IPatchDb<>)))
                Patch = type;
            else if (type.IsOrHasGenericInterfaceTypeOf(typeof(IUpdateDb<>)))
                Update = type;
            else if (type.IsOrHasGenericInterfaceTypeOf(typeof(IDeleteDb<>)))
                Delete = type;
            else if (type.IsOrHasGenericInterfaceTypeOf(typeof(ISaveDb<>)))
                Save = type;
        }
    }

    public static Apis AutoQuery<T>() => new Apis(new[] { typeof(T) });
    public static Apis AutoQuery<T1, T2>() => new Apis(new[] { typeof(T1), typeof(T2) });
    public static Apis AutoQuery<T1, T2, T3>() => new Apis(new[] { typeof(T1), typeof(T2), typeof(T3) });
    public static Apis AutoQuery<T1, T2, T3, T4>() => new Apis(new[] { typeof(T1), typeof(T2), typeof(T3), typeof(T4) });
    public static Apis AutoQuery<T1, T2, T3, T4, T5>() => new Apis(new[] { typeof(T1), typeof(T2), typeof(T3), typeof(T4), typeof(T5) });

    public QueryBase QueryRequest<Model>() => (Query ?? QueryInto).CreateInstance<QueryBase>();
    public IDeleteDb<Model> CreateRequest<Model>() => Create.CreateInstance<IDeleteDb<Model>>();
    public IUpdateDb<Model> UpdateRequest<Model>() => Create.CreateInstance<IUpdateDb<Model>>();
    public IPatchDb<Model> PatchRequest<Model>() => Create.CreateInstance<IPatchDb<Model>>();
    public IDeleteDb<Model> DeleteRequest<Model>() => Create.CreateInstance<IDeleteDb<Model>>();
    public ISaveDb<Model> SaveRequest<Model>() => Create.CreateInstance<ISaveDb<Model>>();
}
