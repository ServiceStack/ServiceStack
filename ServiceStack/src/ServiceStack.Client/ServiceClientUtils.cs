#nullable enable
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace ServiceStack;

public static class ServiceClientUtils
{
    /// <summary>
    /// HTTP Methods supported my Service Clients
    /// </summary>
    public static HashSet<string> SupportedMethods => new() {
        HttpMethods.Get,
        HttpMethods.Post,
        HttpMethods.Put,
        HttpMethods.Patch,
        HttpMethods.Delete,
        HttpMethods.Options,
    };

    private static readonly ConcurrentDictionary<Type, string?> CachedMethods = new();

    /// <summary>
    /// Get the preferred HTTP method to use with this API, if it either:
    ///  - Implements IVerb marker interface
    ///  - Inherits AutoQuery/CRUD DTO
    ///  - Using a single distinct user defined [Route]
    /// </summary>
    /// <param name="requestType"></param>
    /// <returns>preferred HTTP Method or null if cannot be inferred</returns>
    public static string? GetHttpMethod(Type requestType) => CachedMethods.GetOrAdd(requestType, 
        type => GetIVerbMethod(type) ?? GetSingleRouteMethod(type) ?? GetAutoQueryMethod(type));

    public static string? GetSingleRouteMethod(Type requestType)
    {
        var routeMethods = GetRouteMethods(requestType);
        return routeMethods.Length == 1 ? routeMethods[0] : null;
    }
    
    public static string[] GetRouteMethods(Type requestType) => requestType.AllAttributes<RouteAttribute>()
        .Where(x => x.Verbs != null)
        .Select(x => x.Verbs.ToUpper())
        .Where(SupportedMethods.Contains)
        .Distinct().ToArray();

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

    public static object AssertRequestDto(object requestDto)
    {
        if (requestDto == null)
            throw new ArgumentNullException(nameof(requestDto));
        var requestType = requestDto.GetType();
        if (requestType.IsClass && requestType != typeof(string))
            return requestDto;
        throw new NotSupportedException($"{requestType.Name} is not a valid Request DTO");
    }
}