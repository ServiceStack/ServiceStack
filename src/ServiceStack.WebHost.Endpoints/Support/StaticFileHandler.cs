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
using System.Globalization;
using System.IO;
using System.Web;
using ServiceStack.Common.Web;

namespace ServiceStack.WebHost.Endpoints.Support
{
	class StaticFileHandler : IHttpHandler
	{
		public void ProcessRequest(HttpContext context)
		{
			var request = context.Request;
			var response = context.Response;
			var fileName = request.PhysicalPath;
			var fi = new FileInfo(fileName);
			if (!fi.Exists)
			{
				if ((fi.Attributes & FileAttributes.Directory) != 0)
				{
					foreach (var defaultDoc in EndpointHost.Config.DefaultDocuments)
					{
						var defaultFileName = Path.Combine(fi.FullName, defaultDoc);
						var defaultFileInfo = new FileInfo(defaultFileName);
						if (!defaultFileInfo.Exists) continue;
						response.Redirect(request.Path + '/' + defaultDoc);
						return;
					}
				}

				if (!fi.Exists)
					throw new HttpException(404, "File '" + request.FilePath + "' not found.");
			}


			var strHeader = request.Headers["If-Modified-Since"];
			try
			{
				if (strHeader != null)
				{
					var dtIfModifiedSince = DateTime.ParseExact(strHeader, "r", null);
					var ftime = fi.LastWriteTime.ToUniversalTime();
					if (ftime <= dtIfModifiedSince)
					{
						response.StatusCode = 304;
						return;
					}
				}
			}
			catch { }

			try
			{
				var lastWT = fi.LastWriteTime.ToUniversalTime();
				response.AddHeader("Last-Modified", lastWT.ToString("r"));

				response.ContentType = MimeTypes.GetMimeType(fileName);
				response.TransmitFile(fileName);
			}
			catch (Exception e)
			{
				throw new HttpException(403, "Forbidden.");
			}
		}

		public bool IsReusable
		{
			get { return true; }
		}
	}
}