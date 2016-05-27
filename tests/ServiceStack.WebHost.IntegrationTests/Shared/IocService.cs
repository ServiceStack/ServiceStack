using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Funq;
using ServiceStack.Configuration;
using ServiceStack.DataAnnotations;
using ServiceStack.Web;

namespace ServiceStack.Shared.Tests
{
    public static class IocShared
    {
        public static void Configure(ServiceStackHost appHost)
        {
            var container = appHost.Container;

            container.Adapter = new IocAdapter();
            container.Register(c => new FunqDepCtor());
            container.Register(c => new FunqDepProperty());
            container.Register(c => new FunqDepDisposableProperty());

            container.Register(c => new FunqSingletonScope()).ReusedWithin(ReuseScope.Default);
            container.Register(c => new FunqRequestScope()).ReusedWithin(ReuseScope.Request);
            container.Register(c => new FunqNoneScope()).ReusedWithin(ReuseScope.None);
            container.Register(c => new FunqInjectRequest()).ReusedWithin(ReuseScope.None);
            container.Register(c => new FunqRequestScopeDepDisposableProperty()).ReusedWithin(ReuseScope.Request);

            container.Register(c => new FunqSingletonScopeDisposable()).ReusedWithin(ReuseScope.Default);
            container.Register(c => new FunqRequestScopeDisposable()).ReusedWithin(ReuseScope.Request);
            container.Register(c => new FunqNoneScopeDisposable()).ReusedWithin(ReuseScope.None);
        }
    }

    public class IocAdapter : IContainerAdapter, IRelease
    {
        public T TryResolve<T>()
        {
            if (typeof(T) == typeof(IRequest))
                throw new ArgumentException("should not ask for IRequestContext");

            if (typeof(T) == typeof(AltDepProperty))
                return (T)(object)new AltDepProperty();
            if (typeof(T) == typeof(AltDepDisposableProperty))
                return (T)(object)new AltDepDisposableProperty();
            if (typeof(T) == typeof(AltRequestScopeDepDisposableProperty))
                return (T)(object)RequestContext.Instance.GetOrCreate(() => new AltRequestScopeDepDisposableProperty());

            return default(T);
        }

        public T Resolve<T>()
        {
            if (typeof(T) == typeof(AltDepCtor))
                return (T)(object)new AltDepCtor();

            return default(T);
        }

        public void Release(object instance)
        {
            var disposable = instance as IDisposable;
            if (disposable != null)
                disposable.Dispose();
        }
    }


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

    public class FunqInjectRequest : IRequiresRequest
    {
        public FunqInjectRequest()
        {
            this.SecondLevel = new FunqInjectRequest2();
        }

        public IRequest Request { get; set; }

        public FunqInjectRequest2 SecondLevel { get; set; }
    }

    public class FunqInjectRequest2 : IRequiresRequest
    {
        public IRequest Request { get; set; }
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

    [Route("/ioc")]
    public class Ioc { }
    [Route("/iocasync")]
    public class IocAsync { }

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


    [Exclude(Feature.Metadata)]
    [Route("/action-attr")]
    public class ActionAttr : IReturn<IocResponse> { }

    [Route("/action-attr-async")]
    public class ActionAttrAsync : IReturn<IocResponse> { }

    public class ActionLevelAttribute : RequestFilterAttribute
    {
        public IRequest RequestContext { get; set; }
        public FunqDepProperty FunqDepProperty { get; set; }
        public FunqDepDisposableProperty FunqDepDisposableProperty { get; set; }
        public AltDepProperty AltDepProperty { get; set; }
        public AltDepDisposableProperty AltDepDisposableProperty { get; set; }

        public override void Execute(IRequest req, IResponse res, object requestDto)
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

    public class ResetIoc
    {
        public bool ThrowErrors { get; set; }
    }

    public class IocStats : IReturn<IocStatsResponse> { }
    public class IocStatsResponse
    {
        public int FunqNoneScope_Count { get; set; }
        public int FunqRequestScope_Count { get; set; }
        public int IocService_DisposeCount { get; set; }
        public int IocDisposableService_DisposeCount { get; set; }
        public int FunqSingletonScopeDisposable_DisposeCount { get; set; }
        public int FunqRequestScopeDisposable_DisposeCount { get; set; }
        public int FunqNoneScopeDisposable_DisposeCount { get; set; }
        public int FunqRequestScopeDepDisposableProperty_DisposeCount { get; set; }
        public int AltRequestScopeDepDisposableProperty_DisposeCount { get; set; }
        public int Container_disposablesCount { get; set; }
    }


    public class IocResetService : IService
    {
        public void Any(ResetIoc request)
        {
            FunqNoneScope.Count =
            FunqRequestScope.Count =
            IocService.DisposeCount =
            IocDisposableService.DisposeCount =
            FunqSingletonScopeDisposable.DisposeCount =
            FunqRequestScopeDisposable.DisposeCount =
            FunqNoneScopeDisposable.DisposeCount =
            FunqRequestScopeDepDisposableProperty.DisposeCount =
            AltRequestScopeDepDisposableProperty.DisposeCount =
                0;

            IocService.ThrowErrors = request.ThrowErrors;
        }

        public object Any(IocStats request)
        {
            return new IocStatsResponse
            {
                FunqNoneScope_Count = FunqNoneScope.Count,
                FunqRequestScope_Count = FunqRequestScope.Count,
                IocService_DisposeCount = IocService.DisposeCount,
                IocDisposableService_DisposeCount = IocDisposableService.DisposeCount,
                FunqSingletonScopeDisposable_DisposeCount = FunqSingletonScopeDisposable.DisposeCount,
                FunqRequestScopeDisposable_DisposeCount = FunqRequestScopeDisposable.DisposeCount,
                FunqNoneScopeDisposable_DisposeCount = FunqNoneScopeDisposable.DisposeCount,
                FunqRequestScopeDepDisposableProperty_DisposeCount = FunqRequestScopeDepDisposableProperty.DisposeCount,
                AltRequestScopeDepDisposableProperty_DisposeCount = AltRequestScopeDepDisposableProperty.DisposeCount,
                Container_disposablesCount = HostContext.Container.disposablesCount,
            };
        }
    }

    public class IocService : IService, IDisposable, IRequiresRequest
    {
        private readonly FunqDepCtor funqDepCtor;
        private readonly AltDepCtor altDepCtor;

        public IocService(FunqDepCtor funqDepCtor, AltDepCtor altDepCtor)
        {
            this.funqDepCtor = funqDepCtor;
            this.altDepCtor = altDepCtor;
        }

        public IRequest Request { get; set; }
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

        public async Task<object> Any(IocAsync request)
        {
            await Task.Delay(10);
            return Any(request.ConvertTo<Ioc>());
        }

        [ActionLevel]
        public IocResponse Any(ActionAttr request)
        {
            return Request.Items["action-attr"] as IocResponse;
        }

        [ActionLevel]
        public async Task<IocResponse> Any(ActionAttrAsync request)
        {
            await Task.Delay(10);
            return Request.Items["action-attr"] as IocResponse;
        }

        public static int DisposeCount = 0;
        public static bool ThrowErrors = false;

        public void Dispose()
        {
            DisposeCount++;
        }
    }

    [Route("/iocscope")]
    public class IocScope
    {
        public bool Throw { get; set; }
    }

    [Route("/iocscopeasync")]
    public class IocScopeAsync
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

        public int InjectsRequest { get; set; }

        public ResponseStatus ResponseStatus { get; set; }
    }

    public class IocRequestFilterAttribute : AttributeBase, IHasRequestFilter
    {
        public FunqSingletonScope FunqSingletonScope { get; set; }
        public FunqRequestScope FunqRequestScope { get; set; }
        public FunqNoneScope FunqNoneScope { get; set; }
        public FunqRequestScopeDepDisposableProperty FunqRequestScopeDepDisposableProperty { get; set; }
        public AltRequestScopeDepDisposableProperty AltRequestScopeDepDisposableProperty { get; set; }

        public int Priority { get; set; }

        public void RequestFilter(IRequest req, IResponse res, object requestDto)
        {
        }

        public IHasRequestFilter Copy()
        {
            return (IHasRequestFilter)this.MemberwiseClone();
        }
    }

    [IocRequestFilter]
    public class IocScopeService : IService, IDisposable
    {
        public FunqRequestScope FunqRequestScope { get; set; }
        public FunqSingletonScope FunqSingletonScope { get; set; }
        public FunqNoneScope FunqNoneScope { get; set; }
        public FunqInjectRequest FunqInjectRequest { get; set; }
        public FunqRequestScopeDepDisposableProperty FunqRequestScopeDepDisposableProperty { get; set; }
        public AltRequestScopeDepDisposableProperty AltRequestScopeDepDisposableProperty { get; set; }

        public object Any(IocScope request)
        {
            if (request.Throw)
                throw new Exception("Exception requested by user");

            var response = new IocScopeResponse
            {
                Results = {
                    { typeof(FunqSingletonScope).Name, FunqSingletonScope.Count },
                    { typeof(FunqRequestScope).Name, FunqRequestScope.Count },
                    { typeof(FunqNoneScope).Name, FunqNoneScope.Count },
                },
                InjectsRequest = FunqInjectRequest.Request != null 
                    ? 1 + (FunqInjectRequest.SecondLevel.Request != null ? 1 : 0)
                    : 0,
            };

            return response;
        }

        public async Task<object> Any(IocScopeAsync request)
        {
            await Task.Delay(10);
            return Any(request.ConvertTo<IocScope>());
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

    public class IocDisposeAsync : IReturn<IocDisposeResponse>
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

        public async Task<object> Any(IocDisposeAsync request)
        {
            await Task.Delay(10);
            return Any(request.ConvertTo<IocDispose>());
        }

        public static int DisposeCount = 0;

        public void Dispose()
        {
            DisposeCount++;
        }
    }

}