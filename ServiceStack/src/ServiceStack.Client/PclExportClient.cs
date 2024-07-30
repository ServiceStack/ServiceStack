//
// System.Collections.Specialized.NameObjectCollectionBase.cs
//
// Author:
//   Gleb Novodran
//   Andreas Nahr (ClassDevelopment@A-SoftTech.com)
//
// (C) Ximian, Inc.  http://www.ximian.com
// Copyright (C) 2005 Novell, Inc (http://www.novell.com)
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

/*** REMINDER: Keep this file in sync with ServiceStack.Text/Pcl.NameValueCollection.cs ***/

using System.IO;
using System.Net;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using ServiceStack.Text;
using System.Globalization;

//Dummy namespaces
namespace System.Collections.Specialized {}
namespace System.Web {}
namespace ServiceStack.Pcl {}

namespace ServiceStack
{
    using System;
    using System.Collections.Generic;
    using System.Web;
    using ServiceStack.Web;
    using ServiceStack.Pcl;
    using System.Collections.Specialized;

    public class PclExportClient 
    {
        public static PclExportClient Instance
#if !NETFRAMEWORK
          = NetStandardPclExportClient.Configure()
#else
          = Net45PclExportClient.Configure()
#endif
        ;

        public static readonly Task<object> EmptyTask = TypeConstants.EmptyTask;

        static PclExportClient()
        {
            if (Instance != null) 
                return;

            try
            {
                if (ConfigureProvider("ServiceStack.IosPclExportClient, ServiceStack.Pcl.iOS"))
                    return;
                if (ConfigureProvider("ServiceStack.AndroidPclExportClient, ServiceStack.Pcl.Android"))
                    return;
                if (ConfigureProvider("ServiceStack.WinStorePclExportClient, ServiceStack.Pcl.WinStore"))
                    return;
                if (ConfigureProvider("ServiceStack.Net40PclExportClient, ServiceStack.Pcl.Net45"))
                    return;
            }
            catch (Exception /*ignore*/) {}
        }

        public static bool ConfigureProvider(string typeName)
        {
            var type = Type.GetType(typeName);
            if (type == null)
                return false;

            var mi = type.GetMethod("Configure");
            if (mi != null)
            {
                mi.Invoke(null, TypeConstants.EmptyObjectArray);
            }

            return true;
        }

        [Obsolete("Use new NameValueCollection()")]
        public NameValueCollection NewNameValueCollection() => new NameValueCollection();

        public virtual NameValueCollection ParseQueryString(string query)
        {
#if !NETFRAMEWORK
            return ServiceStack.Pcl.HttpUtility.ParseQueryString(query);
#else
			return System.Web.HttpUtility.ParseQueryString(query);
#endif
        }

        public virtual string UrlEncode(string url)
        {
            return WebUtility.UrlEncode(url);
        }

        public virtual string UrlDecode(string url)
        {
            return WebUtility.UrlDecode(url);
        }

        public virtual string HtmlAttributeEncode(string html)
        {
#if !NETFRAMEWORK
            return HtmlEncode(html).Replace("\"","&quot;").Replace("'","&#x27;");
#else
            return System.Web.HttpUtility.HtmlAttributeEncode(html);
#endif
        }

        public virtual string HtmlEncode(string html)
        {
            return WebUtility.HtmlEncode(html);
        }

        public virtual string HtmlDecode(string html)
        {
            return WebUtility.HtmlDecode(html);
        }

        public virtual void AddHeader(WebRequest webReq, NameValueCollection headers)
        {
            if (headers == null)
                return;

            foreach (var name in headers.AllKeys)
            {
                webReq.Headers[name] = headers[name];
            }
        }

        public virtual string GetHeader(WebHeaderCollection headers, string name, Func<string, bool> valuePredicate)
        {
            return null;
        }

        public virtual void SetCookieContainer(HttpWebRequest webRequest, ServiceClientBase client)
        {
            webRequest.CookieContainer = client.CookieContainer;
        }

        public virtual void SetCookieContainer(HttpWebRequest webRequest, AsyncServiceClient client)
        {
            webRequest.CookieContainer = client.CookieContainer;
        }

        public virtual void SynchronizeCookies(AsyncServiceClient client)
        {
        }

        public virtual ITimer CreateTimer(TimerCallback cb, TimeSpan timeOut, object state)
        {
            return new AsyncTimer(new
                System.Threading.Timer(cb, state, (int)timeOut.TotalMilliseconds, Timeout.Infinite));
        }

        public virtual Task WaitAsync(int waitForMs)
        {
            if (waitForMs <= 0)
                throw new ArgumentOutOfRangeException(nameof(waitForMs));

            var tcs = new TaskCompletionSource<bool>();
            Timer timer = null;
            timer = new Timer(self => {
                tcs.TrySetResult(true);
                timer.Dispose();
            }, null, waitForMs, Timeout.Infinite);
            return tcs.Task;
        }

        public virtual void RunOnUiThread(Action fn)
        {
            if (UiContext == null)
            {
                fn();
            }
            else
            {
                UiContext.Post(_ => fn(), null);
            }
        }

        public SynchronizationContext UiContext;
        public static void Configure(PclExportClient instance)
        {
            Instance = instance;
            Instance.UiContext = SynchronizationContext.Current;
        }

        public virtual Exception CreateTimeoutException(Exception ex, string errorMsg)
        {
            return new WebException("The request timed out", ex, WebExceptionStatus.RequestCanceled, null);
        }

        public virtual void CloseReadStream(Stream stream)
        {
            stream.Close();
        }

        public virtual void CloseWriteStream(Stream stream)
        {
            stream.Close();
        }

        public virtual bool IsWebException(WebException webEx)
        {
            return webEx?.Response != null;
        }

        public virtual void SetIfModifiedSince(HttpWebRequest webReq, DateTime lastModified)
        {
            webReq.IfModifiedSince = lastModified;
        }
    }

}
