using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Web;
using ServiceStack.Host.Handlers;
using ServiceStack.Html;
using ServiceStack.Logging;
using ServiceStack.Text;
using ServiceStack.Web;

namespace ServiceStack.Razor.Managers
{
    public delegate string RenderPartialDelegate(string pageName, object model, bool renderHtml, StreamWriter writer = null, HtmlHelper htmlHelper = null, IRequest httpReq = null);

    /// <summary>
    /// A common hook into ServiceStack and the hosting infrastructure used to resolve requests.
    /// </summary>
    public class RazorPageResolver : ServiceStackHandlerBase, IViewEngine
    {
        public static ILog Log = LogManager.GetLogger(typeof(RazorPageResolver));

        public const string ViewKey = "View";
        public const string LayoutKey = "Template";
        public const string QueryStringFormatKey = "format";
        public const string NoTemplateFormatValue = "bare";
        public const string DefaultLayoutName = "_Layout";

        private static readonly UTF8Encoding UTF8EncodingWithoutBom = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);

        private readonly IRazorConfig config;
        private readonly RazorViewManager viewManager;
        public RenderPartialDelegate RenderPartialFn { get; set; }

        public RazorPageResolver(IRazorConfig config, RazorViewManager viewManager)
        {
            this.RequestName = "Razor_PageResolver";

            this.config = config;
            this.viewManager = viewManager;
        }

        public IHttpHandler CatchAllHandler(string httpmethod, string pathInfo, string filepath)
        {
            //does not have a .cshtml extension
            var ext = Path.GetExtension(pathInfo);
            if (!string.IsNullOrEmpty(ext) && ext != config.RazorFileExtension)
                return null;

            //pathInfo on dir doesn't match existing razor page
            if (string.IsNullOrEmpty(ext) && viewManager.GetContentPage(pathInfo) == null)
                return null;

            //if there is any denied predicates for the path, return nothing
            if (this.config.Deny.Any(denined => denined(pathInfo))) return null;

            //Redirect for /default.cshtml => / or /page.cshtml => /page
            if (pathInfo.EndsWith(config.RazorFileExtension))
            {
                pathInfo = pathInfo.EndsWithIgnoreCase(config.DefaultPageName)
                    ? pathInfo.Substring(0, pathInfo.Length - config.DefaultPageName.Length)
                    : pathInfo.WithoutExtension();

                var webHostUrl = config.WebHostUrl;
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
        public override void ProcessRequest(IRequest httpReq, IResponse httpRes, string operationName)
        {
            HostContext.ApplyCustomHandlerRequestFilters(httpReq, httpRes);
            if (httpRes.IsClosed) return;

            httpRes.ContentType = MimeTypes.Html;

            var contentPage = ResolveContentPage(httpReq);
            using (ExecuteRazorPage(httpReq, httpRes, null, contentPage)) 
            { 
                httpRes.EndRequest(skipHeaders: true);
            }
        }

        /// <summary>
        /// Called by the HtmlFormat:IPlugin who checks to see if any registered view engines can handle the response DTO.
        /// If this view engine can handle the response DTO, then process it, otherwise, returning false will
        /// allow another view engine to attempt to process it. If no view engines can process the DTO,
        /// HtmlFormat will simply handle it itself.
        /// </summary>
        public virtual bool ProcessRequest(IRequest httpReq, IResponse httpRes, object dto)
        {
            //for compatibility
            var httpResult = dto as IHttpResult;
            if (httpResult != null)
                dto = httpResult.Response;

            var existingRazorPage = ResolveViewPage(httpReq, dto)
                ?? ResolveContentPage(httpReq);
            if (existingRazorPage == null)
            {
                return false;
            }

            using (ExecuteRazorPage(httpReq, httpRes, dto, existingRazorPage))
            {
                httpRes.EndRequest(skipHeaders:true);
                return true;    
            }
        }

        public RazorPage ResolveContentPage(IRequest httpReq)
        {
            var viewName = httpReq.GetItem(ViewKey) as string;
            if (viewName != null)
                return viewManager.GetContentPage(viewName);

            return viewManager.GetContentPage(httpReq.PathInfo);
        }

        public RazorPage ResolveViewPage(IRequest httpReq, object model)
        {
            var viewName = httpReq.GetItem(ViewKey) as string;
            if (viewName != null)
                return viewManager.GetViewPage(viewName);

            return viewManager.GetViewPage(httpReq.OperationName) // Request DTO
                   ?? viewManager.GetViewPage(model.GetType().Name); // Response DTO
        }

        public IRazorView ExecuteRazorPage(IRequest httpReq, IResponse httpRes, object model, RazorPage razorPage)
        {
            if (razorPage == null)
            {
                httpRes.StatusCode = (int)HttpStatusCode.NotFound;
                return null;
            }

            var page = CreateRazorPageInstance(httpReq, httpRes, model, razorPage);

            var includeLayout = !(httpReq.GetParam(QueryStringFormatKey) ?? "").Contains(NoTemplateFormatValue);
            if (includeLayout)
            {
                var result = ExecuteRazorPageWithLayout(razorPage, httpReq, httpRes, model, page, () =>
                    httpReq.GetItem(LayoutKey) as string
                    ?? page.Layout
                    ?? DefaultLayoutName);

                if (httpRes.IsClosed)
                    return result != null ? result.Item1 : null;

                var layoutWriter = new StreamWriter(httpRes.OutputStream, UTF8EncodingWithoutBom);
                layoutWriter.Write(result.Item2);
                layoutWriter.Flush();
                return result.Item1;
            }

            var writer = new StreamWriter(httpRes.OutputStream, UTF8EncodingWithoutBom);
            page.WriteTo(writer);
            writer.Flush();
            return page;
        }

        private Tuple<IRazorView, string> ExecuteRazorPageWithLayout(RazorPage razorPage, IRequest httpReq, IResponse httpRes, object model, IRazorView pageInstance, Func<string> layout)
        {
            using (var ms = MemoryStreamFactory.GetStream())
            {
                var childWriter = new StreamWriter(ms, UTF8EncodingWithoutBom); //ms disposed in using
                //child page needs to execute before master template to populate ViewBags, sections, etc
                try
                {
                    pageInstance.WriteTo(childWriter);
                }
                catch (StopExecutionException ignore) { }

                if (httpRes.IsClosed)
                    return null;

                var childBody = ms.ToArray().FromUtf8Bytes();

                var layoutName = layout();
                if (!string.IsNullOrEmpty(layoutName))
                {
                    var layoutPage = viewManager.GetLayoutPage(layoutName, razorPage, httpReq, model);
                    if (layoutPage != null)
                    {
                        var layoutView = CreateRazorPageInstance(httpReq, httpRes, model, layoutPage);
                        layoutView.SetChildPage(pageInstance, childBody);
                        return ExecuteRazorPageWithLayout(layoutPage, httpReq, httpRes, model, layoutView, () => layoutView.Layout);
                    }
                }

                return Tuple.Create(pageInstance, childBody);
            }
        }

        private IRazorView CreateRazorPageInstance(IRequest httpReq, IResponse httpRes, object dto, RazorPage razorPage)
        {
            viewManager.EnsureCompiled(razorPage);

            //don't proceed any further, the background compiler found there was a problem compiling the page, so throw instead
            if (razorPage.CompileException != null)
            {
                if (Text.Env.IsMono)
                {
                    //Additional debug info Working around not displaying default exception in IHttpAsyncHandler
                    var errors = razorPage.CompileException.Results.Errors;
                    for (var i = 0; i < errors.Count; i++)
                    {
                        var error = errors[i];
                        Log.Debug("{0} Line: {1}:{2}:".Fmt(error.FileName, error.Line, error.Column));
                        Log.Debug("{0}: {1}".Fmt(error.ErrorNumber, error.ErrorText));
                    }
                } 
                throw razorPage.CompileException;
            }

            //else, EnsureCompiled() ensures we have a page type to work with so, create an instance of the page
            var page = (IRazorView)razorPage.ActivateInstance();

            page.Init(viewEngine: this, httpReq: httpReq, httpRes: httpRes);

            //deserialize the model.
            PrepareAndSetModel(page, httpReq, dto);

            return page;
        }

        private void PrepareAndSetModel(IRazorView page, IRequest httpReq, object dto)
        {
            var hasModel = page as IHasModel;
            if (hasModel == null) return;

            if (hasModel.ModelType == typeof(DynamicRequestObject))
                dto = new DynamicRequestObject(httpReq, dto);

            var model = dto ?? DeserializeHttpRequest(hasModel.ModelType, httpReq, httpReq.ContentType);

            if (model.GetType().IsAnonymousType())
            {
                model = new DynamicRequestObject(httpReq, model);
            }

            hasModel.SetModel(model);
        }

        public override object CreateRequest(IRequest request, string operationName)
        {
            return null;
        }

        public override object GetResponse(IRequest httpReq, object request)
        {
            return null;
        }

        public bool HasView(string viewName, IRequest httpReq = null)
        {
            throw new NotImplementedException();
        }

        public virtual string RenderPartial(string pageName, object model, bool renderHtml, StreamWriter writer, HtmlHelper htmlHelper)
        {
            var httpReq = htmlHelper.HttpRequest;
            var razorPage = viewManager.GetPartialPage(httpReq, pageName);
            if (razorPage != null)
            {
                var page = CreateRazorPageInstance(httpReq, htmlHelper.HttpResponse, model, razorPage);
                page.ParentPage = htmlHelper.RazorPage;
                page.WriteTo(writer);
            }
            else
            {
                if (RenderPartialFn != null)
                {
                    RenderPartialFn(pageName, model, renderHtml, writer, htmlHelper, httpReq);
                }
                else
                {
                    writer.Write("<!--No RenderPartialFn, skipping {0}-->".Fmt(pageName));
                }
            }
            return null;
        }
    }

}