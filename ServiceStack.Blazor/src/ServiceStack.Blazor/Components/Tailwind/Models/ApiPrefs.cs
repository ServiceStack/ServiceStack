namespace ServiceStack.Blazor.Components.Tailwind;

public class ApiPrefs
{
    public static int DefaultTake = 50;
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
}
