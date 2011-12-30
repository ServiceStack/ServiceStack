using System;
using System.Linq;
using System.Web.Razor.Parser;
using Microsoft.CSharp;

namespace ServiceStack.RazorEngine.Compilation.CSharp
{
	/// <summary>
    /// Defines a direct compiler service for the C# syntax.
    /// </summary>
    public class CSharpDirectCompilerService : DirectCompilerServiceBase
    {

        #region Constructor
        /// <summary>
        /// Initialises a new instance of <see cref="CSharpDirectCompilerService"/>.
        /// </summary>
        /// <param name="strictMode">Specifies whether the strict mode parsing is enabled.</param>
        /// <param name="markupParser">The markup parser to use.</param>
        public CSharpDirectCompilerService(bool strictMode = true, MarkupParser markupParser = null)
            : base(
                new CSharpRazorCodeLanguage(strictMode), 
                new CSharpCodeProvider(),
                markupParser) { }
        #endregion

        #region Methods
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
        #endregion
    }
}