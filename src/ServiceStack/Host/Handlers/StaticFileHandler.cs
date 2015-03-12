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
using System.Collections.Generic;
using System.IO;
using System.Web;
using ServiceStack.IO;
using ServiceStack.Logging;
using ServiceStack.Text;
using ServiceStack.Web;

namespace ServiceStack.Host.Handlers
{
    public class StaticFileHandler : HttpAsyncTaskHandler
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(StaticFileHandler));
        public static int DefaultBufferSize = 1024 * 1024;

        public static Action<IRequest, IResponse, IVirtualFile> ResponseFilter { get; set; }

        public StaticFileHandler()
        {
            BufferSize = DefaultBufferSize;
            RequestName = GetType().Name; //Always allow StaticFileHandlers
        }

        public override void ProcessRequest(HttpContextBase context)
        {
            var httpReq = context.ToRequest(GetType().GetOperationName());
            ProcessRequest(httpReq, httpReq.Response, httpReq.OperationName);
        }

        public int BufferSize { get; set; }
        private static DateTime DefaultFileModified { get; set; }
        private static string DefaultFilePath { get; set; }
        private static byte[] DefaultFileContents { get; set; }
        public IVirtualNode VirtualNode { get; set; }

        /// <summary>
        /// Keep default file contents in-memory
        /// </summary>
        /// <param name="defaultFilePath"></param>
        public static void SetDefaultFile(string defaultFilePath, byte[] defaultFileContents, DateTime defaultFileModified)
        {
            try
            {
                DefaultFilePath = defaultFilePath;
                DefaultFileContents = defaultFileContents;
                DefaultFileModified = defaultFileModified;
            }
            catch (Exception ex)
            {
                log.Error(ex.Message, ex);
            }
        }

        public override void ProcessRequest(IRequest request, IResponse response, string operationName)
        {
            HostContext.ApplyCustomHandlerRequestFilters(request, response);
            if (response.IsClosed) return;

            response.EndHttpHandlerRequest(skipClose: true, afterHeaders: r =>
            {
                var node = this.VirtualNode ?? request.GetVirtualNode();
                var file = node as IVirtualFile;
                if (file == null)
                {
                    var dir = node as IVirtualDirectory;
                    if (dir != null)
                    {
                        file = dir.GetDefaultDocument();
                        if (file != null && HostContext.Config.RedirectToDefaultDocuments)
                        {
                            r.Redirect(request.GetPathUrl() + '/' + file.Name);
                            return;
                        }
                    }

                    if (file == null)
                    {
                        var fileName = request.PathInfo;
                        var originalFileName = fileName;

                        if (Env.IsMono)
                        {
                            //Create a case-insensitive file index of all host files
                            if (allFiles == null)
                                allFiles = CreateFileIndex(HostContext.VirtualPathProvider.RootDirectory.RealPath);
                            if (allDirs == null)
                                allDirs = CreateDirIndex(HostContext.VirtualPathProvider.RootDirectory.RealPath);

                            if (allFiles.TryGetValue(fileName.ToLower(), out fileName))
                            {
                                file = HostContext.VirtualPathProvider.GetFile(fileName);
                            }
                        }

                        if (file == null)
                        {
                            var msg = ErrorMessages.FileNotExistsFmt.Fmt(request.PathInfo);
                            log.WarnFormat("{0} in path: {1}", msg, originalFileName);
                            response.StatusCode = 404;
                            response.StatusDescription = msg;
                            return;
                        }
                    }
                }

                file.Refresh(); //refresh FileInfo, DateModified, Length

                TimeSpan maxAge;
                if (r.ContentType != null && HostContext.Config.AddMaxAgeForStaticMimeTypes.TryGetValue(r.ContentType, out maxAge))
                {
                    r.AddHeader(HttpHeaders.CacheControl, "max-age=" + maxAge.TotalSeconds);
                }

                if (request.HasNotModifiedSince(file.LastModified))
                {
                    r.ContentType = MimeTypes.GetMimeType(file.Name);
                    r.StatusCode = 304;
                    return;
                }

                try
                {
                    r.AddHeaderLastModified(file.LastModified);
                    r.ContentType = MimeTypes.GetMimeType(file.Name);

                    if (ResponseFilter != null)
                    {
                        ResponseFilter(request, r, file);

                        if (r.IsClosed)
                            return;
                    }

                    if (file.VirtualPath.EqualsIgnoreCase(DefaultFilePath))
                    {
                        if (file.LastModified > DefaultFileModified)
                            SetDefaultFile(DefaultFilePath, file.ReadAllBytes(), file.LastModified); //reload

                        r.OutputStream.Write(DefaultFileContents, 0, DefaultFileContents.Length);
                        r.Close();
                        return;
                    }

                    if (HostContext.Config.AllowPartialResponses)
                        r.AddHeader(HttpHeaders.AcceptRanges, "bytes");
                    long contentLength = file.Length;
                    long rangeStart, rangeEnd;
                    var rangeHeader = request.Headers[HttpHeaders.Range];
                    if (HostContext.Config.AllowPartialResponses && rangeHeader != null)
                    {
                        rangeHeader.ExtractHttpRanges(contentLength, out rangeStart, out rangeEnd);

                        if (rangeEnd > contentLength - 1)
                            rangeEnd = contentLength - 1;

                        r.AddHttpRangeResponseHeaders(rangeStart: rangeStart, rangeEnd: rangeEnd, contentLength: contentLength);
                    }
                    else
                    {
                        rangeStart = 0;
                        rangeEnd = contentLength - 1;
                        r.SetContentLength(contentLength); //throws with ASP.NET webdev server non-IIS pipelined mode
                    }
                    var outputStream = r.OutputStream;
                    using (var fs = file.OpenRead())
                    {
                        if (rangeStart != 0 || rangeEnd != file.Length - 1)
                        {
                            fs.WritePartialTo(outputStream, rangeStart, rangeEnd);
                        }
                        else
                        {
                            fs.CopyTo(outputStream, BufferSize);
                            outputStream.Flush();
                        }
                    }
                }
                catch (System.Net.HttpListenerException ex)
                {
                    if (ex.ErrorCode == 1229)
                        return;
                    //Error: 1229 is "An operation was attempted on a nonexistent network connection"
                    //This exception occures when http stream is terminated by web browser because user
                    //seek video forward and new http request will be sent by browser
                    //with attribute in header "Range: bytes=newSeekPosition-"
                    throw;
                }
                catch (Exception ex)
                {
                    log.ErrorFormat("Static file {0} forbidden: {1}", request.PathInfo, ex.Message);
                    throw new HttpException(403, "Forbidden.");
                }
            });
        }

        static Dictionary<string, string> CreateFileIndex(string appFilePath)
        {
            log.Debug("Building case-insensitive fileIndex for Mono at: "
                      + appFilePath);

            var caseInsensitiveLookup = new Dictionary<string, string>();
            foreach (var file in GetFiles(appFilePath))
            {
                caseInsensitiveLookup[file.ToLower()] = file;
            }

            return caseInsensitiveLookup;
        }

        static Dictionary<string, string> CreateDirIndex(string appFilePath)
        {
            var indexDirs = new Dictionary<string, string>();

            foreach (var dir in GetDirs(appFilePath))
            {
                indexDirs[dir.ToLower()] = dir;
            }

            return indexDirs;
        }

        public override bool IsReusable
        {
            get { return true; }
        }

        public static bool DirectoryExists(string dirPath, string appFilePath)
        {
            if (dirPath == null) return false;

            try
            {
                if (!Env.IsMono)
                    return Directory.Exists(dirPath);
            }
            catch
            {
                return false;
            }

            if (allDirs == null)
                allDirs = CreateDirIndex(appFilePath);

            var foundDir = allDirs.ContainsKey(dirPath.ToLower());

            //log.DebugFormat("Found dirPath {0} in Mono: ", dirPath, foundDir);

            return foundDir;
        }

        private static Dictionary<string, string> allDirs; //populated by GetFiles()
        private static Dictionary<string, string> allFiles;

        static IEnumerable<string> GetFiles(string path)
        {
            var queue = new Queue<string>();
            queue.Enqueue(path);

            while (queue.Count > 0)
            {
                path = queue.Dequeue();
                try
                {
                    foreach (string subDir in Directory.GetDirectories(path))
                    {
                        queue.Enqueue(subDir);
                    }
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine(ex);
                }
                string[] files = null;
                try
                {
                    files = Directory.GetFiles(path);
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine(ex);
                }
                if (files != null)
                {
                    for (int i = 0; i < files.Length; i++)
                    {
                        yield return files[i];
                    }
                }
            }
        }

        static List<string> GetDirs(string path)
        {
            var queue = new Queue<string>();
            queue.Enqueue(path);

            var results = new List<string>();

            while (queue.Count > 0)
            {
                path = queue.Dequeue();
                try
                {
                    foreach (string subDir in Directory.GetDirectories(path))
                    {
                        queue.Enqueue(subDir);
                        results.Add(subDir);
                    }
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine(ex);
                }
            }

            return results;
        }
    }
}