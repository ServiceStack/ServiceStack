using ServiceStack.VirtualPath;
using ServiceStack.WebHost.Endpoints.Formats;
using ServiceStack.WebHost.Endpoints.Support.Markdown;

namespace ServiceStack.ServiceHost.Tests.Formats
{
    public static class MarkdownFormatExtensions
    {
        public static void AddFileAndPage(this MarkdownFormat markdown, MarkdownPage markdownPage)
        {
            var pathProvider = (InMemoryVirtualPathProvider)markdown.VirtualPathProvider;
            pathProvider.AddFile(markdownPage.FilePath, markdownPage.Contents);
            markdown.AddPage(markdownPage);
        }

        public static void AddFileAndTemplate(this MarkdownFormat markdown, string filePath, string contents)
        {
            var pathProvider = (InMemoryVirtualPathProvider)markdown.VirtualPathProvider;
            pathProvider.AddFile(filePath, contents);
            markdown.AddTemplate(filePath, contents);
        }
    }
}