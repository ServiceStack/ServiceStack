using System;
using System.Net;

namespace ServiceStack.ServiceHost
{
	public interface ICookies
	{
		void DeleteCookie(string cookieName);
		void AddCookie(Cookie cookie);
		void AddPermanentCookie(string cookieName, string cookieValue);
		void AddSessionCookie(string cookieName, string cookieValue);
	}
}