using ServiceStack.Web;

namespace ServiceStack.Caching
{
    /// <summary>
    /// Retrieves a User Session
    /// </summary>
    public interface ISessionFactory
    {
        /// <summary>
        /// Gets the Session Bag for this request, creates one if it doesn't exist.
        /// </summary>
        /// <param name="httpReq"></param>
        /// <param name="httpRes"></param>
        /// <returns></returns>
        ISession GetOrCreateSession(IRequest httpReq, IResponse httpRes);

        /// <summary>
        /// Gets the Session Bag for this request, creates one if it doesn't exist.
        /// Only for ASP.NET apps. Uses the HttpContext.Current singleton.
        /// </summary>
        ISession GetOrCreateSession();

        /// <summary>
        /// Create a Session Bag using a custom sessionId
        /// </summary>
        /// <param name="sessionId"></param>
        /// <returns></returns>
        ISession CreateSession(string sessionId);
    }
}