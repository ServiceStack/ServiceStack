// Copyright (c) Service Stack LLC. All Rights Reserved.
// License: https://raw.github.com/ServiceStack/ServiceStack/master/license.txt

/*
 * Keep platform specific stuff here
 */

using System;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

#if NETFX_CORE
using Windows.System.Threading;
#endif

namespace ServiceStack
{
    public interface ITimer : IDisposable
    {
        void Cancel();
    }

    internal static class AsyncUtils
    {
        public static Exception CreateTimeoutException(this Exception ex, string errorMsg)
        {
#if SILVERLIGHT
            return new WebException("The request timed out", ex, WebExceptionStatus.RequestCanceled, null);
#else
            return new WebException("The request timed out", ex, WebExceptionStatus.Timeout, null);
#endif
        }

        internal static ITimer CreateTimer<TResponse>(this AsyncState<TResponse> state, TimeSpan timeOut)
        {
#if NETFX_CORE
            return new NetFxAsyncTimer(ThreadPoolTimer.CreateTimer(request.TimedOut, timeOut)); 
#else
            return new AsyncTimer(new Timer(state.TimedOut, state, (int)timeOut.TotalMilliseconds, Timeout.Infinite));
#endif
        }

        internal static void EndAsyncStream(this Stream stream)
        {
#if NETFX_CORE || WINDOWS_PHONE
                stream.Flush();
                stream.Dispose();
#else
            stream.Close();
#endif
        }

        internal static HttpWebRequest CreateHttpWebRequest(this AsyncServiceClient client, string requestUri)
        {
#if SILVERLIGHT && !WINDOWS_PHONE && !NETFX_CORE

            var creator = client.EmulateHttpViaPost
                ? System.Net.Browser.WebRequestCreator.BrowserHttp
                : System.Net.Browser.WebRequestCreator.ClientHttp;

            var webRequest = (HttpWebRequest) creator.Create(new Uri(requestUri));

            if (client.StoreCookies && !client.EmulateHttpViaPost)
            {
                if (client.ShareCookiesWithBrowser)
                {
                    if (CookieContainer == null)
                        CookieContainer = new CookieContainer();
                    client.CookieContainer.SetCookies(new Uri(BaseUri), System.Windows.Browser.HtmlPage.Document.Cookies);
                }
                
                webRequest.CookieContainer = client.CookieContainer;	
            }

#else
            var webRequest = (HttpWebRequest)WebRequest.Create(requestUri);
            client.CancelAsyncFn = webRequest.Abort;

            if (client.StoreCookies)
            {
                webRequest.CookieContainer = client.CookieContainer;
            }
#endif

#if !SILVERLIGHT
            if (!client.DisableAutoCompression)
            {
                webRequest.Headers.Add(HttpRequestHeader.AcceptEncoding, "gzip,deflate");
                webRequest.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
            }
#endif
            return webRequest;
        }
    }

#if NETFX_CORE

    public class NetFxAsyncTimer : ITimer
    {
        public ThreadPoolTimer Timer;

        public NetFxAsyncTimer(ThreadPoolTimer timer)
        {
            Timer = timer;
        }

        public void Cancel()
        {
            this.Timer.Cancel();
        }

        public void Dispose()
        {
            this.Timer.Dispose();
            this.Timer = null;
        }
    }

#else

    public class AsyncTimer : ITimer
    {
        public Timer Timer;

        public AsyncTimer(Timer timer)
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
            this.Timer.Dispose();
            this.Timer = null;
        }
    }

#endif

}