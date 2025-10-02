namespace ServiceStack;

public class MySqlUtils
{
    public static string SqlDateFormat(string quotedColumn, string format)
    {
        return $"DATE_FORMAT({quotedColumn}, '{format}')";
    }
}
