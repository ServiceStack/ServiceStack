namespace ServiceStack.Blazor.Components.Bootstrap;

public partial class DateTimeInput<TValue>
{
    protected DateTime? CurrentValueAsDateTime { get => CurrentValue is DateTime dt ? dt : null; set => CurrentValue = value is not null ? (TValue)(object)value : default; }
}
