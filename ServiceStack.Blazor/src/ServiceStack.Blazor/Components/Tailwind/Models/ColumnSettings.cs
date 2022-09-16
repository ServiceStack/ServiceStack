namespace ServiceStack.Blazor.Components.Tailwind;

public class ColumnSettings
{
    public List<Filter> Filters { get; set; } = new();
    public SortOrder? SortOrder { get; set; }

    public void Clear()
    {
        Filters.Clear();
        SortOrder = null;
    }
}

public class Filter
{
    public string Key { get; set; }
    public string Name { get; set; }
    public string? Value { get; set; }
    public List<string>? Values { get; set; }
}

public enum SortOrder
{
    Ascending,
    Descending,
}

public class FilterModel
{
    public string Query = "";
    public string Value = "";
}
