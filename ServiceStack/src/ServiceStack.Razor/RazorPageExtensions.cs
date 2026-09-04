using System.IO;
using System.Text;
using ServiceStack.Html;
using ServiceStack.Text;

namespace ServiceStack.Razor
{
    public static class RazorPageExtensions
    {
        private static readonly UTF8Encoding UTF8EncodingWithoutBom = new UTF8Encoding(false);

        public static string RenderSectionToHtml(this IRazorView razorView, string sectionName)
        {
            using (var ms = MemoryStreamFactory.GetStream())
            {
                using (var writer = new StreamWriter(ms, UTF8EncodingWithoutBom, 1024, leaveOpen: true))
                {
                    razorView.RenderChildSection(sectionName, writer);
                    writer.Flush();
                }
                return ms.ReadToEnd();
            }
        }
    }
}