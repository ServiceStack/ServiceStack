using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace ServiceStack.Blazor;

public abstract class AttributeBuilder
{
    public abstract void AddAttribute(RenderTreeBuilder builder, int sequence, string name);
}

public class EventCallbackBuilder<T> : AttributeBuilder
{
    EventCallback<T> GenericCallback { get; }
    EventCallback<object> Callback { get; }

    public EventCallbackBuilder(EventCallback<object> callback)
    {
        Callback = callback;
        GenericCallback = EventCallback.Factory.Create<T>(this, Invoke);
    }

    public Task Invoke(T obj)
    {
        return Callback.InvokeAsync(obj);
    }

    public override void AddAttribute(RenderTreeBuilder builder, int sequence, string name)
    {
        builder.AddAttribute<T>(sequence, name, GenericCallback);
    }
}
