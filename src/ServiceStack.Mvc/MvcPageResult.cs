using System.IO;
using System.Threading.Tasks;
using ServiceStack.Script;

namespace ServiceStack.Mvc
{
#if !NETSTANDARD
    public class MvcPageResult : System.Web.Mvc.ActionResult
    {
        private readonly PageResult pageResult;
        private readonly Stream contents;

        public MvcPageResult(PageResult pageResult, Stream contents)
        {
            this.pageResult = pageResult;
            this.contents = contents;
        }

        public override void ExecuteResult(System.Web.Mvc.ControllerContext context)
        {
            foreach (var entry in pageResult.Options)
            {
                if (entry.Key == HttpHeaders.ContentType)
                    context.HttpContext.Response.ContentType = entry.Value;
                else
                    context.HttpContext.Response.Headers[entry.Key] = entry.Value;
            }

            contents.WriteTo(context.HttpContext.Response.OutputStream);
        }
    }
#else
    public class MvcPageResult : Microsoft.AspNetCore.Mvc.ActionResult
    {
        readonly PageResult pageResult;

        public MvcPageResult(PageResult pageResult) => this.pageResult = pageResult;

        public override async Task ExecuteResultAsync(Microsoft.AspNetCore.Mvc.ActionContext context)
        {
            foreach (var entry in pageResult.Options)
            {
                if (entry.Key == HttpHeaders.ContentType)
                    context.HttpContext.Response.ContentType = entry.Value;
                else
                    context.HttpContext.Response.Headers[entry.Key] = entry.Value;
            }

            await pageResult.WriteToAsync(context.HttpContext.Response.Body);
        }
    }
#endif    
    
    public static class MvcPageResultExtensions
    {
#if !NETSTANDARD
        public static async Task<MvcPageResult> ToMvcResultAsync(this PageResult pageResult)
        {
            var ms = new MemoryStream();            
            await pageResult.WriteToAsync(ms);
            return new MvcPageResult(pageResult, ms);
        }
#else
        public static MvcPageResult ToMvcResult(this PageResult pageResult) => new MvcPageResult(pageResult);        
#endif    
    }
}