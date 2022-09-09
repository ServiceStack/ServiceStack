using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using ServiceStack;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using static System.Net.Mime.MediaTypeNames;

namespace MyApp.Client.Components;

public enum Breakpoint
{
    ExtraSmall,
    Small,
    Medium,
    Large,
    ExtraLarge,
    ExtraLarge2x,
}

public enum SortOrder
{
    Ascending,
    Descending,
}

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

public class Filter
{
    public string Key { get; set; }
    public string Name { get; set; }
    public string? Value { get; set; }
    public List<string>? Values { get; set; }
}

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

public struct DOMRect
{
    public double X { get; set; }
    public double Y { get; set; }
    public double Width { get; set; }
    public double Height { get; set; }
    public double Top { get; set; }
    public double Right { get; set; }
    public double Bottom { get; set; }
    public double Left { get; set; }
}

public class Apis
{
    public Type? Query { get; set; }
    public Type? QueryInto { get; set; }
    public Type? Create { get; set; }
    public Type? Update { get; set; }
    public Type? Patch { get; set; }
    public Type? Delete { get; set; }
    public Type? Save { get; set; }

    public Apis(Type[] types)
    {
        foreach (var type in types)
        {

            if (typeof(IQuery).IsAssignableFrom(type))
            {
                var genericDef = type.GetTypeWithGenericTypeDefinitionOf(typeof(IQueryDb<,>));
                if (genericDef != null)
                    QueryInto = type;
                else
                    Query = type;
            }
            if (type.IsOrHasGenericInterfaceTypeOf(typeof(ICreateDb<>)))
                Create = type;
            else if (type.IsOrHasGenericInterfaceTypeOf(typeof(IUpdateDb<>)))
                Update = type;
            else if (type.IsOrHasGenericInterfaceTypeOf(typeof(IDeleteDb<>)))
                Delete = type;
            else if (type.IsOrHasGenericInterfaceTypeOf(typeof(IPatchDb<>)))
                Patch = type;
            else if (type.IsOrHasGenericInterfaceTypeOf(typeof(ISaveDb<>)))
                Save = type;
        }
    }

    public static Apis AutoQuery<T>() => new Apis(new[] { typeof(T) });
    public static Apis AutoQuery<T1, T2>() => new Apis(new[] { typeof(T1), typeof(T2) });
    public static Apis AutoQuery<T1, T2, T3>() => new Apis(new[] { typeof(T1), typeof(T2), typeof(T3) });
    public static Apis AutoQuery<T1, T2, T3, T4>() => new Apis(new[] { typeof(T1), typeof(T2), typeof(T3), typeof(T4) });
    public static Apis AutoQuery<T1, T2, T3, T4, T5>() => new Apis(new[] { typeof(T1), typeof(T2), typeof(T3), typeof(T4), typeof(T5) });

    public QueryBase QueryRequest<Model>() => (QueryInto ?? Query).CreateInstance<QueryBase>();
    public IDeleteDb<Model> CreateRequest<Model>() => Create.CreateInstance<IDeleteDb<Model>>();
    public IUpdateDb<Model> UpdateRequest<Model>() => Create.CreateInstance<IUpdateDb<Model>>();
    public IPatchDb<Model> PatchRequest<Model>() => Create.CreateInstance<IPatchDb<Model>>();
    public IDeleteDb<Model> DeleteRequest<Model>() => Create.CreateInstance<IDeleteDb<Model>>();
    public ISaveDb<Model> SaveRequest<Model>() => Create.CreateInstance<ISaveDb<Model>>();
}

/// <summary>
/// To capture Tailwind's animation rules, e.g:
/// {
///     entering: { cls:'ease-out duration-300', from:'opacity-0',    to:'opacity-100' },
///      leaving: { cls:'ease-in duration-200',  from:'opacity-100',  to: 'opacity-0'  }
/// }
/// {
///    entering: { cls:'ease-out duration-300', from:'opacity-0 translate-y-4 sm:translate-y-0 sm:scale-95', to:'opacity-100 translate-y-0 sm:scale-100' }, 
///     leaving: { cls:'ease-in duration-200',  from:'opacity-100 translate-y-0 sm:scale-100',               to:'opacity-0 translate-y-4 sm:translate-y-0 sm:scale-95' }
/// }
/// </summary>
public class DataTransition
{
    public DataTransitionEvent Entering { get; }
    public DataTransitionEvent Leaving { get; }
    public bool EnteringState { get; set; }
    public string Class { get; set; }
    public string DisplayClass => EnteringState ? "block" : "hidden";
    public string OpacityClass => EnteringState ? "opacity-100" : "opacity-0";

    bool transitioning = false;
    Action StateChanged;

    public async Task TransitionAsync(bool show, Action? onChange)
    {
        if (!CanAcquireLock())
            return;

        //Console.WriteLine("TransitionAsync: " + show);

        for (var step=0; step<TotalSteps; step++)
        {
            TransitionStep(show, onChange, step);
            //Console.WriteLine($"Step {step}. ({show}): {Class}");
            onChange?.Invoke();
            await Task.Delay(100);
        }

        //Console.WriteLine();
        ReleaseLock();
    }

    bool CanAcquireLock()
    {
        if (transitioning) return false; // ignore multiple calls during transition
        return transitioning = true;
    }
    void ReleaseLock() => transitioning = false;


    const int TotalSteps = 2;
    void TransitionStep(bool show, Action? onChange, int step)
    {
        //var prev = show
        //    ? Leaving
        //    : Entering;
        var next = show
            ? Entering
            : Leaving;

        switch (step)
        {
            case 0:
                Class = CssUtils.ClassNames(next.Class, next.From);
                break;
            case 1:
                Class = CssUtils.ClassNames(next.Class, next.To);
                break;
        }
    }

    public static async Task TransitionAllAsync(bool show, Action? onChange, params DataTransition[] transitions)
    {
        if (!transitions.All(x => x.EnteringState != show))
            return;
        if (!transitions.All(x => x.CanAcquireLock())) 
            return;

        //Console.WriteLine("TransitionAllAsync: " + show);

        if (show)
            transitions.Each(x => x.Show(show));

        for (var step = 0; step < TotalSteps; step++)
        {
            foreach (var transition in transitions)
            {
                transition.TransitionStep(show, onChange, step);
                //Console.WriteLine($"Step {step}. ({show}): {transition.Class}");
            }
            onChange?.Invoke();
            await Task.Delay(100);
        }

        if (!show)
            transitions.Each(x => x.Show(show));

        //Console.WriteLine();
        transitions.Each(x => x.ReleaseLock());
    }

    public DataTransition(DataTransitionEvent entering, DataTransitionEvent leaving, bool visible = false)
    {
        Entering = entering;
        Leaving = leaving;
        Show(visible);
    }


    public void Show(bool visible)
    {
        EnteringState = visible;
        //Class = EnteringState
        //    ? CssUtils.ClassNames(DisplayClass)
        //    : CssUtils.ClassNames(DisplayClass);
    }
}

public class DataTransitionEvent
{
    public string Class { get; }
    public string From { get; }
    public string To { get; }
    public DataTransitionEvent(string @class, string from, string to)
    {
        Class = @class;
        From = from;
        To = to;
    }
}

public static class ComponentUtils
{
    public static int nextId = 0;
    public static int NextId() => nextId++;


    public static string ToBreakpointCellClass(this Breakpoint breakpoint) => breakpoint switch
    {
        // Use full class names so tailwindcss can find it
        Breakpoint.ExtraSmall => "xs:table-cell",
        Breakpoint.Small => "sm:table-cell",
        Breakpoint.Medium => "md:table-cell",
        Breakpoint.Large => "lg:table-cell",
        Breakpoint.ExtraLarge => "xl:table-cell",
        Breakpoint.ExtraLarge2x => "2xl:table-cell",
        _ => throw new NotSupportedException(),
    };
    public static bool IsComplexType(this Type type) => !type.IsValueType && type != typeof(string);
}

public class ComponentConfig
{
    public static List<AutoQueryConvention> DefaultFilters = new List<AutoQueryConvention> {
        Definition("=","%"),
        Definition("!=","%!"),
        Definition("<","<%"),
        Definition("<=","%<"),
        Definition(">","%>"),
        Definition(">=",">%"),
        Definition("In","%In"),
        Definition("Starts With","%StartsWith", x => x.Types = "string"),
        Definition("Contains","%Contains", x => x.Types = "string"),
        Definition("Ends With","%EndsWith", x => x.Types = "string"),
        Definition("Exists","%IsNotNull", x => x.ValueType = "none"),
        Definition("Not Exists","%IsNull", x => x.ValueType = "none"),
    };

    static AutoQueryConvention Definition(string name, string value, Action<AutoQueryConvention>? fn = null) =>
        X.Apply(new() { Name = name, Value = value }, fn);
}

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

