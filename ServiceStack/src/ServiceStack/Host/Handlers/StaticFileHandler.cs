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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using System.Web;
using ServiceStack.Internal;
using ServiceStack.IO;
using ServiceStack.Logging;
using ServiceStack.Text;
using ServiceStack.Web;

namespace ServiceStack.Host.Handlers;

public class StaticFileHandler : HttpAsyncTaskHandler
{
    public static int DefaultBufferSize = 1024 * 1024;

    public static Action<IRequest, IResponse, IVirtualFile> ResponseFilter { get; set; }
    public Action<IRequest, IResponse, IVirtualFile> Filter { get; set; }

    public StaticFileHandler()
    {
        BufferSize = DefaultBufferSize;
        RequestName = GetType().Name; //Always allow StaticFileHandlers
    }

    /// <summary>
    /// Return File at specified virtualPath from AppHost.VirtualFiles ContentRootPath
    /// </summary>
    public StaticFileHandler(string virtualPath) : this()
    {
        VirtualNode = HostContext.AppHost.VirtualFiles.GetFile(virtualPath);

        if (VirtualNode == null)
            throw new ArgumentException("Could not find file at VirtualPath: " + virtualPath);
    }

    public StaticFileHandler(IVirtualFile virtualFile) : this()
    {
        VirtualNode = virtualFile;
    }

    public StaticFileHandler(IVirtualDirectory virtualDir) : this()
    {
        VirtualNode = virtualDir;
    }

    public int BufferSize { get; set; }
    private static DateTime DefaultFileModified { get; set; }
    private static string DefaultFilePath { get; set; }
    private static byte[] DefaultFileContents { get; set; }
    public IVirtualNode VirtualNode { get; set; }

    private static ConcurrentDictionary<string, byte[]> defaultFileCacheZip = new();
        
    /// <summary>
    /// Keep default file contents in-memory
    /// </summary>
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
            LogManager.GetLogger(typeof(StaticFileHandler)).Error(ex.Message, ex);
        }
    }

    public override async Task ProcessRequestAsync(IRequest request, IResponse response, string operationName)
    {
        HostContext.ApplyCustomHandlerRequestFilters(request, response);
        if (response.IsClosed) return;

        await response.EndHttpHandlerRequestAsync(afterHeaders: async r =>
        {
            var node = this.VirtualNode ?? request.GetVirtualNode();
            var file = node as IVirtualFile;
            var appHost = HostContext.AppHost;
            if (file == null)
            {
                if (node is IVirtualDirectory dir)
                {
                    file = dir.GetDefaultDocument(appHost.Config.DefaultDocuments);
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
                            allFiles = CreateFileIndex(appHost.RootDirectory.RealPath);
                        if (allDirs == null)
                            allDirs = CreateDirIndex(appHost.RootDirectory.RealPath);

                        if (allFiles.TryGetValue(fileName.ToLower(), out fileName))
                        {
                            file = appHost.VirtualFileSources.GetFile(fileName);
                        }
                    }

                    if (file == null)
                    {
                        var msg = ErrorMessages.FileNotExistsFmt.LocalizeFmt(request, request.PathInfo.SafeInput());
                        LogManager.GetLogger(GetType()).Warn($"{msg} in path: {originalFileName}");
                        response.StatusCode = 404;
                        response.StatusDescription = msg;
                        return;
                    }
                }
            }

            file.Refresh(); //refresh FileInfo, DateModified, Length

            if (r.ContentType != null && appHost.Config.AddMaxAgeForStaticMimeTypes.TryGetValue(r.ContentType, out var maxAge))
            {
                r.AddHeader(HttpHeaders.CacheControl, "max-age=" + maxAge.TotalSeconds);
            }

            if (request.HasNotModifiedSince(file.LastModified))
            {
                r.ContentType = MimeTypes.GetMimeType(file.Name);
                r.StatusCode = (int)HttpStatusCode.NotModified;
                r.StatusDescription = HttpStatusCode.NotModified.ToString();

                Filter?.Invoke(request, r, file);
                ResponseFilter?.Invoke(request, r, file);
                return;
            }

            try
            {
                var encoding = request.GetCompressionType();
                var shouldCompress = encoding != null && appHost.ShouldCompressFile(file);
                r.AddHeaderLastModified(file.LastModified);
                r.ContentType = MimeTypes.GetMimeType(file.Name);

                if (Filter != null)
                {
                    Filter(request, r, file);
                    if (r.IsClosed)
                        return;
                }
                if (ResponseFilter != null)
                {
                    ResponseFilter(request, r, file);
                    if (r.IsClosed)
                        return;
                }

                if (!HostContext.DebugMode && file.VirtualPath.EqualsIgnoreCase(DefaultFilePath))
                {
                    if (file.LastModified > DefaultFileModified)
                        SetDefaultFile(DefaultFilePath, file.ReadAllBytes(), file.LastModified); //reload

                    var compressor = shouldCompress ? StreamCompressors.Get(encoding) : null;
                    if (compressor == null)
                    {
                        await r.OutputStream.WriteAsync(DefaultFileContents).ConfigAwait();
                        await r.OutputStream.FlushAsync().ConfigAwait();
                    }
                    else
                    {
                        var zipBytes = defaultFileCacheZip.GetOrAdd(encoding, _ => compressor.Compress(DefaultFileContents));
                        r.AddHeader(HttpHeaders.ContentEncoding, encoding);
                        r.SetContentLength(zipBytes.Length);
                        await r.OutputStream.WriteAsync(zipBytes).ConfigAwait();
                        await r.OutputStream.FlushAsync().ConfigAwait();
                    }

                    await r.CloseAsync().ConfigAwait();
                    return;
                }

                if (appHost.Config.AllowPartialResponses)
                    r.AddHeader(HttpHeaders.AcceptRanges, "bytes");
                long contentLength = file.Length;
                long rangeStart, rangeEnd;
                var rangeHeader = request.Headers[HttpHeaders.Range];
                if (appHost.Config.AllowPartialResponses && rangeHeader != null)
                {
                    rangeHeader.ExtractHttpRanges(contentLength, out rangeStart, out rangeEnd);

                    if (rangeEnd > contentLength - 1)
                        rangeEnd = contentLength - 1;

                    r.AddHttpRangeResponseHeaders(rangeStart: rangeStart, rangeEnd: rangeEnd,
                        contentLength: contentLength);
                }
                else
                {
                    rangeStart = 0;
                    rangeEnd = contentLength - 1;
                }

                var outputStream = r.OutputStream;
                if (rangeStart != 0 || rangeEnd != file.Length - 1)
                {
                    await file.WritePartialToAsync(outputStream, rangeStart, rangeEnd).ConfigAwait();
                }
                else
                {
                    using var fs = file.OpenRead();
                    if (!shouldCompress)
                    {
                        r.SetContentLength(contentLength);
                        await fs.CopyToAsync(outputStream, BufferSize).ConfigAwait();
                        await outputStream.FlushAsync().ConfigAwait();
                    }
                    else
                    {
                        r.AddHeader(HttpHeaders.ContentEncoding, encoding);
                        outputStream = outputStream.CompressStream(encoding);
                        await fs.CopyToAsync(outputStream).ConfigAwait();
                        await outputStream.FlushAsync().ConfigAwait();
                        await outputStream.DisposeAsync();
                    }
                }
            }
#if !NETCORE
            catch (System.Net.HttpListenerException ex)
            {
                if (ex.ErrorCode == 1229)
                    return;
                //Error: 1229 is "An operation was attempted on a nonexistent network connection"
                //This exception occurs when http stream is terminated by web browser because user
                //seek video forward and new http request will be sent by browser
                //with attribute in header "Range: bytes=newSeekPosition-"
                throw;
            }
#endif
            catch (Exception ex)
            {
                if (ex is IHttpError httpError)
                {
                    r.StatusCode = (int) httpError.StatusCode;
                    r.StatusDescription = httpError.Message ?? httpError.ErrorCode;
                    return;
                }
                    
                LogManager.GetLogger(GetType()).ErrorFormat($"Static file {request.PathInfo} forbidden: {ex.Message}");
                throw new HttpException(403, "Forbidden.");
            }
        }).ConfigAwait();
    }

    static Dictionary<string, string> CreateFileIndex(string appFilePath)
    {
        var log = LogManager.GetLogger(typeof(StaticFileHandler));
        if (log.IsDebugEnabled)
            log.Debug("Building case-insensitive fileIndex for Mono at: " + appFilePath);

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

    public override bool IsReusable => true;

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
