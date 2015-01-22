using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;

// General Information about an assembly is controlled through the following 
// set of attributes. Change these attribute values to modify the information
// associated with an assembly.
[assembly: AssemblyTitle("ServiceStack")]
[assembly: AssemblyDescription("")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany("Service Stack LLC")]
[assembly: AssemblyProduct("ServiceStack")]
[assembly: AssemblyCopyright("Copyright (c) ServiceStack 2015")]
[assembly: AssemblyTrademark("Service Stack")]
[assembly: AssemblyCulture("")]

//Keep constant to prevent breaking signed-builds (build.proj on replaces 4 digits, e.g x.x.x.x)
[assembly: AssemblyVersion("4.0.0")]
[assembly: AssemblyFileVersion("4.0.0.0")]

[assembly: ContractNamespace("http://schemas.servicestack.net/types", 
	ClrNamespace = "ServiceStack")]
