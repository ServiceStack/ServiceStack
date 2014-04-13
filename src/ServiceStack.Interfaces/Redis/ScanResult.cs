using System.Collections.Generic;

namespace ServiceStack.Redis
{
    public class ScanResult
    {
        public ulong Cursor { get; set; }
        public List<string> Results { get; set; }
    }
}