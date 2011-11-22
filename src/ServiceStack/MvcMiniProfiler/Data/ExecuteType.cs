using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MvcMiniProfiler.Data
{
    /// <summary>
    /// Categories of sql statements.
    /// </summary>
    public enum ExecuteType : byte
    {
        /// <summary>
        /// Unknown
        /// </summary>
        None = 0,

        /// <summary>
        /// DML statements that alter database state, e.g. INSERT, UPDATE
        /// </summary>
        NonQuery = 1,

        /// <summary>
        /// Statements that return a single record
        /// </summary>
        Scalar = 2,

        /// <summary>
        /// Statements that iterate over a result set
        /// </summary>
        Reader = 3
    }
}
