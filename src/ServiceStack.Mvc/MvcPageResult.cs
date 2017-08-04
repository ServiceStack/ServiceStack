using System.Threading.Tasks;
using ServiceStack.Templates;

namespace ServiceStack.Mvc
{
#if !NETSTANDARD1_6
    public class MvcPageResult : System.Web.Mvc.ActionResult
    {
        readonly PageResult pageResult;

        public MvcPageResult(PageResult pageResult) => this.pageResult = pageResult;

        public override async void ExecuteResult(System.Web.Mvc.ControllerContext context)
        {
            foreach (var entry in pageResult.Options)
            {
                if (entry.Key == HttpHeaders.ContentType)
                    context.HttpContext.Response.ContentType = entry.Value;
                else
                    context.HttpContext.Response.Headers[entry.Key] = entry.Value;
            }
            
            await pageResult.WriteToAsync(context.HttpContext.Response.OutputStream);
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
        public static MvcPageResult ToMvcResult(this PageResult pageResult) => new MvcPageResult(pageResult);        
    }
}