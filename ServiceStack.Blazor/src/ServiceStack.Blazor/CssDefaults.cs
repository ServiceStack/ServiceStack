using ServiceStack.Blazor.Components;

namespace ServiceStack.Blazor;

/// <summary>
/// For CSS classes used in *.cs so they're exported to tailwind.html
/// </summary>
public static class CssDefaults
{
    public static class Grid
    {
        public const TableStyle DefaultTableStyle = TableStyle.StripedRows;

        public const string GridClass = "mt-4 flex flex-col";
        public const string Grid2Class = "-my-2 -mx-4 overflow-x-auto sm:-mx-6 lg:-mx-8";
        public const string Grid3Class = "inline-block min-w-full py-2 align-middle md:px-6 lg:px-8";
        public const string Grid4Class = "overflow-hidden shadow ring-1 ring-black ring-opacity-5 md:rounded-lg";
                                         
        public const string TableClass = "min-w-full divide-y divide-gray-200";

        public const string TableHeadClass = "bg-gray-50";
        
        public const string TableHeaderRowClass = "select-none";
        public const string TableHeaderCellClass = "px-6 py-4 text-left text-sm font-medium tracking-wider whitespace-nowrap";
        
        public const string TableBodyClass = "";

        public static string GetGridClass(TableStyle style = DefaultTableStyle) => GridClass;
        public static string GetGrid2Class(TableStyle style = DefaultTableStyle) => style.HasFlag(TableStyle.FullWidth)
            ? "overflow-x-auto"
            : Grid2Class;
        
        public static string GetGrid3Class(TableStyle style = DefaultTableStyle) => style.HasFlag(TableStyle.FullWidth)
            ? "inline-block min-w-full py-2 align-middle"
            : Grid3Class;

        public static string GetGrid4Class(TableStyle style = DefaultTableStyle) => style.HasFlag(TableStyle.WhiteBackground)
            ? ""
            : style.HasFlag(TableStyle.FullWidth)
                ? "overflow-hidden shadow-sm ring-1 ring-black ring-opacity-5"
                : Grid4Class;

        public static string GetTableClass(TableStyle style = DefaultTableStyle) => style.HasFlag(TableStyle.FullWidth) || style.HasFlag(TableStyle.VerticalLines)
            ? "min-w-full divide-y divide-gray-300"
            : TableClass;

        public static string GetTableHeadClass(TableStyle style = DefaultTableStyle) => style.HasFlag(TableStyle.WhiteBackground)
            ? ""
            : TableHeadClass;

        public static string GetTableHeaderRowClass(TableStyle style = DefaultTableStyle) =>
            TableHeaderRowClass + (style.HasFlag(TableStyle.VerticalLines) ? " divide-x divide-gray-200" : "");

        public static string GetTableHeaderCellClass(TableStyle style = DefaultTableStyle) =>
            TableHeaderCellClass + (style.HasFlag(TableStyle.UppercaseHeadings) ? " uppercase" : "");

        public static string GetTableBodyClass(TableStyle style = DefaultTableStyle) => 
            (style.HasFlag(TableStyle.WhiteBackground) || style.HasFlag(TableStyle.VerticalLines)
            ? "divide-y divide-gray-200"
            : "")
            + (style.HasFlag(TableStyle.VerticalLines)
            ? " bg-white"
            : "");

        public static string GetTableRowClass(TableStyle style, int i, bool selected, bool allowSelection) =>
            (allowSelection ? "cursor-pointer " : "") + 
                (selected ? "bg-indigo-100" : (allowSelection ? "hover:bg-yellow-50 " : "") + (style.HasFlag(TableStyle.StripedRows)
                    ? (i % 2 == 0 ? "bg-white" : "bg-gray-50")
                    : "bg-white")) + 
                 (style.HasFlag(TableStyle.VerticalLines) ? " divide-x divide-gray-200" : "");
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

    public static class Modal
    {
        public const string SizeClass = "sm:max-w-prose lg:max-w-screen-md xl:max-w-screen-lg 2xl:max-w-screen-xl sm:w-full";
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
