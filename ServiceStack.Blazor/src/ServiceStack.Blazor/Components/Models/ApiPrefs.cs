namespace ServiceStack.Blazor.Components;

public class ApiPrefs
{
    public static int DefaultTake = 25;
    public int Take { get; set; } = DefaultTake;
    public List<string> SelectedColumns { get; set; } = new();
    public void Clear()
    {
        SelectedColumns.Clear();
        Take = DefaultTake;
    }
    public ApiPrefs Clone() => new ApiPrefs
    {
        Take = Take, 
        SelectedColumns = SelectedColumns.ToList(),
    };

    public static ApiPrefs Create(int? take = null, List<string>? columns=null) => new ApiPrefs { 
        Take = take ?? DefaultTake,
        SelectedColumns = columns ?? new(),
    };
}
