using System;
using System.Collections.Generic;
using ServiceStack.OrmLite;
using ServiceStack.ServiceHost;
using ServiceStack.ServiceInterface;
using ServiceStack.ServiceInterface.ServiceModel;
using ServiceStack.WebHost.Endpoints.Tests.Support.Host;

namespace ServiceStack.WebHost.Endpoints.Tests.Support.Services
{
    public class FunqRequestScope
    {
        public static int Count = 0;
        public FunqRequestScope() { Count++; }
    }

    public class FunqSingletonScope
    {
        public static int Count = 0;
        public FunqSingletonScope() { Count++; }
    }

    public class FunqNoneScope
    {
        public static int Count = 0;
        public FunqNoneScope() { Count++; }
    }

    public class FunqRequestScopeDisposable : IDisposable
    {
        public static int Count = 0;
        public static int DisposeCount = 0;
        public FunqRequestScopeDisposable() { Count++; }

        public void Dispose()
        {
            DisposeCount++;
        }
    }

    public class FunqSingletonScopeDisposable : IDisposable
    {
        public static int Count = 0;
        public static int DisposeCount = 0;
        public FunqSingletonScopeDisposable() { Count++; }

        public void Dispose()
        {
            DisposeCount++;
        }
    }

    public class FunqNoneScopeDisposable : IDisposable
    {
        public static int Count = 0;
        public static int DisposeCount = 0;
        public FunqNoneScopeDisposable() { Count++; }

        public void Dispose()
        {
            DisposeCount++;
        }
    }

    public class FunqRequestScopeDepDisposableProperty : IDisposable
    {
        public static int Count = 0;
        public static int DisposeCount = 0;
        public FunqRequestScopeDepDisposableProperty() { Count++; }
        public void Dispose() { DisposeCount++; }
    }

    public class AltRequestScopeDepDisposableProperty : IDisposable
    {
        public static int Count = 0;
        public static int DisposeCount = 0;
        public AltRequestScopeDepDisposableProperty() { Count++; }
        public void Dispose() { DisposeCount++; }
    }

    public class FunqDepCtor { }
    public class AltDepCtor { }

    public class FunqDepProperty { }
    public class AltDepProperty { }

    public class FunqDepDisposableProperty : IDisposable
    {
        public static int DisposeCount = 0;
        public void Dispose() { DisposeCount++; }
    }
    public class AltDepDisposableProperty : IDisposable
    {
        public static int DisposeCount = 0;
        public void Dispose() { DisposeCount++; }
    }

    public class Ioc { }

    public class IocResponse : IHasResponseStatus
    {
        public IocResponse()
        {
            this.ResponseStatus = new ResponseStatus();
            this.Results = new List<string>();
        }

        public List<string> Results { get; set; }

        public ResponseStatus ResponseStatus { get; set; }
    }


    [Route("/action-attr")]
    public class ActionAttr : IReturn<IocResponse> {}

    public class ActionLevelAttribute : RequestFilterAttribute
    {
        public IRequestContext RequestContext { get; set; }
        public FunqDepProperty FunqDepProperty { get; set; }
        public FunqDepDisposableProperty FunqDepDisposableProperty { get; set; }
        public AltDepProperty AltDepProperty { get; set; }
        public AltDepDisposableProperty AltDepDisposableProperty { get; set; }

        public override void Execute(IHttpRequest req, IHttpResponse res, object requestDto)
        {
            var response = new IocResponse();

            var deps = new object[] {
				FunqDepProperty, FunqDepDisposableProperty, 
				AltDepProperty, AltDepDisposableProperty
			};

            foreach (var dep in deps)
            {
                if (dep != null)
                    response.Results.Add(dep.GetType().Name);
            }

            req.Items["action-attr"] = response;
        }
    }


    public class IocService : IService, IDisposable, IRequiresRequestContext
    {
        private readonly FunqDepCtor funqDepCtor;
        private readonly AltDepCtor altDepCtor;

        public IocService(FunqDepCtor funqDepCtor, AltDepCtor altDepCtor)
        {
            this.funqDepCtor = funqDepCtor;
            this.altDepCtor = altDepCtor;
        }

        public IRequestContext RequestContext { get; set; }
        public FunqDepProperty FunqDepProperty { get; set; }
        public FunqDepDisposableProperty FunqDepDisposableProperty { get; set; }
        public AltDepProperty AltDepProperty { get; set; }
        public AltDepDisposableProperty AltDepDisposableProperty { get; set; }

        public object Any(Ioc request)
        {
            var response = new IocResponse();

            var deps = new object[] {
				funqDepCtor, altDepCtor, 
				FunqDepProperty, FunqDepDisposableProperty, 
				AltDepProperty, AltDepDisposableProperty
			};

            foreach (var dep in deps)
            {
                if (dep != null)
                    response.Results.Add(dep.GetType().Name);
            }

            if (ThrowErrors) throw new ArgumentException("This service has intentionally failed");

            return response;
        }

        [ActionLevel]
        public IocResponse Any(ActionAttr request)
        {
            return RequestContext.Get<IHttpRequest>().Items["action-attr"] as IocResponse;
        }
        
        public static int DisposedCount = 0;
        public static bool ThrowErrors = false;

        public void Dispose()
        {
            DisposedCount++;
        }
    }


    public class IocScope
    {
        public bool Throw { get; set; }
    }

    public class IocScopeResponse : IHasResponseStatus
    {
        public IocScopeResponse()
        {
            this.ResponseStatus = new ResponseStatus();
            this.Results = new Dictionary<string, int>();
        }

        public Dictionary<string, int> Results { get; set; }

        public ResponseStatus ResponseStatus { get; set; }
    }

    [IocRequestFilter]
    public class IocScopeService : IService, IDisposable
    {
        public FunqRequestScope FunqRequestScope { get; set; }
        public FunqSingletonScope FunqSingletonScope { get; set; }
        public FunqNoneScope FunqNoneScope { get; set; }
        public FunqRequestScopeDepDisposableProperty FunqRequestScopeDepDisposableProperty { get; set; }
        public AltRequestScopeDepDisposableProperty AltRequestScopeDepDisposableProperty { get; set; }

        public object Any(IocScope request)
        {
            if (request.Throw)
                throw new Exception("Exception requested by user");

            var response = new IocScopeResponse {
                Results = {
                    { typeof(FunqSingletonScope).Name, FunqSingletonScope.Count },
                    { typeof(FunqRequestScope).Name, FunqRequestScope.Count },
                    { typeof(FunqNoneScope).Name, FunqNoneScope.Count },
                },                
            };

            return response;
        }

        public static int DisposedCount = 0;
        public static bool ThrowErrors = false;

        public void Dispose()
        {
            DisposedCount++;
        }    
    }

    public class IocDispose : IReturn<IocDisposeResponse>
    {
        public bool Throw { get; set; }
    }

    public class IocDisposeResponse : IHasResponseStatus
    {
        public IocDisposeResponse()
        {
            this.ResponseStatus = new ResponseStatus();
            this.Results = new Dictionary<string, int>();
        }

        public Dictionary<string, int> Results { get; set; }

        public ResponseStatus ResponseStatus { get; set; }
    }

    public class IocDisposableService : IService, IDisposable
    {
        public FunqRequestScopeDisposable FunqRequestScopeDisposable { get; set; }
        public FunqSingletonScopeDisposable FunqSingletonScopeDisposable { get; set; }
        public FunqNoneScopeDisposable FunqNoneScopeDisposable { get; set; }
        public FunqRequestScopeDepDisposableProperty FunqRequestScopeDepDisposableProperty { get; set; }
        public AltRequestScopeDepDisposableProperty AltRequestScopeDepDisposableProperty { get; set; }

        public object Any(IocDispose request)
        {
            if (request.Throw)
                throw new Exception("Exception requested by user");

            var response = new IocDisposeResponse
            {
                Results = {
                    { typeof(FunqSingletonScopeDisposable).Name, FunqSingletonScopeDisposable.DisposeCount },
                    { typeof(FunqRequestScopeDisposable).Name, FunqRequestScopeDisposable.DisposeCount },
                    { typeof(FunqNoneScopeDisposable).Name, FunqNoneScopeDisposable.DisposeCount },
                    { typeof(FunqRequestScopeDepDisposableProperty).Name, FunqRequestScopeDepDisposableProperty.DisposeCount },
                    { typeof(AltRequestScopeDepDisposableProperty).Name, AltRequestScopeDepDisposableProperty.DisposeCount },
                },
            };

            return response;
        }

        public static int DisposeCount = 0;

        public void Dispose()
        {
            DisposeCount++;
        }
    }

}