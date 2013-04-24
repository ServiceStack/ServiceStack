using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Web;
using ServiceStack.Common.Utils;
using ServiceStack.Common.Web;
using ServiceStack.Html;
using ServiceStack.IO;
using ServiceStack.ServiceHost;
using ServiceStack.WebHost.Endpoints;
using ServiceStack.WebHost.Endpoints.Extensions;
using ServiceStack.WebHost.Endpoints.Support;

namespace ServiceStack.Razor2.Managers
{
    /// <summary>
    /// A common hook into ServiceStack and the hosting infrastructure used to resolve requests.
    /// </summary>
    public class PageResolver : EndpointHandlerBase, IViewEngine
    {
        private IAppHost appHost;
        private PageResolverConfig Config;
        private ViewManager viewManager;
        private BuildManager buildManager;
        private IVirtualPathProvider pathProvider;

        public PageResolver( IAppHost appHost, PageResolverConfig pageResolverConfig, ViewManager viewManager, BuildManager buildManager )
        {
            this.RequestName = "Razor_PageResolver";

            this.appHost = appHost;
            this.pathProvider = appHost.VirtualPathProvider;

            this.Config = pageResolverConfig;
            this.viewManager = viewManager;
            this.buildManager = buildManager;

            this.appHost.CatchAllHandlers.Add( OnCatchAll );
            this.appHost.ViewEngines.Add( this );
        }

        private IHttpHandler OnCatchAll( string httpmethod, string pathinfo, string filepath )
        {
            //if there is any denied predicates for the path, return nothing
            if ( this.Config.Deny.Any( denined => denined( pathinfo ) ) ) return null;

            //only return "this" when we can, indeed, handle the request.
            return this;
        }

        /// <summary>
        /// This is called by the hosting environment via CatchAll usually for content pages.
        /// </summary>
        public override void ProcessRequest( IHttpRequest httpReq, IHttpResponse httpRes, string operationName )
        {
            httpRes.ContentType = ContentType.Html;

            ResolveAndExecuteRazorPage(httpReq, httpRes, null);

            httpRes.EndServiceStackRequest( skipHeaders: true );
        }

        /// <summary>
        /// Called by the HtmlFormat:IPlugin who checks to see if any registered view engines can handle the response DTO.
        /// If this view engine can handle the response DTO, then process it, otherwise, returning false will
        /// allow another view engine to attempt to process it. If no view engines can process the DTO,
        /// HtmlFormat will simply handle it itself.
        /// </summary>
        public virtual bool ProcessRequest( IHttpRequest request, IHttpResponse response, object dto )
        {
            //for compatibility
            var httpResult = dto as IHttpResult;
            if ( httpResult != null )
                dto = httpResult.Response;

            ResolveAndExecuteRazorPage( request, response, dto );

            response.EndServiceStackRequest();
            return true;
        }

        private void ResolveAndExecuteRazorPage( IHttpRequest request, IHttpResponse response, object dto )
        {
            var path = NormalizePath( request, dto );

            var razorPage = this.viewManager.GetRazorView( path );

            if ( razorPage == null )
            {
                response.StatusCode = (int)HttpStatusCode.NotFound;
                response.EndHttpRequest();
                return;
            }

            buildManager.EnsureCompiled( razorPage, response );

            //don't proceed any further, the background compiler
            //found there was a problem compiling the page, so throw
            //that compilation error.
            if ( razorPage.CompileException != null )
            {
                throw razorPage.CompileException;
            }

            //else, EnsureCompiled() ensures we have 
            //a page type to work with so, create an instance of the page
            var page = razorPage.ActivateInstance();

            //poor man's IOC -- for the razor pages.
            page.Request = request;
            page.Response = response;
            page.Output = new StreamWriter( page.Response.OutputStream );

            //deserialize the model.
            PrepareAndSetModel( page, request, dto );

            //execute the page.
            page.Execute();
            page.Output.Flush();
        }

        private void PrepareAndSetModel( RenderingPage page, IHttpRequest request, object dto )
        {
            var hasModel = page as IHasModel;
            if ( hasModel == null ) return;

            var model = dto ?? DeserializeHttpRequest( hasModel.ModelType, request, request.ContentType );

            hasModel.SetModel( model );
        }


        private string NormalizePath(IHttpRequest request, object dto)
        {
            if ( dto != null ) // this is for a view inside /views
            {
                //if we have a view name, use it.
                var viewName = request.GetView();

                if( string.IsNullOrWhiteSpace( viewName ) )
                {
                    //use the response DTO name
                    viewName = dto.GetType().Name;
                }
                if ( string.IsNullOrWhiteSpace(viewName) )
                {
                    //the request use the request DTO name.
                    viewName = request.OperationName;
                }

                return "/"+ PathUtils.CombinePaths("views", Path.ChangeExtension( viewName, ".cshtml" ) );
            }

            //content page path.

            var path = request.PathInfo;

            if ( path == "/" )
            {
                return path + this.Config.DefaultPageName;
            }

            return Path.ChangeExtension( path, ".cshtml" );
        }

        public override object CreateRequest( IHttpRequest request, string operationName )
        {
            return null;
        }

        public override object GetResponse( IHttpRequest httpReq, IHttpResponse httpRes, object request )
        {
            return null;
        }

        public bool HasView( string viewName, IHttpRequest httpReq = null )
        {
            throw new NotImplementedException();
        }

        public virtual string RenderPartial( string pageName, object model, bool renderHtml, HtmlHelper htmlHelper = null )
        {
            return null;
        }
    }


    public class PageResolverConfig
    {
        public List<Predicate<string>> Deny = new List<Predicate<string>>();

        public string DefaultPageName = "default.cshtml";

        public PageResolverConfig()
        {
            this.Deny.Add( DenyPathsWithLeading_ );
        }

        public virtual bool DenyPathsWithLeading_( string path )
        {
            return Path.GetFileName( path ).StartsWith( "_" );
        }
    }

}