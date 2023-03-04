using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace ServiceStack;

public static class Crud 
{
    const string Query = "IQueryDb`1";
    const string QueryInto = "IQueryDb`2";
    const string Create = "ICreateDb`1";
    const string Update = "IUpdateDb`1";
    const string Patch = "IPatchDb`1";
    const string Delete = "IDeleteDb`1";
    const string Save = "ISaveDb`1";

    public static bool HasInterface(MetadataOperationType op, string cls) => op.Request.Implements.FirstOrDefault(i => i.Name == cls) != null;

    static string[] AnyRead = { Query, QueryInto };
    static string[] AnyWrite = { Create, Update, Patch, Delete };

    public static List<string> Default { get; } = new() {
        nameof(Query),
        nameof(Create),
        nameof(Update),
        nameof(Patch),
        nameof(Delete),
    };

    public static HashSet<string> All { get; } = new() {
        nameof(Query),
        nameof(Create),
        nameof(Update),
        nameof(Patch),
        nameof(Delete),
        nameof(Save),
    };

    public static List<string> Read { get; } = new() {
        nameof(Query),
    };

    public static List<string> Write { get; } = new() {
        nameof(Create),
        nameof(Update),
        nameof(Patch),
        nameof(Delete),
        nameof(Save),
    };

    public static string[] CrudInterfaceMetadataNames(List<string> operations = null) =>
        (operations ?? Write).Select(x => $"I{x}Db`1").ToArray();


    public static string[] ReadInterfaces => new[] { Query, QueryInto };
    public static string[] WriteInterfaces => new[] { Create, Update, Patch, Delete, Save };


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
    public static bool IsCrudRead(this MetadataType type) => type.IsAnyQuery();
    /// <summary>
    /// Is AutoQuery QueryDb`1 Request DTO 
    /// </summary>
    public static bool IsQuery(this MetadataType type) =>
        type.Inherits is { Name: "QueryDb`1" };
    /// <summary>
    /// Is AutoQuery Into QueryDb`2 Request DTO 
    /// </summary>
    public static bool IsQueryInto(this MetadataType type) =>
        type.Inherits is { Name: "QueryDb`2" };
    /// <summary>
    /// Is Any AutoQuery Request DTO 
    /// </summary>
    public static bool IsAnyQuery(this MetadataType type) =>
        type.Inherits is { Name: "QueryDb`1" or "QueryDb`2" };
    /// <summary>
    /// Is AutoQuery QueryData`1 Request DTO 
    /// </summary>
    public static bool IsQueryData(this MetadataType type) =>
        type.Inherits is { Name: "QueryData`1" };
    /// <summary>
    /// Is AutoQuery QueryData`2 Request DTO 
    /// </summary>
    public static bool IsQueryDataInto(this MetadataType type) =>
        type.Inherits is { Name: "QueryData`2" };
    /// <summary>
    /// Is AutoQuery Request DTO 
    /// </summary>
    public static bool IsAnyQueryData(this MetadataType type) =>
        type.Inherits is { Name: "QueryData`1" or "QueryData`2" };
    /// <summary>
    /// Is Crud Request DTO 
    /// </summary>
    public static bool IsCrudWrite(this MetadataType type) =>
        type.Implements?.Any(iface => WriteInterfaces.Contains(iface.Name)) == true;
    /// <summary>
    /// Is AutoQuery or Crud Request DTO for Data Model 
    /// </summary>
    public static bool IsCrud(this MetadataType type, string model) =>
        type.IsCrudRead(model) || type.IsCrudWrite(model);
    /// <summary>
    /// Is Crud Request DTO for Data Model 
    /// </summary>
    public static bool IsCrudWrite(this MetadataType type, string model) =>
        type.IsCrudWrite() && type.Implements.Any(x => WriteInterfaces.Contains(x.Name) && x.FirstGenericArg() == model);
    /// <summary>
    /// Is AutoQuery Request DTO for Data Model 
    /// </summary>
    public static bool IsCrudRead(this MetadataType type, string model) => type.IsAutoQuery(model);
    /// <summary>
    /// Is AutoQuery Request DTO for Data Model 
    /// </summary>
    public static bool IsAutoQuery(this MetadataType type, string model) =>
        type.IsAnyQuery() && type.Inherits.FirstGenericArg() == model;

    /// <summary>
    /// Is ICreateDb or ISaveDb Crud Request DTO 
    /// </summary>
    public static bool IsCrudCreate(this MetadataType type) =>
        type.Implements?.Any(iface => iface.Name is Create or Save) == true;
    /// <summary>
    /// Is ICreateDb or ISaveDb Crud Request DTO for Data Model 
    /// </summary>
    public static bool IsCrudCreate(this MetadataType type, string model) =>
        type.Implements?.Any(iface => (iface.Name is Create or Save) && iface.FirstGenericArg() == model) == true;
    /// <summary>
    /// Is IPatchDb, IUpdateDb or ISaveDb Crud Request DTO 
    /// </summary>
    public static bool IsCrudUpdate(this MetadataType type) =>
        type.Implements?.Any(iface => iface.Name is Patch or Update or Save) == true;
    /// <summary>
    /// Is IPatchDb, IUpdateDb or ISaveDb Crud Request DTO for Data Model
    /// </summary>
    public static bool IsCrudUpdate(this MetadataType type, string model) =>
        type.Implements?.Any(iface => (iface.Name is Patch or Update or Save) && iface.FirstGenericArg() == model) == true;
    /// <summary>
    /// Is ICreateDb, IPatchDb, IUpdateDb or ISaveDb Crud Request DTO 
    /// </summary>
    public static bool IsCrudCreateOrUpdate(this MetadataType type) =>
        type.Implements?.Any(iface => iface.Name is Create or Patch or Update or Save) == true;
    /// <summary>
    /// Is ICreateDb, IPatchDb, IUpdateDb or ISaveDb Crud Request DTO for Data Model
    /// </summary>
    public static bool IsCrudCreateOrUpdate(this MetadataType type, string model) =>
        type.Implements?.Any(iface => (iface.Name is Create or Patch or Update or Save) && iface.FirstGenericArg() == model) == true;
    /// <summary>
    /// Is IDeleteDb Crud Request DTO 
    /// </summary>
    public static bool IsCrudDelete(this MetadataType type) =>
        type.Implements?.Any(iface => iface.Name is Delete) == true;
    /// <summary>
    /// Is IDeleteDb Crud Request DTO for Data Model 
    /// </summary>
    public static bool IsCrudDelete(this MetadataType type, string model) =>
        type.Implements?.Any(iface => iface.Name is Delete && iface.FirstGenericArg() == model) == true;

    /// <summary>
    /// Retrieve AutoQuery Data Model from AutoQuery CRUD APIs
    /// </summary>
    public static string CrudModel(this MetadataType type) =>
        type.Inherits is { Name: "QueryDb`1" or "QueryDb`2" }
            ? type.Inherits.FirstGenericArg()
            : type.Implements?.FirstOrDefault(iface => WriteInterfaces.Contains(iface.Name)).FirstGenericArg();

    public static bool IsCrudQuery(Type type) => IsCrudQueryDb(type) || IsCrudQueryData(type);
    public static bool IsCrudQueryDb(Type type) => type.IsOrHasGenericTypeOf(typeof(QueryDb<>)) || type.IsOrHasGenericTypeOf(typeof(QueryDb<,>));
    public static bool IsCrudQueryData(Type type) => type.IsOrHasGenericTypeOf(typeof(QueryData<>)) || type.IsOrHasGenericTypeOf(typeof(QueryData<,>));
    public static bool IsCrudCreate(Type type) => type.IsOrHasGenericInterfaceTypeOf(typeof(ICreateDb<>));
    public static bool IsCrudUpdate(Type type) => type.IsOrHasGenericInterfaceTypeOf(typeof(IUpdateDb<>));
    public static bool IsCrudPatch(Type type) => type.IsOrHasGenericInterfaceTypeOf(typeof(IPatchDb<>));
    public static bool IsCrudDelete(Type type) => type.IsOrHasGenericInterfaceTypeOf(typeof(IDeleteDb<>));


    public static string FirstGenericArg(this MetadataTypeName type) => type.GenericArgs?.Length > 0 ? type.GenericArgs[0] : null;
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
        Create,
        Update,
        Patch,
        Delete,
        Save,
    };
    public static string[] ApiQueryBaseTypes { get; } = {
        typeof(QueryDb<>).Name,
        typeof(QueryDb<,>).Name,
        typeof(QueryData<>).Name,
        typeof(QueryData<,>).Name,
    };
    public static HashSet<string> ApiInterfaces { get; } = CombineSet(ApiMarkerInterfaces, ApiReturnInterfaces, ApiCrudInterfaces);
    public static HashSet<string> ApiBaseTypes { get; } = new(ApiQueryBaseTypes);

    public static HashSet<T> CombineSet<T>(T[] original, params T[][] others)
    {
        var to = new HashSet<T>();
        foreach (var item in original)
        {
            to.Add(item);
        }
        foreach (var arr in others)
        {
            foreach (var item in arr)
            {
                to.Add(item);
            }
        }
        return to;
    }
}