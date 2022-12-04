using ServiceStack.Html;
using System.Linq.Expressions;

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

    public static ApiPrefs Columns<T>(Expression<Func<T, object?>> expr) => Create(null, expr.GetFieldNames().ToList());
    public static ApiPrefs Columns(params string[] columns) => Create(null, columns.ToList());

    public static ApiPrefs Configure(Action<ApiPrefs> configure)
    {
        var to = new ApiPrefs();
        configure(to);
        return to;
    }

    public static ApiPrefs Create(int? take = null, List<string>? columns=null) => new ApiPrefs { 
        Take = take ?? DefaultTake,
        SelectedColumns = columns ?? new(),
    };
}
