using System.Reflection;
using System.Runtime.Serialization;

[assembly: AssemblyVersion("1.0.0.0")]

//Default DataContract namespace instead of tempuri.org
//Note: doesn't work for ilmerged assemblies
[assembly: ContractNamespace("http://schemas.servicestack.net/types", ClrNamespace = "ServiceStack.WebHost.Endpoints.Tests.Support.Operations")]
[assembly: ContractNamespace("http://schemas.servicestack.net/types", ClrNamespace = "ServiceStack.WebHost.Endpoints.Tests.IntegrationTests")]
[assembly: ContractNamespace("http://schemas.servicestack.net/types", ClrNamespace = "ServiceStack.WebHost.Endpoints.Tests.Support.Host")]
[assembly: ContractNamespace("http://schemas.servicestack.net/types", ClrNamespace = "ServiceStack.WebHost.Endpoints.Tests")]

