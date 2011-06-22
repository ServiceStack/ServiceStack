namespace RazorEngine.Compilation
{
    using System;

    /// <summary>
    /// Defines an attribute that marks the presence of a dynamic model in a template.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    public sealed class HasDynamicModelAttribute : Attribute
    {
    }
}