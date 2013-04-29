using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Web;
using ServiceStack.Common;
using ServiceStack.Common.Web;
using ServiceStack.DataAnnotations;
using ServiceStack.Html;
using ServiceStack.ServiceHost;
using ServiceStack.Text;
using ServiceStack.WebHost.Endpoints;
using ServiceStack.WebHost.Endpoints.Extensions;
using ServiceStack.WebHost.Endpoints.Support;

namespace ServiceStack.Razor.Managers
{
    /// <summary>
    /// A common hook into ServiceStack and the hosting infrastructure used to resolve requests.
    /// </summary>
    public class PageResolver : EndpointHandlerBase, IViewEngine
    {
        public const string DefaultLayoutName = "_Layout";

        private static readonly UTF8Encoding UTF8EncodingWithoutBom = new UTF8Encoding(encoderShouldEmitUTF8Identifier:false);

        private readonly IAppHost appHost;
        private readonly IRazorConfig config;
        private readonly ViewManager viewManager;
        private readonly BuildManager buildManager;

        public PageResolver(IAppHost appHost, IRazorConfig config, ViewManager viewManager, BuildManager buildManager)
        {
            this.RequestName = "Razor_PageResolver";

            this.appHost = appHost;

            this.config = config;
            this.viewManager = viewManager;
            this.buildManager = buildManager;

            this.appHost.CatchAllHandlers.Add(OnCatchAll);
            this.appHost.ViewEngines.Add(this);
        }

        private IHttpHandler OnCatchAll(string httpmethod, string pathInfo, string filepath)
        {
            //does not have a .cshtml extension
            var ext = Path.GetExtension(pathInfo);
            if (!string.IsNullOrEmpty(ext) && ext != config.RazorFileExtension)
                return null;

            //pathInfo on dir doesn't match existing razor page
            if (string.IsNullOrEmpty(ext) && viewManager.GetRazorViewByPathInfo(pathInfo) == null)
                return null;

            //if there is any denied predicates for the path, return nothing
            if (this.config.Deny.Any(denined => denined(pathInfo))) return null;

            //Redirect for /default.cshtml => / or /page.cshtml => /page
            if (pathInfo.EndsWith(config.RazorFileExtension))
            {
                pathInfo = pathInfo.EndsWithIgnoreCase(config.DefaultPageName)
                    ? pathInfo.Substring(0, pathInfo.Length - config.DefaultPageName.Length)
                    : pathInfo.WithoutExtension();

                var webHostUrl = appHost.Config.WebHostUrl;
                return new RedirectHttpHandler
                {
                    AbsoluteUrl = webHostUrl.IsNullOrEmpty()
                        ? null
                        : webHostUrl.CombineWith(pathInfo),
                    RelativeUrl = webHostUrl.IsNullOrEmpty()
                        ? pathInfo
                        : null
                };
            }

            //only return "this" when we can, indeed, handle the httpReq.
            return this;
        }

        /// <summary>
        /// This is called by the hosting environment via CatchAll usually for content pages.
        /// </summary>
        public override void ProcessRequest(IHttpRequest httpReq, IHttpResponse httpRes, string operationName)
        {
            httpRes.ContentType = ContentType.Html;

            ResolveAndExecuteRazorPage(httpReq, httpRes, null);
            httpRes.EndServiceStackRequest(skipHeaders: true);
        }

        /// <summary>
        /// Called by the HtmlFormat:IPlugin who checks to see if any registered view engines can handle the response DTO.
        /// If this view engine can handle the response DTO, then process it, otherwise, returning false will
        /// allow another view engine to attempt to process it. If no view engines can process the DTO,
        /// HtmlFormat will simply handle it itself.
        /// </summary>
        public virtual bool ProcessRequest(IHttpRequest request, IHttpResponse response, object dto)
        {
            //for compatibility
            var httpResult = dto as IHttpResult;
            if (httpResult != null)
                dto = httpResult.Response;

            ResolveAndExecuteRazorPage(request, response, dto);

            response.EndServiceStackRequest();
            return true;
        }

        public void ResolveAndExecuteRazorPage(IHttpRequest httpReq, IHttpResponse httpRes, object dto, RazorPage razorView=null)
        {
            if (razorView == null)
            {
                var viewName = httpReq.GetItem("View") as string;
                razorView = viewName != null ? this.viewManager.GetRazorViewByName(viewName) : null;
            }
            if (razorView == null)
            {
                razorView = this.viewManager.GetRazorView(httpReq, dto);

                if (razorView == null)
                {
                    httpRes.StatusCode = (int)HttpStatusCode.NotFound;
                    return;
                }
            }
            
            using (var writer = new StreamWriter(httpRes.OutputStream, UTF8EncodingWithoutBom))
            {
                var page = CreateRazorPageInstance(httpReq, httpRes, dto, razorView, writer);
                var layoutAttr = page.GetType().GetCustomAttributes(typeof(MetaAttribute), true)
                    .Cast<MetaAttribute>()
                    .FirstOrDefault(x => x.Name == "Layout");

                var layout = httpReq.GetItem("Template") as string
                    ?? (layoutAttr != null ? layoutAttr.Value : null) 
                    ?? DefaultLayoutName;

                var includeLayout = !(httpReq.GetParam("format") ?? "").Contains("bare");
                if (includeLayout)
                {
                    var layoutView = this.viewManager.GetRazorViewByName(layout, httpReq, dto);
                    var layoutPage = CreateRazorPageInstance(httpReq, httpRes, dto, layoutView, writer);
                    layoutPage.ChildPage = page;
                    layoutPage.WriteTo(writer);
                }
                else
                {
                    page.WriteTo(writer);                    
                }
            }
        }

        private IRazorViewPage ExecuteRazorPage(IHttpRequest request, IHttpResponse response, object dto, RazorPage razorPage, StreamWriter writer)
        {
            var page = CreateRazorPageInstance(request, response, dto, razorPage, writer);

            //execute the page.
            page.WriteTo(writer);

            return page;
        }

        private IRazorViewPage CreateRazorPageInstance(IHttpRequest request, IHttpResponse response, object dto, RazorPage razorPage, StreamWriter writer)
        {
            buildManager.EnsureCompiled(razorPage, response);

            //don't proceed any further, the background compiler found there was a problem compiling the page, so throw instead
            if (razorPage.CompileException != null)
            {
                throw razorPage.CompileException;
            }

            //else, EnsureCompiled() ensures we have a page type to work with so, create an instance of the page
            var page = (IRazorViewPage) razorPage.ActivateInstance();

            page.Init(viewEngine: this, httpReq: request, httpRes: response, writer: writer);

            //deserialize the model.
            PrepareAndSetModel(page, request, dto);
        
            return page;
        }

        private void PrepareAndSetModel(IRazorViewPage page, IHttpRequest httpReq, object dto)
        {
            var hasModel = page as IHasModel;
            if (hasModel == null) return;

            if (hasModel.ModelType == typeof (DynamicRequestObject))
                dto = new DynamicRequestObject(httpReq);

            var model = dto ?? DeserializeHttpRequest(hasModel.ModelType, httpReq, httpReq.ContentType);

            hasModel.SetModel(model);
        }

        public override object CreateRequest(IHttpRequest request, string operationName)
        {
            return null;
        }

        public override object GetResponse(IHttpRequest httpReq, IHttpResponse httpRes, object request)
        {
            return null;
        }

        public bool HasView(string viewName, IHttpRequest httpReq = null)
        {
            throw new NotImplementedException();
        }

        public virtual string RenderPartial(string pageName, object model, bool renderHtml, StreamWriter writer, HtmlHelper htmlHelper = null)
        {
            var httpReq = htmlHelper.HttpRequest;
            var razorPage = this.viewManager.GetRazorViewByName(pageName, httpReq, model);
            if (razorPage != null)
            {
                ExecuteRazorPage(httpReq, htmlHelper.HttpResponse, model, razorPage, writer);
            }
            else
            {
                foreach (var viewEngine in appHost.ViewEngines)
                {
                    if (viewEngine == this || !viewEngine.HasView(pageName, httpReq)) continue;
                    viewEngine.RenderPartial(pageName, model, renderHtml, writer, htmlHelper);
                }
                writer.Write("<!--{0} not found-->".Fmt(pageName));
            }
            return null;
        }

    }

}