namespace ServiceStack.Blazor;

/// <summary>
/// Also extend functionality to any class implementing IHasJsonApiClient
/// </summary>
public static class BlazorUtils
{
    public static void Log(string? message = null)
    {
        if (BlazorConfig.Instance.EnableVerboseLogging)
            Console.WriteLine(message ?? "");
    }
}

