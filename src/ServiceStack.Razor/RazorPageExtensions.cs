using System.IO;
using ServiceStack.Html;
using ServiceStack.Text;

namespace ServiceStack.Razor
{
    public static class RazorPageExtensions
    {
         public static string RenderSectionToHtml(this IRazorView razorView, string sectionName)
         {
             using (var ms = new MemoryStream())
             using (var writer = new StreamWriter(ms))
             {
                 razorView.RenderSection(sectionName, writer);
                 writer.Flush();
                 return ms.ToArray().FromUtf8Bytes();
             }
         }
    }
}