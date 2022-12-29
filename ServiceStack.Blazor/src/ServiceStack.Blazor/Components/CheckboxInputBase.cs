namespace ServiceStack.Blazor.Components;

public class CheckboxInputBase<TValue> : TextInputBase<TValue>
{
    protected bool? CurrentValueAsBool
    {
        get => CurrentValue switch
        {
            bool b => b,
            string s => string.Equals(s, bool.TrueString, StringComparison.OrdinalIgnoreCase),
            _ => null,
        };
#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.
        set => CurrentValue = value is null
            ? default
            : value is TValue v
                ? v
                : typeof(TValue) == typeof(string)
                    ? (TValue)(object)value.ToString()
                    : default;
#pragma warning restore CS8600 // Converting null literal or possible null value to non-nullable type.
    }
}
