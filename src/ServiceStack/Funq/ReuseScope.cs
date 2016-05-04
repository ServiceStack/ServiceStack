
namespace Funq
{
    /// <summary>
    /// Determines visibility and reuse of instances provided by the container.
    /// </summary>
    public enum ReuseScope
    {
        /// <summary>
        /// Instances are reused within a container hierarchy. Instances 
        /// are created (if necessary) in the container where the registration
        /// was performed, and are reused by all descendent containers.
        /// </summary>
        Hierarchy,
        /// <summary>
        /// Instances are reused only at the given container. Descendent 
        /// containers do not reuse parent container instances and get  
        /// a new instance at their level.
        /// </summary>
        Container,
        /// <summary>
        /// Each request to resolve the dependency will result in a new 
        /// instance being returned.
        /// </summary>
        None,
        /// <summary>
        /// Instaces are reused within the given request
        /// </summary>
        Request,
        /// <summary>
        /// Default scope, which equals <see cref="Hierarchy"/>.
        /// </summary>
        Default = Hierarchy,
    }
}
