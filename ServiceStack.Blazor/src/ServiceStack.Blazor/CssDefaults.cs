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
        public const string Grid4Class = "overflow-hidden shadow ring-1 ring-black/5 md:rounded-lg";
                                         
        public const string TableClass = "min-w-full divide-y divide-gray-200 dark:divide-gray-700";

        public const string TableHeadClass = "bg-gray-50 dark:bg-gray-900";
        public const string TableCellClass = "px-6 py-4 whitespace-nowrap text-sm text-gray-500 dark:text-gray-400";

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
                ? "overflow-hidden shadow-sm ring-1 ring-black/5"
                : Grid4Class;

        public static string GetTableClass(TableStyle style = DefaultTableStyle) => style.HasFlag(TableStyle.FullWidth) || style.HasFlag(TableStyle.VerticalLines)
            ? "min-w-full divide-y divide-gray-300"
            : TableClass;

        public static string GetTableHeadClass(TableStyle style = DefaultTableStyle) => style.HasFlag(TableStyle.WhiteBackground)
            ? ""
            : TableHeadClass;

        public static string GetTableHeaderRowClass(TableStyle style = DefaultTableStyle) =>
            TableHeaderRowClass + (style.HasFlag(TableStyle.VerticalLines) ? " divide-x divide-gray-200 dark:divide-gray-700" : "");

        public static string GetTableHeaderCellClass(TableStyle style = DefaultTableStyle) =>
            TableHeaderCellClass + (style.HasFlag(TableStyle.UppercaseHeadings) ? " uppercase" : "");

        public static string GetTableBodyClass(TableStyle style = DefaultTableStyle) => 
            (style.HasFlag(TableStyle.WhiteBackground) || style.HasFlag(TableStyle.VerticalLines)
            ? "divide-y divide-gray-200 dark:divide-gray-800"
            : "")
            + (style.HasFlag(TableStyle.VerticalLines)
            ? " bg-white"
            : "");

        public static string GetTableRowClass(TableStyle style, int i, bool selected, bool allowSelection) =>
            (allowSelection ? "cursor-pointer " : "") + 
                (selected ? "bg-indigo-100 dark:bg-blue-800" : (allowSelection ? "hover:bg-yellow-50 dark:hover:bg-blue-900 " : "") + (style.HasFlag(TableStyle.StripedRows)
                    ? (i % 2 == 0 ? "bg-white dark:bg-black" : "bg-gray-50 dark:bg-gray-800")
                    : "bg-white dark:bg-black")) + 
                 (style.HasFlag(TableStyle.VerticalLines) ? " divide-x divide-gray-200 dark:divide-gray-700" : "");
    }

    public static class Form
    {
        public const FormStyle DefaultFormStyle = FormStyle.SlideOver;
        public static string GetPanelClass(FormStyle style = FormStyle.SlideOver) => style == FormStyle.Card ? Card.PanelClass : SlideOver.PanelClass;
        public static string GetFormClass(FormStyle style = FormStyle.SlideOver) => style == FormStyle.Card ? Card.FormClass : SlideOver.FormClass;
        public static string GetHeadingClass(FormStyle style = FormStyle.SlideOver) => style == FormStyle.Card ? Card.HeadingClass : SlideOver.HeadingClass;
        public static string GetSubHeadingClass(FormStyle style = FormStyle.SlideOver) => style == FormStyle.Card ? Card.SubHeadingClass : SlideOver.SubHeadingClass;
        public const string ButtonsClass = "mt-4 px-4 py-3 bg-gray-50 dark:bg-gray-900 sm:px-6 flex flex-wrap justify-between";
        public const string LegendClass = "text-base font-medium text-gray-900 dark:text-gray-100 text-center mb-4";

        public static class SlideOver
        {
            public const string PanelClass = "pointer-events-auto w-screen xl:max-w-3xl md:max-w-xl max-w-lg";
            public const string FormClass = "flex h-full flex-col divide-y divide-gray-200 dark:divide-gray-700 shadow-xl bg-white dark:bg-black";
            public const string TitlebarClass = "bg-gray-50 dark:bg-gray-900 px-4 py-6 sm:px-6";
            public const string HeadingClass = "text-lg font-medium text-gray-900 dark:text-gray-100";
            public const string SubHeadingClass = "mt-1 text-sm text-gray-500 dark:text-gray-400";
            public const string CloseButtonClass = "rounded-md bg-gray-50 dark:bg-gray-900 text-gray-400 dark:text-gray-500 hover:text-gray-500 dark:hover:text-gray-400 focus:outline-none focus:ring-2 focus:ring-indigo-500 dark:ring-offset-black";

            public static DataTransition SlideOverTransition = new DataTransition(
                entering: new(@class: "transform transition ease-in-out duration-500 sm:duration-700", from: "translate-x-full", to: "translate-x-0"),
                leaving: new(@class: "transform transition ease-in-out duration-500 sm:duration-700", from: "translate-x-0", to: "translate-x-full"),
                visible: false);
        }

        public static class Card
        {
            public const string PanelClass = "shadow sm:overflow-hidden sm:rounded-md";
            public const string FormClass = "space-y-6 bg-white dark:bg-black py-6 px-4 sm:p-6";
            public const string HeadingClass = "text-lg font-medium leading-6 text-gray-900 dark:text-gray-100";
            public const string SubHeadingClass = "mt-1 text-sm text-gray-500 dark:text-gray-400";
        }
    }

    public static class Modal
    {
        public const string SizeClass = "sm:max-w-prose lg:max-w-screen-md xl:max-w-screen-lg 2xl:max-w-screen-xl sm:w-full";
    }

    public static class SlideOver
    {
        public const string SlideOverClass = "relative z-10";
        public const string DialogClass = "pointer-events-none fixed inset-y-0 right-0 flex max-w-full pl-10 sm:pl-16";
        public const string PanelClass = "pointer-events-auto w-screen xl:max-w-3xl md:max-w-xl max-w-lg";
        public const string FormClass = "flex h-full flex-col divide-y divide-gray-200 dark:divide-gray-700 bg-white dark:bg-black shadow-xl";
        public const string TitlebarClass = "bg-gray-50 dark:bg-gray-900 p-3 sm:p-6";
        public const string HeadingClass = "text-lg font-medium text-gray-900 dark:text-gray-50";
        public const string CloseButtonClass = "rounded-md bg-gray-50 dark:bg-black text-gray-400 dark:text-gray-500 hover:text-gray-500 dark:hover:text-gray-400 focus:outline-none focus:ring-2 focus:ring-indigo-500 dark:ring-offset-black";

        public const string TransitionClass = "transform transition ease-in-out duration-500 sm:duration-700";
    }

    public static class HtmlFormat
    {
        public const string Class = "prose html-format";
    }

    public static class PreviewFormat
    {
        public const string Class = "flex items-center";
        public const string IconClass = "w-6 h-6";
        public const string IconRoundedClass = "w-8 h-8 rounded-full";
        public const string ValueIconClass = "w-5 h-5 mr-1";
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

/* CSS Classes for Generic Components */

public static class TextInput
{
    public static string ErrorClasses { get; set; } = "mt-2 text-sm text-red-500";
    public static string LabelClasses { get; set; } = "block text-sm font-medium text-gray-700 dark:text-gray-300";
    public static string InputBaseClasses { get; set; } = "block w-full sm:text-sm rounded-md dark:text-white dark:bg-gray-900 disabled:bg-gray-100 dark:disabled:bg-gray-800 disabled:shadow-none";
    public static string InputValidClasses { get; set; } = "shadow-sm focus:ring-indigo-500 focus:border-indigo-500 border-gray-300 dark:border-gray-600";
    public static string InputInvalidClasses { get; set; } = "pr-10 border-red-300 text-red-900 placeholder-red-300 focus:outline-none focus:ring-red-500 focus:border-red-500";
    public static string InputClasses { get; set; } = InputBaseClasses + " " + InputValidClasses;
}
public static class DateTimeInput
{
    public static string ErrorClasses { get; set; } = TextInput.ErrorClasses;
    public static string LabelClasses { get; set; } = TextInput.LabelClasses;
    public static string InputBaseClasses { get; set; } = TextInput.InputBaseClasses;
    public static string InputValidClasses { get; set; } = TextInput.InputValidClasses;
    public static string InputInvalidClasses { get; set; } = TextInput.InputInvalidClasses;
    public static string InputClasses { get; set; } = InputBaseClasses + " " + InputValidClasses;
}

public static class TextAreaInput
{
    public static string ErrorClasses { get; set; } = TextInput.ErrorClasses;
    public static string LabelClasses { get; set; } = TextInput.LabelClasses;
    public static string InputBaseClasses { get; set; } = "shadow-sm block w-full sm:text-sm rounded-md dark:text-white dark:bg-gray-900 disabled:bg-gray-100 dark:disabled:bg-gray-800 disabled:shadow-none";
    public static string InputValidClasses { get; set; } = "text-gray-900 focus:ring-indigo-500 focus:border-indigo-500 border-gray-300 dark:border-gray-600";
    public static string InputInvalidClasses { get; set; } = "text-red-900 focus:ring-red-500 focus:border-red-500 border-red-300";
    public static string InputClasses { get; set; } = InputBaseClasses + " " + InputValidClasses;
}

public static class SelectInput
{
    public static string ErrorClasses { get; set; } = TextInput.ErrorClasses;
    public static string LabelClasses { get; set; } = TextInput.LabelClasses;
    public static string InputBaseClasses { get; set; } = "mt-1 block w-full pl-3 pr-10 py-2 text-base focus:outline-none border-gray-300 sm:text-sm rounded-md dark:text-white dark:bg-gray-900 dark:border-gray-600 disabled:bg-gray-100 dark:disabled:bg-gray-800 disabled:shadow-none";
    public static string InputValidClasses { get; set; } = "text-gray-900 focus:ring-indigo-500 focus:border-indigo-500";
    public static string InputInvalidClasses { get; set; } = "text-red-900 focus:ring-red-500 focus:border-red-500";
    public static string InputClasses { get; set; } = InputBaseClasses + " " + InputValidClasses;
}


public static class CheckboxInput
{
    public static string ErrorClasses { get; set; } = "text-gray-500";
    public static string LabelClasses { get; set; } = "font-medium text-gray-700 dark:text-gray-300";
    public static string InputClasses { get; set; } = "focus:ring-indigo-500 h-4 w-4 text-indigo-600 rounded border-gray-300 dark:border-gray-600 dark:bg-gray-800 dark:ring-offset-black";                                                                
}
