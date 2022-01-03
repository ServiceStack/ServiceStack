using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ServiceStack.DataAnnotations;
using ServiceStack.IO;
using ServiceStack.Text;

namespace ServiceStack
{
    /// <summary>
    /// Back-end Service used by /js/hot-fileloader.js to detect file changes in /wwwroot and auto reload page.
    /// </summary>
    public class HotReloadFeature : IPlugin, Model.IHasStringId
    {
        public string Id { get; set; } = Plugins.HotReload;
        public IVirtualPathProvider VirtualFiles
        {
            set => HotReloadFilesService.UseVirtualFiles = value;
            get => HotReloadFilesService.UseVirtualFiles;
        }
        public string DefaultPattern
        {
            set => HotReloadFilesService.DefaultPattern = value;
        }
        
        public void Register(IAppHost appHost)
        {
            appHost.RegisterService(typeof(HotReloadFilesService));
        }
    }

    [ExcludeMetadata]
    [Route("/hotreload/files")]
    public class HotReloadFiles : IReturn<HotReloadPageResponse>
    {
        public string Pattern { get; set; }
        public string ETag { get; set; }
    }

    [DefaultRequest(typeof(HotReloadFiles))]
    [Restrict(VisibilityTo = RequestAttributes.None)]
    public class HotReloadFilesService : Service
    {
        public static IVirtualPathProvider UseVirtualFiles { get; set; }
        public static string DefaultPattern { get; set; } = "*";
        
        public static List<string> ExcludePatterns { get; } = new List<string> {
            "*.sqlite",
            "*.db",
            "*.cs",  //monitored by dotnet watch
            "*.ts",  //watch on generated .js instead
            "*.log", //exclude log files
            "*.csv",
        };
        
        public static TimeSpan LongPollDuration = TimeSpan.FromSeconds(60);
        public static TimeSpan CheckDelay = TimeSpan.FromMilliseconds(50);
        // No delay sometimes causes repetitive loop 
        public static TimeSpan ModifiedDelay = TimeSpan.FromMilliseconds(50);

        public async Task<HotReloadPageResponse> Any(HotReloadFiles request)
        {
            var vfs = UseVirtualFiles ?? VirtualFileSources;
            // Remove embedded ResourceVirtualFiles from scan list
            if (vfs is MultiVirtualFiles multiVfs)
                vfs = new MultiVirtualFiles(multiVfs.ChildProviders.Where(x => !(x is ResourceVirtualFiles)).ToArray());

            var startedAt = DateTime.UtcNow;
            var maxLastModified = DateTime.MinValue;
            IVirtualFile maxLastFile = null;
            var shouldReload = false;

            while (DateTime.UtcNow - startedAt < LongPollDuration)
            {
                maxLastModified = DateTime.MinValue;

                var patterns = (!string.IsNullOrEmpty(request.Pattern) 
                    ? request.Pattern 
                    : DefaultPattern).Split(';');
                
                foreach (var pattern in patterns)
                {
                    var files = vfs.GetAllMatchingFiles(pattern.Trim()).ToList();
                    foreach (var file in files)
                    {
                        if (ExcludePatterns.Any(exclude => file.Name.Glob(exclude)))
                            continue;
                    
                        file.Refresh();
                        if (file.LastModified > maxLastModified)
                        {
                            maxLastModified = file.LastModified;
                            maxLastFile = file;
                        }
                    }
                }

                if (string.IsNullOrEmpty(request.ETag))
                    return new HotReloadPageResponse { ETag = maxLastModified.Ticks.ToString() };

                shouldReload = maxLastModified != DateTime.MinValue && maxLastModified.Ticks > long.Parse(request.ETag);
                if (shouldReload)
                {
                    await Task.Delay(ModifiedDelay).ConfigAwait();
                    break;
                }

                await Task.Delay(CheckDelay).ConfigAwait();
            }

            return new HotReloadPageResponse {
                Reload = shouldReload, 
                ETag = maxLastModified.Ticks.ToString(),
                LastUpdatedPath = maxLastFile?.VirtualPath,
            };
        }
    }
}