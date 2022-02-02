namespace ServiceStack.Web
{
    public interface ICookies
    {
        /// <summary>
        /// Adds an expired Set-Cookie instruction for clients to delete this Cookie
        /// </summary>
        void DeleteCookie(string cookieName);

        /// <summary>
        /// Adds a new Set-Cookie instruction for ss-pid
        /// </summary>
        void AddPermanentCookie(string cookieName, string cookieValue, bool? secureOnly = null);

        /// <summary>
        /// Adds a new Set-Cookie instruction for ss-id
        /// </summary>
        void AddSessionCookie(string cookieName, string cookieValue, bool? secureOnly = null);
    }
}