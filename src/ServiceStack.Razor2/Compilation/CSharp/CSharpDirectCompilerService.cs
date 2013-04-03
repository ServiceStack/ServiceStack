using System;
using System.Linq;
using System.Web.Razor;
using System.Web.Razor.Parser;
using Microsoft.CSharp;

namespace ServiceStack.Razor.Compilation.CSharp
{
	/// <summary>
    /// Defines a direct compiler service for the C# syntax.
    /// </summary>
    public class CSharpDirectCompilerService : CompilerServiceBase
    {
        public CSharpDirectCompilerService(bool strictMode = true)
            : base(
                new CSharpCodeProvider(),
                new CSharpRazorCodeLanguage()) { }

        /// <summary>
        /// Builds a type name for the specified generic type.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <param name="isDynamic">Specifies whether the type is dynamic.</param>
        /// <returns>
        /// The string typename (including namespace and generic type parameters).
        /// </returns>
        public override string BuildTypeNameInternal(Type type, bool isDynamic)
        {
            if (!type.IsGenericType)
                return type.FullName;

            return type.Namespace
                   + "."
                   + type.Name.Substring(0, type.Name.IndexOf('`'))
                   + "<"
                   + (isDynamic ? "dynamic" : string.Join(", ", type.GetGenericArguments().Select(t => BuildTypeNameInternal(t, CompilerServices.IsDynamicType(t)))))
                   + ">";
        }
    }
}