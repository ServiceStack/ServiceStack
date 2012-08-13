using System;
using System.Collections.Generic;
using ServiceStack.ServiceHost;
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

    public class FunqDepDisposableProperty : IDisposable { public void Dispose() { } }
    public class AltDepDisposableProperty : IDisposable { public void Dispose() { } }

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

    public class IocService : IService<Ioc>, IDisposable, IRequiresRequestContext
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

        public object Execute(Ioc request)
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
    public class IocScopeService : IService<IocScope>, IDisposable
    {
        public FunqRequestScope FunqRequestScope { get; set; }
        public FunqSingletonScope FunqSingletonScope { get; set; }
        public FunqNoneScope FunqNoneScope { get; set; }
        public FunqRequestScopeDepDisposableProperty FunqRequestScopeDepDisposableProperty { get; set; }
        public AltRequestScopeDepDisposableProperty AltRequestScopeDepDisposableProperty { get; set; }

        public object Execute(IocScope request)
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

}