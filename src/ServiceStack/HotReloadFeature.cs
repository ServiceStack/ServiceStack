using System;
using System.Threading.Tasks;
using ServiceStack.DataAnnotations;

namespace ServiceStack
{
    /// <summary>
    /// Back-end Service used by /js/hot-fileloader.js to detect file changes in /wwwroot and auto reload page.
    /// </summary>
    public class HotReloadFeature : IPlugin
    {
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
        public static TimeSpan LongPollDuration = TimeSpan.FromSeconds(60);
        public static TimeSpan CheckDelay = TimeSpan.FromMilliseconds(50);

        public async Task<HotReloadPageResponse> Any(HotReloadFiles request)
        {
            var pattern = request.Pattern ?? "*";

            var startedAt = DateTime.UtcNow;
            var maxLastModified = DateTime.MinValue;
            var shouldReload = false;

            while (DateTime.UtcNow - startedAt < LongPollDuration)
            {
                maxLastModified = DateTime.MinValue;
                var files = VirtualFileSources.GetAllMatchingFiles(pattern);
                foreach (var file in files)
                {
                    file.Refresh();
                    if (file.LastModified > maxLastModified)
                        maxLastModified = file.LastModified;
                }

                if (string.IsNullOrEmpty(request.ETag))
                    return new HotReloadPageResponse { ETag = maxLastModified.Ticks.ToString() };

                shouldReload = maxLastModified != DateTime.MinValue && maxLastModified.Ticks > long.Parse(request.ETag);
                if (shouldReload)
                    break;

                await Task.Delay(CheckDelay);
            }

            return new HotReloadPageResponse { Reload = shouldReload, ETag = maxLastModified.Ticks.ToString() };
        }
    }
}