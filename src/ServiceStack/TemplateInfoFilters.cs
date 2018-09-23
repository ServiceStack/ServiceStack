using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using ServiceStack.Auth;
using ServiceStack.Host;
using ServiceStack.Templates;
using ServiceStack.Text;
using ServiceStack.Web;

namespace ServiceStack
{
    // ReSharper disable InconsistentNaming
    
    public class TemplateInfoFilters : TemplateFilter
    {
        public const string ServiceStackLogoSvg = "<svg xmlns=\"http://www.w3.org/2000/svg\" viewBox=\"0 0 496 512\"><path fill=\"currentColor\" d=\"M88 216c81.7 10.2 273.7 102.3 304 232H0c99.5-8.1 184.5-137 88-232zm32-152c32.3 35.6 47.7 83.9 46.4 133.6C249.3 231.3 373.7 321.3 400 448h96C455.3 231.9 222.8 79.5 120 64z\"></path></svg>";

        public const string ServiceStackLogoDataUri = "\"data:image/svg+xml;charset=UTF-8,%3csvg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 496 512'%3e%3cpath fill='currentColor' d='M88 216c81.7 10.2 273.7 102.3 304 232H0c99.5-8.1 184.5-137 88-232zm32-152c32.3 35.6 47.7 83.9 46.4 133.6C249.3 231.3 373.7 321.3 400 448h96C455.3 231.9 222.8 79.5 120 64z'%3e%3c/path%3e%3c/svg%3e\"";

        public IRawString serviceStackLogoSvg(string color) => ServiceStackLogoSvg.Replace("currentColor", color).ToRawString();
        public IRawString serviceStackLogoSvg() => ServiceStackLogoSvg.ToRawString();
        public IRawString serviceStackLogoDataUri(string color) => ServiceStackLogoDataUri.Replace("currentColor", color).ToRawString();
        public IRawString serviceStackLogoDataUri() => ServiceStackLogoDataUri.ToRawString();
        public IRawString serviceStackLogoDataUriLight() => serviceStackLogoDataUri("%23dddddd");
        
        public string envVariable(string variable) => Environment.GetEnvironmentVariable(variable);
        public string envExpandVariables(string name) => Environment.ExpandEnvironmentVariables(name);
        public string envStackTrace() => Environment.StackTrace;
        public int envProcessorCount() => Environment.ProcessorCount;
        public int envTickCount() => Environment.TickCount;

        public string envServerUserAgent() => Env.ServerUserAgent;
        public decimal envServiceStackVersion() => Env.ServiceStackVersion;

        public bool envIsMono() => Env.IsMono;
        public bool envIsAndroid() => Env.IsAndroid;
        public bool envIsIOS() => Env.IsIOS;
        public string licensedFeatures() => LicenseUtils.ActivatedLicenseFeatures() == LicenseFeature.All ? "All" : LicenseUtils.ActivatedLicenseFeatures().ToString();

        public string envCurrentDirectory() => Environment.CurrentDirectory;
        public bool envIsWindows() => Env.IsWindows;
        public bool envIsLinux() => Env.IsLinux;
        public bool envIsOSX() => Env.IsOSX;
        public IDictionary envVariables() => Environment.GetEnvironmentVariables();
        public OperatingSystem envOSVersion() => Environment.OSVersion;
        public string envCommandLine() => Environment.CommandLine;
        public string envMachineName() => Environment.MachineName;
        public string envSystemDirectory() => Environment.SystemDirectory;
        public string envUserDomainName() => Environment.UserDomainName;
        public string envUserName() => Environment.UserName;
        public bool envIs64BitOperatingSystem() => Environment.Is64BitOperatingSystem;
        public bool envIs64BitProcess() => Environment.Is64BitProcess;
        public Version envVersion() => Environment.Version;
        public string[] envLogicalDrives() => Environment.GetLogicalDrives();

#if NETSTANDARD2_0
        public string envFrameworkDescription() => System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription;
        public string envOSDescription() => System.Runtime.InteropServices.RuntimeInformation.OSDescription;
        public System.Runtime.InteropServices.Architecture envOSArchitecture() => System.Runtime.InteropServices.RuntimeInformation.OSArchitecture;
#endif

        public List<IPAddress> networkIpv4Addresses() => IPAddressExtensions.GetAllNetworkInterfaceIpv4Addresses().Keys.ToList();
        public List<IPAddress> networkIpv6Addresses() => IPAddressExtensions.GetAllNetworkInterfaceIpv6Addresses();

        private IHttpRequest req(TemplateScopeContext scope) => scope.GetValue("Request") as IHttpRequest;

        public IAuthSession userSession(TemplateScopeContext scope) => req(scope).GetSession();
        public string userSessionId(TemplateScopeContext scope) => req(scope).GetSessionId();
        public string userPermanentSessionId(TemplateScopeContext scope) => req(scope).GetPermanentSessionId();
        public HashSet<string> userSessionOptions(TemplateScopeContext scope) => req(scope).GetSessionOptions();
        public bool userHasRole(TemplateScopeContext scope, string role) => 
            userSession(scope)?.HasRole(role, HostContext.AppHost.GetAuthRepository(req(scope))) == true;
        public bool userHasPermission(TemplateScopeContext scope, string permission) => 
            userSession(scope)?.HasPermission(permission, HostContext.AppHost.GetAuthRepository(req(scope))) == true;

        public string userId(TemplateScopeContext scope) => req(scope).GetSession()?.UserAuthId;
        public string userName(TemplateScopeContext scope) => req(scope).GetSession()?.UserAuthName ?? req(scope).GetSession()?.UserName;
        public string userEmail(TemplateScopeContext scope) => req(scope).GetSession()?.Email;

        public string hostServiceName(TemplateScopeContext scope) => HostContext.AppHost.ServiceName;
        public HostConfig hostConfig(TemplateScopeContext scope) => HostContext.Config;
        
        public HashSet<Type> metaAllDtos() => HostContext.Metadata.GetAllDtos();
        public List<string> metaAllDtoNames() => HostContext.Metadata.GetOperationDtos().Map(x => x.Name);
        public IEnumerable<Operation> metaAllOperations() => HostContext.Metadata.Operations;
        public List<string> metaAllOperationNames() => HostContext.Metadata.GetAllOperationNames();
        public List<Type> metaAllOperationTypes() => HostContext.Metadata.GetAllOperationTypes();
        public Operation metaOperation(string name) => HostContext.Metadata.GetOperation(HostContext.Metadata.GetOperationType(name));

        public List<IPlugin> plugins() => HostContext.AppHost.Plugins;
    }
}