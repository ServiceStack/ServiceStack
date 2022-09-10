namespace MyApp.Client.Components;

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
}
