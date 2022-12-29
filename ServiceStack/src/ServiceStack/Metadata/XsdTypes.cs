using System.Collections.Generic;

namespace ServiceStack.Metadata
{
    internal static class XsdTypes
    {
        public static IDictionary<int, string> Xsds { get; private set; }

        static XsdTypes()
        {
            Xsds = new Dictionary<int, string> 
            {
                {1, "Service Types"},
                {0, "Wcf Data Types"},
                {2, "Wcf Collection Types"},
            };
        }
    }
}