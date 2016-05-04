using System.ComponentModel;

namespace Funq
{
    /// <summary>
    /// Funqlets are a set of components provided as a package 
    /// to an existing container (like a module).
    /// </summary>
    public interface IFunqlet
    {
        /// <summary>
        /// Configure the given container with the 
        /// registrations provided by the funqlet.
        /// </summary>
        /// <param name="container">Container to register.</param>
        void Configure(Container container);
    }

    // Interface definition provided for compatibility with 
    // p&p ContainerModel.

    /// <summary>
    /// Interface used by plugins to contribute registrations 
    /// to an existing container.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public interface IContainerModule : IFunqlet
    {
    }
}
