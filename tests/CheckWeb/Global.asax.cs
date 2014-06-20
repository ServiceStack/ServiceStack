using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Net;
using System.Web;
using Check.ServiceInterface;
using Funq;
using ServiceStack;
using ServiceStack.Api.Swagger;
using ServiceStack.MiniProfiler;
using ServiceStack.Razor;
using ServiceStack.Text;
using ServiceStack.Validation;

namespace CheckWeb
{
    public class AppHost : AppHostBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AppHost"/> class.
        /// </summary>
        public AppHost()
            : base("CheckWeb", typeof(ErrorsService).Assembly) {}

        /// <summary>
        /// Configure the Web Application host.
        /// </summary>
        /// <param name="container">The container.</param>
        public override void Configure(Container container)
        {
            this.CustomErrorHttpHandlers[HttpStatusCode.NotFound] = null;

            // Change ServiceStack configuration
            this.SetConfig(new HostConfig
            {
                AppendUtf8CharsetOnContentTypes = new HashSet<string> { MimeTypes.Html },

                // Set to return JSON if no request content type is defined
                // e.g. text/html or application/json
                //DefaultContentType = MimeTypes.Json,
#if !DEBUG
                // Show StackTraces in service responses during development
                DebugMode = true,
#endif
                // Disable SOAP endpoints
                EnableFeatures = Feature.All.Remove(Feature.Soap)
            });

            container.Register<IServiceClient>(c =>
                new JsonServiceClient("http://localhost:55799/") {
                    CaptureSynchronizationContext = true,
                });

            // Configure JSON serialization properties.
            this.ConfigureSerialization(container);

            // Configure ServiceStack database connections.
            this.ConfigureDataConnection(container);

            // Configure ServiceStack Authentication plugin.
            this.ConfigureAuth(container);

            // Configure ServiceStack Fluent Validation plugin.
            this.ConfigureValidation(container);

            // Configure ServiceStack Razor views.
            this.ConfigureView(container);
        }

        /// <summary>
        /// Configure JSON serialization properties.
        /// </summary>
        /// <param name="container">The container.</param>
        private void ConfigureSerialization(Container container)
        {
            // Set JSON web services to return idiomatic JSON camelCase properties
            JsConfig.EmitCamelCaseNames = true;

            // Set JSON web services to return ISO8601 date format
            JsConfig.DateHandler = DateHandler.ISO8601;

            // Exclude type info during serialization as an effect of IoC
            JsConfig.ExcludeTypeInfo = true;
        }

        /// <summary>
        /// // Configure ServiceStack database connections.
        /// </summary>
        /// <param name="container">The container.</param>
        private void ConfigureDataConnection(Container container)
        {
            container.RegisterAutoWiredAs<Echo, IEcho>();

            // ...
        }

        /// <summary>
        /// Configure ServiceStack Authentication plugin.
        /// </summary>
        /// <param name="container">The container.</param>
        private void ConfigureAuth(Container container)
        {
            // ...
        }

        /// <summary>
        /// Configure ServiceStack Fluent Validation plugin.
        /// </summary>
        /// <param name="container">The container.</param>
        private void ConfigureValidation(Container container)
        {
            // Provide fluent validation functionality for web services
            Plugins.Add(new ValidationFeature());

            container.RegisterValidators(typeof(AppHost).Assembly);
        }

        /// <summary>
        /// Configure ServiceStack Razor views.
        /// </summary>
        /// <param name="container">The container.</param>
        private void ConfigureView(Container container)
        {
            // Enable ServiceStack Razor
            Plugins.Add(new RazorFormat());

            // Enable support for Swagger API browser
            Plugins.Add(new SwaggerFeature());
            //Plugins.Add(new CorsFeature()); // Uncomment if the services to be available from external sites
        }
    }

    public class Global : System.Web.HttpApplication
    {
        protected void Application_Start(object sender, EventArgs e)
        {
            new AppHost().Init();
        }

        protected void Application_BeginRequest(object src, EventArgs e)
        {
            if (Request.IsLocal)
                Profiler.Start();
        }

        protected void Application_EndRequest(object src, EventArgs e)
        {
            Profiler.Stop();
        }
    }
}