using System;
using System.Collections.Generic;
using System.Collections.Specialized;

namespace ServiceStack.Desktop
{
    public class DesktopConfig
    {
        public static DesktopConfig Instance { get; } = new DesktopConfig();
        
        public List<ProxyConfig> ProxyConfigs { get; set; } = new List<ProxyConfig>();
    }

    public class ProxyConfig
    {
        public string Scheme { get; set; }
        public string TargetScheme { get; set; }
        public string Domain { get; set; }
        public bool AllowCors { get; set; }
        public List<string> IgnoreHeaders { get; set; } = new List<string>();
        public Dictionary<string,string> AddHeaders { get; set; } = new Dictionary<string, string>();
        public Action<NameValueCollection> OnResponseHeaders { get; set; }
    }
}