using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using ServiceStack.IO;
using ServiceStack.Text;

namespace ServiceStack.Templates
{
    // ReSharper disable InconsistentNaming
    
    public class TemplateProtectedFilters : TemplateFilter
    {
        public IVirtualFile ResolveFile(string filterName, TemplateScopeContext scope, string virtualPath)
        {
            var file = ResolveFile(scope.Context.VirtualFiles, scope.PageResult.VirtualPath, virtualPath);
            if (file == null)
                throw new FileNotFoundException($"{filterName} '{virtualPath}' in page '{scope.Page.VirtualPath}' was not found");

            return file;
        }

        public IVirtualFile ResolveFile(IVirtualPathProvider virtualFiles, string fromVirtualPath, string virtualPath)
        {
            IVirtualFile file = null;

            var pathMapKey = nameof(ResolveFile) + ">" + fromVirtualPath;
            var pathMapping = Context.GetPathMapping(pathMapKey, virtualPath);
            if (pathMapping != null)
            {
                file = virtualFiles.GetFile(pathMapping);
                if (file != null)
                    return file;                    
                Context.RemovePathMapping(pathMapKey, pathMapping);
            }

            var tryExactMatch = virtualPath.IndexOf('/') >= 0; //if nested path specified, look for an exact match first
            if (tryExactMatch)
            {
                file = virtualFiles.GetFile(virtualPath);
                if (file != null)
                {
                    Context.SetPathMapping(pathMapKey, virtualPath, virtualPath);
                    return file;
                }
            }

            if (file == null)
            {
                var parentPath = fromVirtualPath.IndexOf('/') >= 0
                    ? fromVirtualPath.LastLeftPart('/')
                    : "";

                do
                {
                    var seekPath = parentPath.CombineWith(virtualPath);
                    file = virtualFiles.GetFile(seekPath);
                    if (file != null)
                    {
                        Context.SetPathMapping(pathMapKey, virtualPath, seekPath);
                        return file;
                    }

                    if (parentPath == "")
                        break;

                    parentPath = parentPath.IndexOf('/') >= 0
                        ? parentPath.LastLeftPart('/')
                        : "";
                } while (true);
            }
            return null;
        }

        //alias
        public Task fileContents(TemplateScopeContext scope, string virtualPath) => includeFile(scope, virtualPath);

        public async Task includeFile(TemplateScopeContext scope, string virtualPath)
        {
            var file = ResolveFile(nameof(includeFile), scope, virtualPath);
            using (var reader = file.OpenRead())
            {
                await reader.CopyToAsync(scope.OutputStream);
            }
        }

        public IEnumerable<IVirtualFile> vfsAllFiles() => Context.VirtualFiles.GetAllFiles();
        public IEnumerable<IVirtualFile> vfsAllRootFiles() => Context.VirtualFiles.GetRootFiles();
        public IEnumerable<IVirtualDirectory> vfsAllRootDirectories() => Context.VirtualFiles.GetRootDirectories();
        public string vfsCombinePath(string basePath, string relativePath) => Context.VirtualFiles.CombineVirtualPath(basePath, relativePath);

        public IVirtualDirectory dir(string virtualPath) => Context.VirtualFiles.GetDirectory(virtualPath);
        public bool dirExists(string virtualPath) => Context.VirtualFiles.DirectoryExists(virtualPath);
        public IVirtualFile dirFile(string dirPath, string fileName) => Context.VirtualFiles.GetDirectory(dirPath)?.GetFile(fileName);
        public IEnumerable<IVirtualFile> dirFiles(string dirPath) => Context.VirtualFiles.GetDirectory(dirPath)?.GetFiles() ?? new List<IVirtualFile>();
        public IVirtualDirectory dirDirectory(string dirPath, string dirName) => Context.VirtualFiles.GetDirectory(dirPath)?.GetDirectory(dirName);
        public IEnumerable<IVirtualDirectory> dirDirectories(string dirPath) => Context.VirtualFiles.GetDirectory(dirPath)?.GetDirectories() ?? new List<IVirtualDirectory>();
        public IEnumerable<IVirtualFile> dirFilesFind(string dirPath, string globPatern) => Context.VirtualFiles.GetDirectory(dirPath)?.GetAllMatchingFiles(globPatern);

        public IEnumerable<IVirtualFile> filesFind(string globPatern) => Context.VirtualFiles.GetAllMatchingFiles(globPatern);
        public bool fileExists(string virtualPath) => Context.VirtualFiles.FileExists(virtualPath);
        public IVirtualFile file(string virtualPath) => Context.VirtualFiles.GetFile(virtualPath);
        public string fileWrite(string virtualPath, object contents)
        {
            if (contents is string s)
                Context.VirtualFiles.WriteFile(virtualPath, s);
            else if (contents is byte[] bytes)
                Context.VirtualFiles.WriteFile(virtualPath, bytes);
            else if (contents is Stream stream)
                Context.VirtualFiles.WriteFile(virtualPath, stream);
            else
                return null;

            return virtualPath;
        }

        public string fileAppend(string virtualPath, object contents)
        {
            if (contents is string s)
                Context.VirtualFiles.AppendFile(virtualPath, s);
            else if (contents is byte[] bytes)
                Context.VirtualFiles.AppendFile(virtualPath, bytes);
            else if (contents is Stream stream)
                Context.VirtualFiles.AppendFile(virtualPath, stream);
            else
                return null;

            return virtualPath;
        }

        public string fileDelete(string virtualPath)
        {
            Context.VirtualFiles.DeleteFile(virtualPath);
            return virtualPath;
        }

        public string dirDelete(string virtualPath)
        {
            Context.VirtualFiles.DeleteFolder(virtualPath);
            return virtualPath;
        }

        public string fileReadAll(string virtualPath) => Context.VirtualFiles.GetFile(virtualPath)?.ReadAllText();
        public byte[] fileReadAllBytes(string virtualPath) => Context.VirtualFiles.GetFile(virtualPath)?.ReadAllBytes();
        public string fileHash(string virtualPath) => Context.VirtualFiles.GetFileHash(virtualPath);

        //alias
        public Task urlContents(TemplateScopeContext scope, string url) => includeUrl(scope, url, null);
        public Task urlContents(TemplateScopeContext scope, string url, object options) => includeUrl(scope, url, options);

        public Task includeUrl(TemplateScopeContext scope, string url) => includeUrl(scope, url, null);
        public async Task includeUrl(TemplateScopeContext scope, string url, object options)
        {
            var scopedParams = scope.AssertOptions(nameof(includeUrl), options);

            var webReq = (HttpWebRequest)WebRequest.Create(url);
            var dataType = scopedParams.TryGetValue("dataType", out object value)
                ? ConvertDataTypeToContentType((string)value)
                : null;

            if (scopedParams.TryGetValue("method", out value))
                webReq.Method = (string)value;
            if (scopedParams.TryGetValue("contentType", out value) || dataType != null)
                webReq.ContentType = (string)value ?? dataType;            
            if (scopedParams.TryGetValue("accept", out value) || dataType != null) 
                webReq.Accept = (string)value ?? dataType;            
            if (scopedParams.TryGetValue("userAgent", out value))
                PclExport.Instance.SetUserAgent(webReq, (string)value);

            if (scopedParams.TryRemove("data", out object data))
            {
                if (webReq.Method == null)
                    webReq.Method = HttpMethods.Post;
                    
                if (webReq.ContentType == null)
                    webReq.ContentType = MimeTypes.FormUrlEncoded;

                var body = ConvertDataToString(data, webReq.ContentType);
                using (var stream = await webReq.GetRequestStreamAsync())
                {
                    await stream.WriteAsync(body);
                }
            }

            using (var webRes = await webReq.GetResponseAsync())
            using (var stream = webRes.GetResponseStream())
            {
                await stream.CopyToAsync(scope.OutputStream);
            }
        }

        private static string ConvertDataTypeToContentType(string dataType)
        {
            switch (dataType)
            {
                case "json":
                    return MimeTypes.Json;
                case "jsv":
                    return MimeTypes.Jsv;
                case "csv":
                    return MimeTypes.Csv;
                case "xml":
                    return MimeTypes.Xml;
                case "text":
                    return MimeTypes.PlainText;
                case "form":
                    return MimeTypes.FormUrlEncoded;
            }
            
            throw new NotSupportedException($"Unknown dataType '{dataType}'");
        }

        private static string ConvertDataToString(object data, string contentType)
        {
            if (data is string s)
                return s;
            switch (contentType)
            {
                case MimeTypes.PlainText:
                    return data.ToString();
                case MimeTypes.Json:
                    return data.ToJson();
                case MimeTypes.Csv:
                    return data.ToCsv();
                case MimeTypes.Jsv:
                    return data.ToJsv();
                case MimeTypes.Xml:
                    return data.ToXml();
                case MimeTypes.FormUrlEncoded:
                    WriteComplexTypeDelegate holdQsStrategy = QueryStringStrategy.FormUrlEncoded;
                    QueryStringSerializer.ComplexTypeStrategy = QueryStringStrategy.FormUrlEncoded;
                    var urlEncodedBody = QueryStringSerializer.SerializeToString(data);
                    QueryStringSerializer.ComplexTypeStrategy = holdQsStrategy;
                    return urlEncodedBody;
            }

            throw new NotSupportedException($"Can not serialize to unknown Content-Type '{contentType}'");
        }

        public static string CreateCacheKey(string url, Dictionary<string,object> options=null)
        {
            var sb = StringBuilderCache.Allocate()
                .Append(url);
            
            if (options != null)
            {
                foreach (var entry in options)
                {
                    sb.Append(entry.Key)
                      .Append('=')
                      .Append(entry.Value);
                }
            }

            return StringBuilderCache.ReturnAndFree(sb);
        }
        
        //alias
        public Task fileContentsWithCache(TemplateScopeContext scope, string virtualPath) => includeFileWithCache(scope, virtualPath, null);
        public Task fileContentsWithCache(TemplateScopeContext scope, string virtualPath, object options) => includeFileWithCache(scope, virtualPath, options);

        public Task includeFileWithCache(TemplateScopeContext scope, string virtualPath) => includeFileWithCache(scope, virtualPath, null);
        public async Task includeFileWithCache(TemplateScopeContext scope, string virtualPath, object options)
        {
            var scopedParams = scope.AssertOptions(nameof(includeUrl), options);
            var expireIn = scopedParams.TryGetValue("expireInSecs", out object value)
                ? TimeSpan.FromSeconds(value.ConvertTo<int>())
                : (TimeSpan)scope.Context.Args[TemplateConstants.DefaultFileCacheExpiry];
            
            var cacheKey = CreateCacheKey("file:" + scope.PageResult.VirtualPath + ">" + virtualPath, scopedParams);
            if (Context.ExpiringCache.TryGetValue(cacheKey, out Tuple<DateTime, object> cacheEntry))
            {
                if (cacheEntry.Item1 > DateTime.UtcNow && cacheEntry.Item2 is byte[] bytes)
                {
                    await scope.OutputStream.WriteAsync(bytes);
                    return;
                }
            }

            var file = ResolveFile(nameof(includeFileWithCache), scope, virtualPath);
            var ms = MemoryStreamFactory.GetStream();
            using (ms)
            {
                using (var reader = file.OpenRead())
                {
                    await reader.CopyToAsync(ms);
                }

                ms.Position = 0;
                var bytes = ms.ToArray();
                Context.ExpiringCache[cacheKey] = Tuple.Create(DateTime.UtcNow.Add(expireIn),(object)bytes);
                await scope.OutputStream.WriteAsync(bytes);
            }
        }

        //alias
        public Task urlContentsWithCache(TemplateScopeContext scope, string url) => includeUrlWithCache(scope, url, null);
        public Task urlContentsWithCache(TemplateScopeContext scope, string url, object options) => includeUrlWithCache(scope, url, options);
        
        public Task includeUrlWithCache(TemplateScopeContext scope, string url) => includeUrlWithCache(scope, url, null);
        public async Task includeUrlWithCache(TemplateScopeContext scope, string url, object options)
        {
            var scopedParams = scope.AssertOptions(nameof(includeUrl), options);
            var expireIn = scopedParams.TryGetValue("expireInSecs", out object value)
                ? TimeSpan.FromSeconds(value.ConvertTo<int>())
                : (TimeSpan)scope.Context.Args[TemplateConstants.DefaultUrlCacheExpiry];

            var cacheKey = CreateCacheKey("url:" + url, scopedParams);
            if (Context.ExpiringCache.TryGetValue(cacheKey, out Tuple<DateTime, object> cacheEntry))
            {
                if (cacheEntry.Item1 > DateTime.UtcNow && cacheEntry.Item2 is byte[] bytes)
                {
                    await scope.OutputStream.WriteAsync(bytes);
                    return;
                }
            }

            var dataType = scopedParams.TryGetValue("dataType", out value)
                ? ConvertDataTypeToContentType((string)value)
                : null;

            if (scopedParams.TryGetValue("method", out value) && !((string)value).EqualsIgnoreCase("GET"))
                throw new NotSupportedException($"Only GET requests can be used in {nameof(includeUrlWithCache)} filters in page '{scope.Page.VirtualPath}'");
            if (scopedParams.TryGetValue("data", out value))
                throw new NotSupportedException($"'data' is not supported in {nameof(includeUrlWithCache)} filters in page '{scope.Page.VirtualPath}'");

            var ms = MemoryStreamFactory.GetStream();
            using (ms)
            {
                var captureScope = scope.ScopeWithStream(ms);
                await includeUrl(captureScope, url, options);

                ms.Position = 0;
                var expireAt = DateTime.UtcNow.Add(expireIn);

                var bytes = ms.ToArray();
                Context.ExpiringCache[cacheKey] = cacheEntry = Tuple.Create(DateTime.UtcNow.Add(expireIn),(object)bytes);
                await scope.OutputStream.WriteAsync(bytes);
            }
        }

        internal IDictionary GetCache(string cacheName)
        {
            switch (cacheName)
            {
                case "Cache":
                    return Context.Cache;
                case "ExpiringCache":
                    return Context.ExpiringCache;
                case "BinderCache":
                    return Context.BinderCache;
                case "AssignExpressionCache":
                    return Context.AssignExpressionCache;
                case "PathMappings":
                    return Context.PathMappings;
            }
            return null;
        }

        public object cacheClear(TemplateScopeContext scope, object cacheNames)
        {
            List<string> caches;
            if (cacheNames is string strName)
            {
                caches = strName.EqualsIgnoreCase("all")
                    ? new List<string> {"Cache", "ExpiringCache", "BinderCache", "AssignExpressionCache", "PathMappings"}
                    : new List<string> {strName};
            }
            else if (cacheNames is IEnumerable<string> nameList)
            {
                caches = new List<string>(nameList);
            }
            else throw new NotSupportedException(nameof(cacheClear) + 
                 " expects a cache name or list of cache names but received: " + (cacheNames.GetType()?.Name ?? "null"));

            int entriesRemoved = 0;
            foreach (var cacheName in caches)
            {
                var cache = GetCache(cacheName);
                if (cache == null)
                    throw new NotSupportedException(nameof(cacheClear) + $": Unknown cache '{cacheName}'");

                entriesRemoved += cache.Count;
                cache.Clear();
            }

            return entriesRemoved;
        }
    }
}