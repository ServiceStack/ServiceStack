using System.Threading.Tasks;

namespace ServiceStack.Web
{
	public interface IRequestFilterBase
	{
		/// <summary>
		/// Order in which Request Filters are executed. 
		/// &lt;0 Executed before global request filters
		/// &gt;0 Executed after global request filters
		/// </summary>
		int Priority { get; }

		/// <summary>
		/// A new shallow copy of this filter is used on every request.
		/// </summary>
		/// <returns></returns>
		IRequestFilterBase Copy();
	}
	
    /// <summary>
    /// This interface can be implemented by an attribute
    /// which adds an request filter for the specific request DTO the attribute marked.
    /// </summary>
    public interface IHasRequestFilter : IRequestFilterBase
    {
        /// <summary>
        /// The request filter is executed before the service.
        /// </summary>
        /// <param name="req">The http request wrapper</param>
        /// <param name="res">The http response wrapper</param>
        /// <param name="requestDto">The request DTO</param>
        void RequestFilter(IRequest req, IResponse res, object requestDto);
    }

	/// <summary>
	/// This interface can be implemented by an attribute
	/// which adds an request filter for the specific request DTO the attribute marked.
	/// </summary>
	public interface IHasRequestFilterAsync : IRequestFilterBase
	{
		/// <summary>
		/// The request filter is executed before the service.
		/// </summary>
		/// <param name="req">The http request wrapper</param>
		/// <param name="res">The http response wrapper</param>
		/// <param name="requestDto">The request DTO</param>
		Task RequestFilterAsync(IRequest req, IResponse res, object requestDto);
	}
}
