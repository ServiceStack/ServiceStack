namespace MvcMiniProfiler
{
    /// <summary>
    /// Takes a SqlTiming and returns a formatted SQL string, for parameter replacement, etc.
    /// </summary>
    public interface ISqlFormatter
    {
        /// <summary>
        /// Return SQL the way you want it to look on the in the trace. Usually used to format parameters 
        /// </summary>
        /// <param name="timing"></param>
        /// <returns>Formatted SQL</returns>
        string FormatSql(SqlTiming timing);
    }
}
