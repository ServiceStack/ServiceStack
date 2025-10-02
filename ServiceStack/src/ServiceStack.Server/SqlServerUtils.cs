using System.Collections.Generic;

namespace ServiceStack;

public static class SqlServerUtils
{
    // strftime('%Y-%m-%d %H:%M:%S', 'now')
    public static Dictionary<string, string> DateFormatMap = new() {
        {"%Y", "yyyy"},
        {"%m", "MM"},
        {"%d", "dd"},
        {"%H", "HH"},
        {"%M", "mm"},
        {"%S", "ss"},
    };

    public static string SqlDateFormat(string quotedColumn, string format)
    {
        var fmt = format.IndexOf('\'') >= 0
            ? format.Replace("'", "")
            : format;
        foreach (var entry in DateFormatMap)
        {
            fmt = fmt.Replace(entry.Key, entry.Value);
        }
        return $"FORMAT({quotedColumn}, '{fmt}')";
    }
}