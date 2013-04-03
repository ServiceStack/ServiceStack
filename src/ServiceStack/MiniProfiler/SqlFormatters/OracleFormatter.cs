using System;

namespace ServiceStack.MiniProfiler.SqlFormatters
{
    /// <summary>
    /// NOT IMPLEMENTED - will format statements with paramters in an Oracle friendly way
    /// </summary>
    public class OracleFormatter : ISqlFormatter
    {
        /// <summary>
        /// Does NOTHING, implement me!
        /// </summary>
        public string FormatSql(SqlTiming timing)
        {
            // It would be nice to have an oracle formatter, if anyone feel up to the challange a patch would be awesome
            throw new NotImplementedException();
        }
    }
}
