using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Threading.Tasks;
using System.Web;
using Funq;
using ServiceStack;
using ServiceStack.Api.Swagger;
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
            : base("ServiceStack Test (1) REST API", typeof(AppHost).Assembly)
        {
        }

        /// <summary>
        /// Configure the Web Application host.
        /// </summary>
        /// <param name="container">The container.</param>
        public override void Configure(Container container)
        {
            // Change ServiceStack configuration
            this.SetConfig(new HostConfig
            {
                AppendUtf8CharsetOnContentTypes = new HashSet<string> { MimeTypes.Html },

                // Set to return JSON if no request content type is defined
                // e.g. text/html or application/json
                DefaultContentType = MimeTypes.Json,
#if DEBUG
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

    /// <summary>
    /// The Echo interface.
    /// </summary>
    public interface IEcho
    {
        /// <summary>
        /// Gets or sets the sentence to echo.
        /// </summary>
        string Sentence { get; set; }
    }

    /// <summary>
    /// The Echo.
    /// </summary>
    public class Echo : IEcho
    {
        /// <summary>
        /// Gets or sets the sentence.
        /// </summary>
        public string Sentence { get; set; }
    }

    /// <summary>
    /// The Echoes operation endpoints.
    /// </summary>
    [Api("Echoes a sentence")]
    [Route("/echoes", "POST", Summary = @"Echoes a sentence.")]
    public class Echoes : IReturn<Echo>
    {
        /// <summary>
        /// Gets or sets the sentence to echo.
        /// </summary>
        [ApiMember(Name = "Sentence",
            DataType = "string",
            Description = "The sentence to echo.",
            IsRequired = true,
            ParameterType = "form")]
        public string Sentence { get; set; }
    }

    public class AsyncTest : IReturn<Echo> {}

    /// <summary>
    /// The Echoes web service.
    /// </summary>
    public class EchoesService : Service
    {
        public IServiceClient Client { get; set; }

        /// <summary>
        /// GET echoes.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns>The <see cref="object"/>.</returns>
        public object Post(Echoes request)
        {
            return new Echo { Sentence = request.Sentence };
        }

        public async Task<object> Any(AsyncTest request)
        {
            var response = await Client.PostAsync(new Echoes { Sentence = "Foo" });
            return response;
        }
    }

    [Route("/changerequest/{Id}")]
    public class ChangeRequest
    {
        public string Id { get; set; }
    }

    public class ChangeRequestResponse
    {
        public string ContentType { get; set; }
        public string Header { get; set; }
        public string QueryString { get; set; }
        public string Form { get; set; }

        public ResponseStatus ResponseStatus { get; set; }
    }

    public class CustomHttpRequest : HttpRequestBase
    {
        private readonly HttpRequestBase original;
        private readonly NameValueCollection queryString = new NameValueCollection();
        private readonly NameValueCollection formData = new NameValueCollection();

        public CustomHttpRequest(object original)
        {
            this.original = (HttpRequestBase)original;

            this.original.ContentType = this.original.ContentType;

            foreach (string key in this.original.QueryString.Keys)
            {
                queryString[key] = this.original.QueryString[key];
            }
            
            foreach (string key in this.original.Form.Keys)
            {
                formData[key] = this.original.Form[key];
            }
        }

        public override string ContentType
        {
            get
            {
                return original.ContentType;
            }
            set
            {
                original.ContentType = value;
            }
        }

        public override NameValueCollection QueryString
        {
            get { return queryString; }
        }

        public override NameValueCollection Form
        {
            get { return formData; }
        }

        public override NameValueCollection Headers
        {
            get
            {
                return original.Headers;
            }
        }
    }

    public class ChangeRequestService : Service
    {
        public object Any(ChangeRequest request)
        {
            var aspReq = new CustomHttpRequest(base.Request.OriginalRequest) {
                ContentType = MimeTypes.FormUrlEncoded
            };

            aspReq.QueryString["Id"] = request.Id;
            aspReq.Form["Id"] = request.Id;
            aspReq.Headers["Id"] = request.Id;

            return new ChangeRequestResponse {
                ContentType = aspReq.ContentType,
                Header = aspReq.Headers["Id"],
                QueryString = aspReq.QueryString["Id"],
                Form = aspReq.Form["Id"],                
            };
        }
    }

    public class Global : System.Web.HttpApplication
    {
        protected void Application_Start(object sender, EventArgs e)
        {
            new AppHost().Init();
        }
    }
}