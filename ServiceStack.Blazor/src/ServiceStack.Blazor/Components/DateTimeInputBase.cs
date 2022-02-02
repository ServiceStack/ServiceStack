namespace ServiceStack.Blazor.Components;

public class DateTimeInputBase<TValue> : TextInputBase<TValue>
{
    protected DateTime? CurrentValueAsDateTime 
    { 
        get => CurrentValue is DateTime dt ? dt : null; 
        set => CurrentValue = value is not null ? (TValue)(object)value : default; 
    }
}
