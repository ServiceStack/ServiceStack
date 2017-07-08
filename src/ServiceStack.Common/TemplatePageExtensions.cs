using System.IO;
using System.Threading.Tasks;
using ServiceStack.Text;
using ServiceStack.Web;

namespace ServiceStack
{
    public static class TemplatePageExtensions
    {
        public static async Task<string> RenderToStringAsync(this IStreamWriterAsync writer)
        {
            using (var ms = MemoryStreamFactory.GetStream())
            {
                await writer.WriteToAsync(ms);
                ms.Position = 0;
                using (var reader = new StreamReader(ms))
                {
                    return await reader.ReadToEndAsync();
                }
            }
        }
    }
}