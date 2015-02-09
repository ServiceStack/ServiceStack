using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;

// General Information about an assembly is controlled through the following 
// set of attributes. Change these attribute values to modify the information
// associated with an assembly.
[assembly: AssemblyTitle("ServiceStack")]
[assembly: AssemblyDescription("")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany("")]
[assembly: AssemblyProduct("ServiceStack")]
[assembly: AssemblyCopyright("Copyright (c) ServiceStack 2014")]
[assembly: AssemblyTrademark("Service Stack")]
[assembly: AssemblyCulture("")]

// Setting ComVisible to false makes the types in this assembly not visible 
// to COM components.  If you need to access a type in this assembly from 
// COM, set the ComVisible attribute to true on that type.
[assembly: ComVisible(false)]

// The following GUID is for the ID of the typelib if this project is exposed to COM
[assembly: Guid("b1eeca45-c9f8-457d-a6ee-98ac3b071639")]

[assembly: AssemblyVersion("4.0.0.0")]
[assembly: AssemblyFileVersion("4.0.0.0")]

//Default DataContract namespace instead of tempuri.org
[assembly: ContractNamespace("http://schemas.servicestack.net/types",
    ClrNamespace = "ServiceStack.ServiceInterface")]
[assembly: ContractNamespace("http://schemas.servicestack.net/types",
    ClrNamespace = "ServiceStack.ServiceInterface.Auth")]
[assembly: ContractNamespace("http://schemas.servicestack.net/types",
    ClrNamespace = "ServiceStack.ServiceInterface.Admin")]
