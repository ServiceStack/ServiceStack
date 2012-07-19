using System;

namespace ServiceStack.Razor.Compilation
{
	/// <summary>
    /// Defines the required contract for implementing a compiler service.
    /// </summary>
    public interface ICompilerService
    {
        #region Methods
        /// <summary>
        /// Builds a type name for the specified template type and model type.
        /// </summary>
        /// <param name="templateType">The template type.</param>
        /// <param name="modelType">The model type.</param>
        /// <returns>The string type name (including namespace).</returns>
        string BuildTypeName(Type templateType, Type modelType);

        /// <summary>
        /// Compiles the type defined in the specified type context.
        /// </summary>
        /// <param name="context">The type context which defines the type to compile.</param>
        /// <returns>The compiled type.</returns>
        Type CompileType(TypeContext context);
        #endregion
    }
}