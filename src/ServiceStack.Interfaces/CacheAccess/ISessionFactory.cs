#if !SILVERLIGHT && !MONOTOUCH && !XBOX
using ServiceStack.ServiceHost;

namespace ServiceStack.CacheAccess
{
	/// <summary>
	/// Retrieves a User Session
	/// </summary>
	public interface ISessionFactory
	{
		/// <summary>
		/// Gets the session for this request, creates one if it doesn't exist.
		/// </summary>
		/// <param name="httpReq"></param>
		/// <param name="httpRes"></param>
		/// <returns></returns>
		ISession GetOrCreateSession(IHttpRequest httpReq, IHttpResponse httpRes);

		/// <summary>
		/// Gets the session for this request, creates one if it doesn't exist.
		/// Only for ASP.NET apps. Uses the HttpContext.Current singleton.
		/// </summary>
		ISession GetOrCreateSession();
	}
}
#endif