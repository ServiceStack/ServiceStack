namespace ServiceStack.Configuration
{
    /// <summary>
    /// Allow delegation of dependencies to other IOC's
    /// </summary>
    public interface IContainerAdapter : IResolver
    {
        /// <summary>
        /// Resolve Constructor Dependency
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        T Resolve<T>();
    }
}