using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;

// General Information about an assembly is controlled through the following 
// set of attributes. Change these attribute values to modify the information
// associated with an assembly.
[assembly: AssemblyTitle("ServiceStack.WebHost.Endpoints.Tests")]
[assembly: AssemblyDescription("")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany("")]
[assembly: AssemblyProduct("ServiceStack.WebHost.Endpoints.Tests")]
[assembly: AssemblyCopyright("Copyright (c)  2009")]
[assembly: AssemblyTrademark("Service Stack")]
[assembly: AssemblyCulture("")]

// Setting ComVisible to false makes the types in this assembly not visible 
// to COM components.  If you need to access a type in this assembly from 
// COM, set the ComVisible attribute to true on that type.
[assembly: ComVisible(false)]

// The following GUID is for the ID of the typelib if this project is exposed to COM
[assembly: Guid("a10fdf44-0168-49d4-9f25-0dfac96998ea")]

// Version information for an assembly consists of the following four values:
//
//      Major Version
//      Minor Version 
//      Build Number
//      Revision
//
// You can specify all the values or you can default the Build and Revision Numbers 
// by using the '*' as shown below:
// [assembly: AssemblyVersion("1.0.*")]
[assembly: AssemblyVersion("1.0.0.0")]
[assembly: AssemblyFileVersion("4.0.0.0")]	

//Default DataContract namespace instead of tempuri.org
//Note: doesn't work for ilmerged assemblies
[assembly: ContractNamespace("http://schemas.servicestack.net/types", ClrNamespace = "ServiceStack.WebHost.Endpoints.Tests.Support.Operations")]
[assembly: ContractNamespace("http://schemas.servicestack.net/types", ClrNamespace = "ServiceStack.WebHost.Endpoints.Tests.IntegrationTests")]
[assembly: ContractNamespace("http://schemas.servicestack.net/types", ClrNamespace = "ServiceStack.WebHost.Endpoints.Tests.Support.Host")]
[assembly: ContractNamespace("http://schemas.servicestack.net/types", ClrNamespace = "ServiceStack.WebHost.Endpoints.Tests")]

