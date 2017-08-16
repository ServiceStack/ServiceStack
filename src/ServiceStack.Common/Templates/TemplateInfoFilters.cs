using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using ServiceStack.Text;

namespace ServiceStack.Templates
{
    public class TemplateInfoFilters : TemplateFilter
    {

        public string envVariable(string variable) => Environment.GetEnvironmentVariable(variable);
        public string envExpandVariables(string name) => Environment.ExpandEnvironmentVariables(name);
        public string envStackTrace() => Environment.StackTrace;
        public int envProcessorCount(string variable) => Environment.ProcessorCount;
        public int envTickCount(string variable) => Environment.TickCount;

        public bool envIsAndroid() => Env.IsAndroid;
        public bool envIsMonoTouch() => Env.IsMonoTouch;
        public bool envIsMono() => Env.IsMono;
        public string envServerUserAgent() => Env.ServerUserAgent;
        public decimal envServiceStackVersion() => Env.ServiceStackVersion;

#if NET45
        public bool envIsWindows() => Env.IsWindows;
        public bool envIsLinux() => Env.IsLinux;
        public bool envIsOSX() => Env.IsOSX;

        public IDictionary envVariables() => Environment.GetEnvironmentVariables();
        public OperatingSystem envOSVersion() => Environment.OSVersion;
        public string envCommandLine() => Environment.CommandLine;
        public string envCurrentDirectory() => Environment.CurrentDirectory;
        public string envMachineName() => Environment.MachineName;
        public string envSystemDirectory() => Environment.SystemDirectory;
        public string envUserDomainName() => Environment.UserDomainName;
        public string envUserName() => Environment.UserName;
        public bool envIs64BitOperatingSystem() => Environment.Is64BitOperatingSystem;
        public bool envIs64BitProcess() => Environment.Is64BitProcess;
        public Version envVersion() => Environment.Version;
        public string[] envLogicalDrives() => Environment.GetLogicalDrives();
#elif NETSTANDARD1_3
        public bool envIsWindows() => System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows);
        public bool envIsLinux() => System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Linux);
        public bool envIsOSX() => System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.OSX);

        public string envFrameworkDescription() => System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription;
        public string envOSDescription() => System.Runtime.InteropServices.RuntimeInformation.OSDescription;
        public System.Runtime.InteropServices.Architecture envOSArchitecture() => System.Runtime.InteropServices.RuntimeInformation.OSArchitecture;
#endif


        public List<IPAddress> networkIpv4Addresses() => IPAddressExtensions.GetAllNetworkInterfaceIpv4Addresses().Keys.ToList();
        public List<IPAddress> networkIpv6Addresses() => IPAddressExtensions.GetAllNetworkInterfaceIpv6Addresses();
    }
}