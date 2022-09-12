namespace ServiceStack.Blazor;
public static class BlazorConfig
{
    public static bool EnableVerboseLogging { get; set; } = false;
}

public static class KeyCodes
{
    public const string Escape = nameof(Escape);
    public const string ArrowLeft = nameof(ArrowLeft);
    public const string ArrowRight = nameof(ArrowRight);
    public const string ArrowUp = nameof(ArrowUp);
    public const string ArrowDown = nameof(ArrowDown);
    public const string Home = nameof(Home);
    public const string End = nameof(End);
}