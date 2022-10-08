namespace ServiceStack.Html;

public static class Formats
{
    public static FormatInfo Currency { get; set; } = new() { Method = FormatMethods.Currency };
    public static FormatInfo Bytes { get; set; } = new() { Method = FormatMethods.Bytes };
    public static FormatInfo Icon { get; set; } = new() { Method = FormatMethods.Icon };
    public static FormatInfo IconRounded { get; set; } = new() { Method = FormatMethods.IconRounded };
    public static FormatInfo Attachment { get; set; } = new() { Method = FormatMethods.Attachment };
    public static FormatInfo Link { get; set; } = new() { Method = FormatMethods.Link };
    public static FormatInfo LinkEmail { get; set; } = new() { Method = FormatMethods.LinkEmail };
    public static FormatInfo LinkPhone { get; set; } = new() { Method = FormatMethods.LinkPhone };
    public static FormatInfo Hidden { get; set; } = new() { Method = FormatMethods.Hidden };
}
