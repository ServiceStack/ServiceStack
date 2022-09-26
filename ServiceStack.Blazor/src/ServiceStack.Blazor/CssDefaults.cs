using ServiceStack.Blazor.Components;

namespace ServiceStack.Blazor;

/// <summary>
/// For CSS classes used in *.cs so they're exported to tailwind.html
/// </summary>
public static class CssDefaults
{
    public static class Grid
    {
        public const string GridClass = "mt-4 flex flex-col";
        public const string HoverSelectionClass = "cursor-pointer hover:bg-yellow-50";
        public const string SelectedClass = "cursor-pointer bg-indigo-100";
        public const string OutlineClass = "shadow overflow-hidden border-b border-gray-200 sm:rounded-lg";
    }

    public static class Form
    {
        public const string FormClass = "flex h-full flex-col divide-y divide-gray-200 bg-white shadow-xl";
        public const string PanelClass = "pointer-events-auto w-screen xl:max-w-3xl md:max-w-xl max-w-lg";
        public const string TitlebarClass = "bg-gray-50 px-4 py-6 sm:px-6";
        public const string HeadingClass = "text-lg font-medium text-gray-900";

        public static DataTransition SlideOverTransition = new DataTransition(
            entering: new(@class: "transform transition ease-in-out duration-500 sm:duration-700", from: "translate-x-full", to: "translate-x-0"),
            leaving: new(@class: "transform transition ease-in-out duration-500 sm:duration-700", from: "translate-x-0", to: "translate-x-full"),
            visible: false); 
    }

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
}

public enum Breakpoint
{
    ExtraSmall,
    Small,
    Medium,
    Large,
    ExtraLarge,
    ExtraLarge2x,
}
