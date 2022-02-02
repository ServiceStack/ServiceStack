using System;

namespace ServiceStack.DataAnnotations
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    public class SqlServerFileTableAttribute : AttributeBase
    {
        public SqlServerFileTableAttribute() { }

        public SqlServerFileTableAttribute(string directory, string collateFileName = null)
        {
            FileTableDirectory = directory;
            FileTableCollateFileName = collateFileName;
        }

        public string FileTableDirectory { get; internal set; }

        public string FileTableCollateFileName { get; internal set; }
    }
}