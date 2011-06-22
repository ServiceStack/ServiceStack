namespace RazorEngine
{
    using Microsoft.CSharp.RuntimeBinder;

    /// <summary>
    /// Defines an internal stub type that enforces that the Microsoft.CSharp assembly is
    /// referenced in the dynamically compiled template assemblies.
    /// </summary>
    internal class MicrosoftCSharpReferenceStub : RuntimeBinderException { }
}