using ServiceStack.Formats;
using ServiceStack.IO;
using ServiceStack.Support.Markdown;

namespace ServiceStack.ServiceHost.Tests.Formats
{
    public static class MarkdownFormatExtensions
    {
        public static void AddFileAndPage(this MarkdownFormat markdown, MarkdownPage markdownPage)
        {
            var pathProvider = (MemoryVirtualFiles)markdown.VirtualPathProvider;
            pathProvider.WriteFile(markdownPage.FilePath, markdownPage.Contents);
            markdown.AddPage(markdownPage);
        }

        public static void AddFileAndTemplate(this MarkdownFormat markdown, string filePath, string contents)
        {
            var pathProvider = (MemoryVirtualFiles)markdown.VirtualPathProvider;
            pathProvider.WriteFile(filePath, contents);
            markdown.AddTemplate(filePath, contents);
        }
    }
}