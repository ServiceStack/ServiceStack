namespace ServiceStack;

public class SqliteUtils
{
    public static string SqlDateFormat(string quotedColumn, string format)
    {
        return $"strftime('{format}',{quotedColumn})";
    }
}