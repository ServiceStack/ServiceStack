//Copyright (c) Service Stack LLC. All Rights Reserved.
//License: https://raw.github.com/ServiceStack/ServiceStack/master/license.txt

#if SL5
using System;
using System.Collections.Specialized;
using System.Net;
using System.Threading;
using System.Web;
using ServiceStack.Pcl;
using ServiceStack.Text;
using ServiceStack.Web;

namespace ServiceStack
{
    public class Sl5PclExportClient : PclExportClient
    {
        public static Sl5PclExportClient Provider = new Sl5PclExportClient();

        public static PclExportClient Configure()
        {
            Configure(Provider ?? (Provider = new Sl5PclExportClient())); 
            Sl5PclExport.Configure();
            return Provider;
        }

        public override INameValueCollection NewNameValueCollection()
        {
            return new NameValueCollectionWrapper(new NameValueCollection());
        }

        public override INameValueCollection ParseQueryString(string query)
        {
            return HttpUtility.ParseQueryString(query).InWrapper();
        }

        public override void RunOnUiThread(Action fn)
        {
            System.Windows.Deployment.Current.Dispatcher.BeginInvoke(fn);
        }

        public override void SetCookieContainer(HttpWebRequest webRequest, ServiceClientBase client)
        {
            if (!client.EmulateHttpViaPost)
            {
                if (client.ShareCookiesWithBrowser)
                {
                    if (client.CookieContainer == null)
                        client.CookieContainer = new CookieContainer();
                    client.CookieContainer.SetCookies(webRequest.RequestUri, System.Windows.Browser.HtmlPage.Document.Cookies);
                }
            }

            try
            {
                webRequest.CookieContainer = client.CookieContainer;
            }
            catch (NotImplementedException) {}
        }

        public override void SetCookieContainer(HttpWebRequest webRequest, AsyncServiceClient client)
        {
            if (!client.EmulateHttpViaPost)
            {
                if (client.ShareCookiesWithBrowser)
                {
                    if (client.CookieContainer == null)
                        client.CookieContainer = new CookieContainer();
                    client.CookieContainer.SetCookies(webRequest.RequestUri, System.Windows.Browser.HtmlPage.Document.Cookies);
                }
            }

            try
            {
                webRequest.CookieContainer = client.CookieContainer;
            }
            catch (NotImplementedException) {}
        }

        public override void SynchronizeCookies(AsyncServiceClient client)
        {
            if (client.StoreCookies && client.ShareCookiesWithBrowser && !client.EmulateHttpViaPost)
            {
                // browser cookies must be set on the ui thread
                System.Windows.Deployment.Current.Dispatcher.BeginInvoke(() =>
                {
                    var cookieHeader = client.CookieContainer.GetCookieHeader(new Uri(client.BaseUri));
                    System.Windows.Browser.HtmlPage.Document.Cookies = cookieHeader;
                });
            }
        }
    }


    public class AsyncTimer : ITimer
    {
        public System.Threading.Timer Timer;

        public AsyncTimer(System.Threading.Timer timer)
        {
            Timer = timer;
        }

        public void Cancel()
        {
            this.Timer.Change(Timeout.Infinite, Timeout.Infinite);
            this.Dispose();
        }

        public void Dispose()
        {
            if (Timer == null) return;

            this.Timer.Dispose();
            this.Timer = null;
        }
    }
}
#endif