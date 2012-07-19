using System;

namespace ServiceStack.Razor.Templating
{
	/// <summary>
    /// Defines the required contract for implementing an object activator.
    /// </summary>
    public interface IActivator
    {
        #region Methods
        /// <summary>
        /// Creates an instance of the specified type.
        /// </summary>
        /// <param name="type">The type to create an instance.</param>
        /// <returns>The <see cref="ITemplate"/> instance.</returns>
        ITemplate CreateInstance(Type type);
        #endregion
    }
}