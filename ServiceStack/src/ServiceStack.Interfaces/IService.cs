//Copyright (c) ServiceStack, Inc. All Rights Reserved.
//License: https://raw.github.com/ServiceStack/ServiceStack/master/license.txt

using System.Threading.Tasks;

namespace ServiceStack;

/// <summary>
/// Marker interfaces
/// </summary>
public interface IService { }

public interface IServiceBeforeFilter
{
    void OnBeforeExecute(object requestDto);
}
public interface IServiceBeforeFilterAsync
{
    Task OnBeforeExecuteAsync(object requestDto);
}
public interface IServiceAfterFilter
{
    object OnAfterExecute(object response);
}
public interface IServiceAfterFilterAsync
{
    Task<object> OnAfterExecuteAsync(object response);
}
public interface IServiceErrorFilter
{
    Task<object> OnExceptionAsync(object requestDto, System.Exception ex);
}
public interface IServiceFilters : IServiceBeforeFilter, IServiceAfterFilter, IServiceErrorFilter {}

public interface IReturn { }
public interface IReturn<T> : IReturn { }
public interface IReturnVoid : IReturn { }

/* Supported signatures: */
//Not used or needed, here in-case someone wants to know what the correct signatures should be

//Empty marker interfaces to enforce correct mappings
public interface IVerb {}
public interface IGet : IVerb { }
public interface IPost : IVerb { }
public interface IPut : IVerb { }
public interface IDelete : IVerb { }
public interface IPatch : IVerb { }
public interface IOptions : IVerb { }
public interface IStream : IVerb { }

/// <summary>
/// Marker interface to enforce recommended method signature on ServiceStack Services: object Any(T request)   
/// </summary>
public interface IAny<T>
{
    object Any(T request);
}
public interface IAnyAsync<T>
{
    Task<object> AnyAsync(T request);
}

/// <summary>
/// Marker interface to enforce recommended method signature on ServiceStack Services: object Get(T request)   
/// </summary>
public interface IGet<T>
{
    object Get(T request);
}
public interface IGetAsync<T>
{
    Task<object> GetAsync(T request);
}

/// <summary>
/// Marker interface to enforce recommended method signature on ServiceStack Services: object Post(T request)   
/// </summary>
public interface IPost<T>
{
    object Post(T request);
}
public interface IPostAsync<T>
{
    Task<object> PostAsync(T request);
}

/// <summary>
/// Marker interface to enforce recommended method signature on ServiceStack Services: object Put(T request)   
/// </summary>
public interface IPut<T>
{
    object Put(T request);
}
public interface IPutAsync<T>
{
    Task<object> PutAsync(T request);
}

/// <summary>
/// Marker interface to enforce recommended method signature on ServiceStack Services: object Delete(T request)   
/// </summary>
public interface IDelete<T>
{
    object Delete(T request);
}
public interface IDeleteAsync<T>
{
    Task<object> DeleteAsync(T request);
}

/// <summary>
/// Marker interface to enforce recommended method signature on ServiceStack Services: object Patch(T request)   
/// </summary>
public interface IPatch<T>
{
    object Patch(T request);
}
public interface IPatchAsync<T>
{
    Task<object> PatchAsync(T request);
}

/// <summary>
/// Marker interface to enforce recommended method signature on ServiceStack Services: object Options(T request)   
/// </summary>
// Rename to avoid conflicts with Microsoft.Extensions.Options.IOptions
public interface IOptionsVerb<T>
{
    object Options(T request);
}
public interface IOptionsAsync<T>
{
    Task<object> OptionsAsync(T request);
}


public interface IAnyVoid<T>
{
    void Any(T request);
}
public interface IAnyVoidAsync<T>
{
    Task AnyAsync(T request);
}
public interface IGetVoid<T>
{
    void Get(T request);
}
public interface IGetVoidAsync<T>
{
    Task GetAsync(T request);
}
public interface IPostVoid<T>
{
    void Post(T request);
}
public interface IPostVoidAsync<T>
{
    Task PostAsync(T request);
}
public interface IPutVoid<T>
{
    void Put(T request);
}
public interface IPutVoidAsync<T>
{
    Task PutAsync(T request);
}
public interface IDeleteVoid<T>
{
    void Delete(T request);
}
public interface IDeleteVoidAsync<T>
{
    Task DeleteAsync(T request);
}
public interface IPatchVoid<T>
{
    void Patch(T request);
}
public interface IPatchVoidAsync<T>
{
    Task PatchAsync(T request);
}
public interface IOptionsVoid<T>
{
    void Options(T request);
}
public interface IOptionsVoidAsync<T>
{
    Task OptionsAsync(T request);
}
