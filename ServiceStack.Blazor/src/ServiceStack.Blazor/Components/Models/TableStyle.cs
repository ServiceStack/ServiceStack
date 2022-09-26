namespace ServiceStack.Blazor.Components;

// https://tailwindui.com/components/application-ui/lists/tables
[Flags]
public enum TableStyle
{
    Simple = 0,
    FullWidth = 1 << 0,
    StripedRows = 1 << 1,
    WhiteBackground = 1 << 2,
    UppercaseHeadings = 1 << 3,
    VerticalLines = 1 << 4,
}
