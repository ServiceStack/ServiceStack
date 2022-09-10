using Microsoft.JSInterop;

namespace ServiceStack.Blazor;

public class LocalStorage
{
    readonly IJSRuntime js;
    public LocalStorage(IJSRuntime js) => this.js = js;
    
    public async Task SetItemAsync<T>(string name, T value)
    {
        await js.InvokeVoidAsync("localStorage.setItem", name, value.ToJson());
    }
    
    public async Task<T> GetItemAsync<T>(string name)
    {
        var str = await js.InvokeAsync<string>("localStorage.getItem", new object[] { name });
        return str.FromJson<T>();
    }

    public async Task RemoveItemAsync(string name)
    {
        await js.InvokeVoidAsync("localStorage.removeItem", name);
    }
}
