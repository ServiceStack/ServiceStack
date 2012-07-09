//
// System.Web.StaticFileHandler
//
// Authors:
//	Gonzalo Paniagua Javier (gonzalo@ximian.com)
//
// (C) 2002 Ximian, Inc (http://www.ximian.com)
//

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

using System;
using System.IO;
using System.Web;
using ServiceStack.Common;
using ServiceStack.Common.Web;
using ServiceStack.Logging;
using ServiceStack.ServiceHost;
using ServiceStack.Text;
using ServiceStack.WebHost.Endpoints.Extensions;

namespace ServiceStack.WebHost.Endpoints.Support
{
	class StaticFileHandler : IHttpHandler, IServiceStackHttpHandler
	{
		private static ILog log = LogManager.GetLogger(typeof (StaticFileHandler));

		public void ProcessRequest(HttpContext context)
		{
			ProcessRequest(
			new HttpRequestWrapper(null, context.Request),
			new HttpResponseWrapper(context.Response), 
			null);
		}

		private DateTime DefaultFileModified { get; set; }
		private string DefaultFilePath { get; set; }
		private byte[] DefaultFileContents { get; set; }

		/// <summary>
		/// Keep default file contents in-memory
		/// </summary>
		/// <param name="defaultFilePath"></param>
		public void SetDefaultFile(string defaultFilePath)
		{
			try
			{
				this.DefaultFileContents = File.ReadAllBytes(defaultFilePath);
				this.DefaultFilePath = defaultFilePath;
				this.DefaultFileModified = File.GetLastWriteTime(defaultFilePath);
			}
			catch (Exception ex)
			{
				log.Error(ex.Message, ex);
			}
		}

        public void ProcessRequest(IHttpRequest request, IHttpResponse response, string operationName)
		{
            response.EndHttpRequest(skipClose: true, afterBody: r => {
                var fileName = request.GetPhysicalPath();

                var fi = new FileInfo(fileName);
                if (!fi.Exists)
                {
                    if ((fi.Attributes & FileAttributes.Directory) != 0)
                    {
                        foreach (var defaultDoc in EndpointHost.Config.DefaultDocuments)
                        {
                            var defaultFileName = Path.Combine(fi.FullName, defaultDoc);
                            if (!File.Exists(defaultFileName)) continue;
                            r.Redirect(request.GetPathUrl() + '/' + defaultDoc);
                            return;
                        }
                    }

                    if (!fi.Exists)
                        throw new HttpException(404, "File '" + request.PathInfo + "' not found.");
                }

                TimeSpan maxAge;
                if (r.ContentType != null && EndpointHost.Config.AddMaxAgeForStaticMimeTypes.TryGetValue(r.ContentType, out maxAge))
                {
                    r.AddHeader(HttpHeaders.CacheControl, "max-age=" + maxAge.TotalSeconds);
                }

                if (request.HasNotModifiedSince(fi.LastWriteTime))
                {
                    r.ContentType = MimeTypes.GetMimeType(fileName);
                    r.StatusCode = 304;
                    return;
                }

                try
                {
                    r.AddHeaderLastModified(fi.LastWriteTime);
                    r.ContentType = MimeTypes.GetMimeType(fileName);

                    if (fileName.EqualsIgnoreCase(this.DefaultFilePath))
                    {
                        if (fi.LastWriteTime > this.DefaultFileModified)
                            SetDefaultFile(this.DefaultFilePath); //reload

                        r.OutputStream.Write(this.DefaultFileContents, 0, this.DefaultFileContents.Length);
                        r.Close();
                        return;
                    }

                    if (!Env.IsMono)
                    {
                        r.TransmitFile(fileName);
                    }
                    else
                    {
                        r.WriteFile(fileName);
                    }
                }
                catch (Exception)
                {
                    throw new HttpException(403, "Forbidden.");
                }
            });
		}

		public bool IsReusable
		{
			get { return true; }
		}
	}
}