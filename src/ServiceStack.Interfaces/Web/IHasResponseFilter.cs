namespace ServiceStack.Web
{
    /// <summary>
    /// This interface can be implemented by an attribute
    /// which adds an response filter for the specific response DTO the attribute marked.
    /// </summary>
    public interface IHasResponseFilter
    {
        /// <summary>
        /// Order in which Response Filters are executed. 
        /// &lt;0 Executed before global response filters
        /// &gt;0 Executed after global response filters
        /// </summary>
        int Priority { get; }

        /// <summary>
        /// The response filter is executed after the service
        /// </summary>
        /// <param name="req">The http request wrapper</param>
        /// <param name="res">The http response wrapper</param>
        void ResponseFilter(IRequest req, IResponse res, object response);

        /// <summary>
        /// A new shallow copy of this filter is used on every request.
        /// </summary>
        /// <returns></returns>
        IHasResponseFilter Copy();
    }
}
