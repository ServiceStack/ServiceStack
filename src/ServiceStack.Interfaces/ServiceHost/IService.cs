using System;
using ServiceStack.Messaging;

namespace ServiceStack.ServiceHost
{
    /// <summary>
    /// Marker interfaces
    /// </summary>
    public interface IService { }

    public interface IReturn {}
    public interface IReturn<T> : IReturn { }
    public interface IReturnVoid : IReturn { }

    public interface IServiceRunner
    {
        object Process(IRequestContext requestContext, object instance, object request);
        object Process(IRequestContext requestContext, object instance, IMessage message);
        object ProcessOneWay(IRequestContext requestContext, object instance, object request);
    }

    public interface IServiceRunner<TRequest> : IServiceRunner
    {
        void OnBeforeExecute(IRequestContext requestContext, TRequest request);
        object OnAfterExecute(IRequestContext requestContext, object response);
        object HandleException(IRequestContext requestContext, TRequest request, Exception ex);

        object Execute(IRequestContext requestContext, object instance, TRequest request);
        object Execute(IRequestContext requestContext, object instance, IMessage<TRequest> request);
        object ExecuteOneWay(IRequestContext requestContext, object instance, TRequest request);
    }


    /* Supported signatures: */
    //Not used or needed, here in-case someone wants to know what the correct signatures should be

    public interface IAny<T>
    {
        object Any(T request);
    }
    public interface IGet<T>
    {
        object Get(T request);
    }
    public interface IPost<T>
    {
        object Post(T request);
    }
    public interface IPut<T>
    {
        object Put(T request);
    }
    public interface IDelete<T>
    {
        object Delete(T request);
    }
    public interface IPatch<T>
    {
        object Patch(T request);
    }
    public interface IOptions<T>
    {
        object Options(T request);
    }


    public interface IAnyVoid<T>
    {
        void Any(T request);
    }
    public interface IGetVoid<T>
    {
        void Get(T request);
    }
    public interface IPostVoid<T>
    {
        void Post(T request);
    }
    public interface IPutVoid<T>
    {
        void Put(T request);
    }
    public interface IDeleteVoid<T>
    {
        void Delete(T request);
    }
    public interface IPatchVoid<T>
    {
        void Patch(T request);
    }
    public interface IOptionsVoid<T>
    {
        void Options(T request);
    }
}