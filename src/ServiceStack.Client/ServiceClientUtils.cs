#nullable enable
using System;
using System.Linq;

namespace ServiceStack;

public static class ServiceClientUtils
{
    public static string? GetHttpMethod(Type requestType)
    {
        var interfaceTypes = requestType.GetInterfaces();
        return GetIVerbMethod(interfaceTypes) ?? GetAutoQueryMethod(requestType);
    }

    public static string? GetIVerbMethod(Type requestType) => GetIVerbMethod(requestType.GetInterfaces());
    public static string? GetIVerbMethod(Type[] interfaceTypes)
    {
        if (interfaceTypes.Contains(typeof(IVerb)))
        {
            if (interfaceTypes.Contains(typeof(IGet)))
                return HttpMethods.Get;
            if (interfaceTypes.Contains(typeof(IPost)))
                return HttpMethods.Post;
            if (interfaceTypes.Contains(typeof(IPut)))
                return HttpMethods.Put;
            if (interfaceTypes.Contains(typeof(IPatch)))
                return HttpMethods.Patch;
            if (interfaceTypes.Contains(typeof(IDelete)))
                return HttpMethods.Delete;
            if (interfaceTypes.Contains(typeof(IOptions)))
                return HttpMethods.Options;
        }
        return null;
    }

    public static string? GetAutoQueryMethod(Type requestType)
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
        if (typeof(IQuery).IsAssignableFrom(requestType))
            return HttpMethods.Get;
        return null;
    }
}