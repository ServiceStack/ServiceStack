using System;
using System.Diagnostics;
using System.Net;
using Autofac;
using Check.ServiceInterface;
using Funq;
using ServiceStack;
using ServiceStack.Auth;
using ServiceStack.Configuration;
using ServiceStack.Host.Handlers;
using ServiceStack.Text;

namespace CheckHttpListener
{
    [Route("/request")]
    public class Request : IReturn<Response> { }
    public class Response { }

    public class MyServices : Service
    {
        public object Any(Request request)
        {
            return new Response();
        }
    }

    public class AutofacIocAdapter : IContainerAdapter
    {
        private readonly IContainer _container;

        public AutofacIocAdapter(IContainer container)
        {
            _container = container;
        }

        public T Resolve<T>()
        {
            return _container.Resolve<T>();
        }

        public T TryResolve<T>()
        {
            T result;

            if (_container.TryResolve<T>(out result))
            {
                return result;
            }

            return default(T);
        }
    }

    public class AppHost : AppHostHttpListenerBase
    {
        public AppHost() : base("Check HttpListener Tests", 
            typeof(ErrorsService).Assembly,
            typeof(UserService).Assembly) { }

        public override void Configure(Container container)
        {
            //RawHttpHandlers.Add(_ => new CustomActionHandler((req, res) =>
            //{
            //    var bytes = req.InputStream.ReadFully();
            //    res.OutputStream.Write(bytes, 0, bytes.Length);
            //}));

            this.CustomErrorHttpHandlers[HttpStatusCode.NotFound] = null;

            SetConfig(new HostConfig
            {
                DebugMode = true,
                SkipFormDataInCreatingRequest = true,
            });

            //Plugins.Add(new NativeTypesFeature());

            var builder = new ContainerBuilder();
            var autofac = builder.Build();
            container.Adapter = new AutofacIocAdapter(autofac);

            Plugins.Add(new AuthFeature(() => new AuthUserSession(), 
                new IAuthProvider[] {
                    new BasicAuthProvider(AppSettings), 
                }));
        }

        public override void OnAfterInit()
        {
            base.OnAfterInit();

            using (var authService = Container.Resolve<AuthenticateService>())
            {
                authService.Authenticate(new Authenticate());
            }
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            //Licensing.RegisterLicenseFromFileIfExists(@"c:\src\appsettings.license.txt");

            new AppHost()
                .Init()
                .Start("http://*:2020/");

            //TestService();

            Process.Start("http://localhost:2020/upload.html");

            //var response = "http://localhost:2020".PostToUrl(new { a = "foo", b = "bar" });
            //"Response: {0}".Print(response);

            
            Console.ReadLine();
        }

        private async static void TestService()
        {
            var client = new JsonServiceClient("http://localhost:2020/");

            try
            {
                var request = new UpdateAddressVoid();
                request.ToGetUrl().Print();
                request.ToPostUrl().Print();
                await client.PostAsync(request);
                //var response = client.Post(request);
            }
            catch (WebServiceException ex)
            {
                ex.StatusCode.ToString().Print();
                ex.StatusDescription.Print();
                ex.ResponseBody.Print();
            }
        }
    }

    [Route("/user/{UserId}/Address")]
    public class UpdateAddress : IReturn<AddressResponse>
    {
        public int UserId { get; set; }

        public string Address { get; set; }
    }

    [Route("/user/{UserId}/AddressVoid")]
    public class UpdateAddressVoid : IReturnVoid
    {
        public int UserId { get; set; }

        public string Address { get; set; }
    }

    public class AddressResponse
    {
        public string Address { get; set; }
    }

    public class UserService : Service
    {
        public object Post(UpdateAddress request)
        {
            if (request.UserId > 0)
            {
                return new HttpResult(request.Address, HttpStatusCode.OK);
            }

            //throw new UnauthorizedAccessException("Unauthorized UserId");
            //throw HttpError.Unauthorized(request.Address ?? "Unauthorized UserId");
            return HttpError.Unauthorized(request.Address ?? "Unauthorized UserId");
        }

        public void Post(UpdateAddressVoid request)
        {
            //throw new UnauthorizedAccessException("Unauthorized UserId");
            throw HttpError.Unauthorized(request.Address ?? "Unauthorized UserId");
            //return HttpError.Unauthorized(request.Address ?? "Unauthorized UserId");
        }
    }

    [Route("/upload", "POST")]
    public class UploadFileRequest { }

    public class TestController : Service
    {
        public void Any(UploadFileRequest request)
        {
            int filesCount = Request.Files.Length;
            Console.WriteLine(filesCount); // Always 0
        }
    }
}
