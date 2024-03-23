using ServiceStack.IO;

[assembly: HostingStartup(typeof(MyApp.ConfigureSsg))]

namespace MyApp;

public class ConfigureSsg : IHostingStartup
{
    public void Configure(IWebHostBuilder builder) => builder
        .ConfigureServices(services => {
            services.AddSingleton<MarkdownIncludes>();
            services.AddSingleton<MarkdownPages>();
            services.AddSingleton<MarkdownVideos>();
            services.AddSingleton<MarkdownBlog>();
            services.AddSingleton<MarkdownMeta>();
        })
        .ConfigureAppHost(afterPluginsLoaded: appHost => {
            MarkdigConfig.Set(new MarkdigConfig
            {
                ConfigurePipeline = pipeline =>
                {
                    // Extend Markdig Pipeline
                },
                ConfigureContainers = config =>
                {
                    config.AddBuiltInContainers();
                    // Add Custom Block or Inline containers
                }
            });

            var includes = appHost.Resolve<MarkdownIncludes>();
            var pages = appHost.Resolve<MarkdownPages>();
            var videos = appHost.Resolve<MarkdownVideos>();
            var blogPosts = appHost.Resolve<MarkdownBlog>();
            var meta = appHost.Resolve<MarkdownMeta>();

            meta.Features = [pages, videos, blogPosts];
            
            includes.LoadFrom("_includes");
            pages.LoadFrom("_pages");
            videos.LoadFrom("_videos");
            blogPosts.LoadFrom("_posts");
        });
}

// Add additional frontmatter info to include
public class MarkdownFileInfo : MarkdownFileBase
{
}
