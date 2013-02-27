using System;
using System.Web;

namespace ServiceStack.ServiceInterface
{
    //adapted from  https://github.com/Haacked/CodeHaacks/blob/master/src/AspNetHaack/SuppressFormsAuthenticationRedirectModule.cs
    /// <summary>
    /// This class interecepts 401 requests and changes them to 402 errors.   When this happens the FormAuthentication module
    /// will no longer hijack it and redirect back to login because it is a 402 error, not a 401.
    /// When the request ends, this class sets the status code back to 401 and everything works as it should.
    /// 
    /// PathToSupress is the path inside your website where the above swap should happen.
    /// 
    /// If you can build for .net 4.5, you do not have to do this swap. You can take advantage of a new flag (SuppressFormsAuthenticationRedirect)
    /// that tells the FormAuthenticationModule to not redirect, which also means you will not need the EndRequest code.
    /// </summary>
    public class SuppressFormsAuthenticationRedirectModule : IHttpModule
    {

        public static string PathToSupress { get; set; }

        public virtual void Init(HttpApplication context)
        {
            PathToSupress = "/api";
            context.PostReleaseRequestState += OnPostReleaseRequestState;
            context.EndRequest += OnEndRequest;  //not needed if .net 4.5 
        }

        //not needed if .net 4.5 
        void OnEndRequest(object source, EventArgs e)
        {
            var context = (HttpApplication)source;
            if (context.Response.StatusCode == 402 && context.Request.Url.PathAndQuery.StartsWith(PathToSupress))
                context.Response.StatusCode = 401;
        }

        public void Dispose()
        {

        }

        
        private void OnPostReleaseRequestState(object source, EventArgs args)
        {
          //System.Web.Security.FormsAuthenticationModule  //swap error code to 402 ...then put it back on endrequest?
            var context = (HttpApplication)source;
            if (context.Response.StatusCode == 401 && context.Request.Url.PathAndQuery.StartsWith(PathToSupress))
                context.Response.StatusCode = 402;                              //.net 4.0 solution.
                //context.Response.SuppressFormsAuthenticationRedirect = true;  //.net 4.5 solution.
        }
    }
}
