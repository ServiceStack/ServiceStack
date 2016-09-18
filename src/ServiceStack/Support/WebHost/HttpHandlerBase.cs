using System;
using System.Web;
using ServiceStack.Host.Handlers;
using ServiceStack.Logging;

namespace ServiceStack.Support.WebHost
{
    public abstract class HttpHandlerBase : HttpAsyncTaskHandler, IHttpHandler
    {
        private readonly ILog log;

        protected HttpHandlerBase()
        {
            this.log = LogManager.GetLogger(this.GetType());
        }

#if !NETSTANDARD1_6
        public override void ProcessRequest(HttpContextBase context)
        {
            var before = DateTime.UtcNow;
            Execute(context);
            var elapsed = DateTime.UtcNow - before;
            if (Log.IsDebugEnabled)
                log.DebugFormat($"'{GetType().GetOperationName()}' was completed in {elapsed.TotalMilliseconds}ms");
        }

        public abstract void Execute(HttpContextBase context);
#endif

        public override bool IsReusable => false;
    }
}