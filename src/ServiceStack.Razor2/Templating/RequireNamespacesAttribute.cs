using System;
using System.Collections.Generic;

namespace ServiceStack.Razor.Templating
{
	/// <summary>
    /// Allows base templates to define required namespaces that will be automatically be
    /// added to generated templates.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    public class RequireNamespacesAttribute : Attribute
    {
        /// <summary>
        /// Initialises a new instance of <see cref="RequireNamespacesAttribute"/>
        /// </summary>
        /// <param name="namespaces">The set of namespaces to include.</param>
        public RequireNamespacesAttribute(params string[] namespaces)
        {
            Namespaces = new HashSet<string>();
            if (namespaces != null)
            {
                foreach (string ns in namespaces)
                    Namespaces.Add(ns);
            }
        }

        /// <summary>
        /// Gets the set of namespaces.
        /// </summary>
        public ISet<string> Namespaces { get; private set; }
    }
}