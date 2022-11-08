using Microsoft.JSInterop;
using System.Collections.Concurrent;

namespace ServiceStack.Blazor;

public interface ILocalStorage
{
    Task SetStringAsync(string name, string value);
    Task SetItemAsync<T>(string name, T value);
    Task<string?> GetStringAsync(string name);
    Task<T?> GetItemAsync<T>(string name);
    Task RemoveAsync(string name);
}

public class LocalStorage : ILocalStorage
{
    readonly IJSRuntime js;
    public LocalStorage(IJSRuntime js) => this.js = js;

    public async Task SetStringAsync(string name, string value)
    {
        await js.InvokeVoidAsync("localStorage.setItem", name, value);
    }

    public Task SetItemAsync<T>(string name, T value) => SetStringAsync(name, value.ToJson());

    public async Task<string?> GetStringAsync(string name) => 
        await js.InvokeAsync<string?>("localStorage.getItem", new object[] { name });

    public async Task<T?> GetItemAsync<T>(string name)
    {
        var str = await GetStringAsync(name);
        return str.FromJson<T>();
    }

    public async Task RemoveAsync(string name)
    {
        await js.InvokeVoidAsync("localStorage.removeItem", name);
    }
}

public class CachedLocalStorage : ILocalStorage
{
    public LocalStorage LocalStorage { get; }
    public CachedLocalStorage(LocalStorage localStorage)
    {
        LocalStorage = localStorage;
    }

    ConcurrentDictionary<string, string> cache = new();

    public async Task SetItemAsync<T>(string name, T value)
    {
        await LocalStorage.SetStringAsync(name, cache[name] = value.ToJson());
    }

    public async Task SetStringAsync(string name, string value)
    {
        await LocalStorage.SetStringAsync(name, cache[name] = value);
    }

    public string? GetCachedString(string name) => cache.TryGetValue(name, out var str)
        ? str
        : null;

    public T? GetCachedItem<T>(string name) => cache.TryGetValue(name, out var str)
        ? str.FromJson<T>()
        : default;

    public async Task<string?> GetStringAsync(string name)
    {
        if (cache.TryGetValue(name, out var str))
            return str;

        str = await LocalStorage.GetStringAsync(name);
        return str != null
            ? cache[name] = str
            : null;
    }

    public async Task<T?> GetItemAsync<T>(string name)
    {
        var str = await GetStringAsync(name);
        return str.FromJson<T>();
    }

    public async Task RemoveAsync(string name)
    {
        cache.TryRemove(name, out _);
        await LocalStorage.RemoveAsync(name);
    }
}