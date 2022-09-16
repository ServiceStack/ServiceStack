namespace ServiceStack.Blazor;

/// <summary>
/// Also extend functionality to any class implementing IHasJsonApiClient
/// </summary>
public static class BlazorUtils
{
    public static int nextId = 0;
    public static int NextId() => nextId++;
    public static bool IsComplexType(this Type type) => !type.IsValueType && type != typeof(string);


    public static void Log(string? message = null)
    {
        if (BlazorConfig.Instance.EnableVerboseLogging)
            Console.WriteLine(message ?? "");
    }
}

