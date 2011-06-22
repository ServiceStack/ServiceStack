using System.Reflection;
using System.Runtime.InteropServices;

[assembly: AssemblyTitle("RazorEngine: Core")]
[assembly: AssemblyDescription("Provides templating services using the Razor parser and code generator.")]

[assembly: Guid("e38fdd39-0ae9-4c67-af72-79191d7a1f85")]

#if DEBUG
[assembly: AssemblyConfiguration("Debug")]
#else
[assembly: AssemblyConfiguration("Release")]
#endif

[assembly: AssemblyCompany("RazorEngine Project")]
[assembly: AssemblyProduct("RazorEngine")]
[assembly: AssemblyCopyright("Copyright © RazorEngine Project 2010")]

[assembly: ComVisible(false)]

[assembly: AssemblyVersion("2.1.*")]
[assembly: AssemblyFileVersion("2.1.0.0")]