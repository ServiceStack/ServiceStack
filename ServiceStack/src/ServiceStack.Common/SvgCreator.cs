#nullable enable
using System;

namespace ServiceStack;

public static class SvgCreator
{
    public static string[] DarkColors { get; set; } =
    [
        "#334155",
        "#374151",
        "#44403c",
        "#b91c1c",
        "#c2410c",
        "#b45309",
        "#4d7c0f",
        "#15803d",
        "#047857",
        "#0f766e",
        "#0e7490",
        "#0369a1",
        "#1d4ed8",
        "#4338ca",
        "#6d28d9",
        "#7e22ce",
        "#a21caf",
        "#be185d",
        "#be123c",
        //
        "#824d26",
        "#865081",
        "#0c7047",
        "#0064a7",
        "#8220d0",
        "#009645",
        "#ab00f0",
        "#9a3c69",
        "#227632",
        "#4b40bd",
        "#ad3721",
        "#6710f2",
        "#1a658a",
        "#078e57",
        "#2721e1",
        "#168407",
        "#019454",
        "#967312",
        "#6629d8",
        "#108546",
        "#9a2aa1",
        "#3d7813",
        "#257124",
        "#6f14ed",
        "#1f781d",
        "#a29906",
    ];

    public static string GetDarkColor(int index) => DarkColors[index % DarkColors.Length];

    public static string CreateSvg(char letter, string? bgColor = null, string? textColor = null)
    {
        #if NET6_0_OR_GREATER
        bgColor ??= GetDarkColor(Random.Shared.Next(DarkColors.Length));
        #else
        bgColor ??= GetDarkColor(new Random().Next(DarkColors.Length));
        #endif
        textColor ??= "#FFF";

        var svg = $@"<svg xmlns=""http://www.w3.org/2000/svg"" xmlns:xlink=""http://www.w3.org/1999/xlink"" version=""1.1"" style=""isolation:isolate"" viewBox=""0 0 32 32"">
            <path d=""M0 0h32v32H0V0z"" fill=""{bgColor}"" />
            <text font-family=""Helvetica"" font-size=""20px"" x=""50%"" y=""48%"" dy=""0em"" fill=""{textColor}"" alignment-baseline=""central"" text-anchor=""middle"">{letter}</text>
        </svg>";
        return svg;
    }

    public static string CreateSvgDataUri(char letter, string? bgColor = null, string? textColor = null) =>
        ToDataUri(CreateSvg(letter, bgColor, textColor));
    
    public static string Decode(string dataUri)
    {
        return dataUri                
            .Replace("'","\"")
            .Replace("%25","%")
            .Replace("%23","#")
            .Replace("%3C","<")
            .Replace("%3E",">")
            .Replace("%3F","?")
            .Replace("%5B","[")
            .Replace("%5C","\\")
            .Replace("%5D","]")
            .Replace("%5E","^")
            .Replace("%60","`")
            .Replace("%7B","{")
            .Replace("%7C","|")
            .Replace("%7D","}");
    }

    public static string DataUriToSvg(string dataUri) => Decode(dataUri.RightPart(','));

    public static char GradeLetter(int votes) => votes >= 9
        ? 'A'
        : votes >= 6
            ? 'B'
            : votes >= 3
                ? 'C'
                : votes >= 2
                    ? 'D'
                    : 'F';
    
    public static string GradeBgColor(char grade) => grade switch
    {
        'A' => "#16a34a",
        'B' => "#2563eb",
        'C' => "#4b5563",
        'D' => "#dc2626",
        _ => "#7f1d1d"
    };
    
    public static string CreateGradeSvg(char grade) => CreateSvg(grade, GradeBgColor(grade), "#fff");
    
    public static string CreateGradeDataUri(char grade) => ToDataUri(CreateGradeSvg(grade));

    public static string? Encode(string svg)
    {
        if (string.IsNullOrEmpty(svg))
            return null;

        //['%','#','<','>','?','[','\\',']','^','`','{','|','}'].map(x => `.Replace("${x}","` + encodeURIComponent(x) + `")`).join('')
        return svg.Replace("\r", " ").Replace("\n", "")                
            .Replace("\"","'")
            .Replace("%","%25")
            .Replace("#","%23")
            .Replace("<","%3C")
            .Replace(">","%3E")
            .Replace("?","%3F")
            .Replace("[","%5B")
            .Replace("\\","%5C")
            .Replace("]","%5D")
            .Replace("^","%5E")
            .Replace("`","%60")
            .Replace("{","%7B")
            .Replace("|","%7C")
            .Replace("}","%7D");
    }

    public static string ToDataUri(string svg) => "data:image/svg+xml," + Encode(svg);
}