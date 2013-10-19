using System.Net;

namespace ServiceStack.Web
{
    public interface ICookies
    {
        void DeleteCookie(string cookieName);
        void AddPermanentCookie(string cookieName, string cookieValue, bool? secureOnly = null);
        void AddSessionCookie(string cookieName, string cookieValue, bool? secureOnly = null);
    }
}