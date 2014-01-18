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
[assembly: AssemblyCopyright("Copyright (c) Service Stack LLC 2013")]
[assembly: AssemblyTrademark("Service Stack")]
[assembly: AssemblyCulture("")]

//Keep constant to prevent breaking signed-builds (build.proj on replaces 4 digits, e.g x.x.x.x)
[assembly: AssemblyVersion("4.0.0")]
[assembly: AssemblyFileVersion("4.0.0.0")]

#if !(PCL || SL5)
// The following GUID is for the ID of the typelib if this project is exposed to COM
[assembly: Guid("d13ebd2a-6589-453d-bf31-4c744a59e993")]
#endif

[assembly: ContractNamespace("http://schemas.servicestack.net/types", 
	ClrNamespace = "ServiceStack")]
