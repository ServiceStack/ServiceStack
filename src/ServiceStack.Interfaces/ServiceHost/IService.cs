using System;
using ServiceStack.Messaging;

namespace ServiceStack.ServiceHost
{
    /// <summary>
    /// Marker interface
    /// </summary>
    public interface IService { }

    //Marker interface
    public interface IReturn<T> { }
    public interface IReturnVoid { }

    public interface IServiceRunner
    {
        object Process(IRequestContext requestContext, object instance, object request);
        object Process(IRequestContext requestContext, object instance, IMessage message);
        object ProcessAsync(IRequestContext requestContext, object instance, object request);
    }

    public interface IServiceRunner<TRequest> : IServiceRunner
    {
        void OnBeforeExecute(IRequestContext requestContext, TRequest request);
        object OnAfterExecute(IRequestContext requestContext, object response);
        object HandleException(IRequestContext requestContext, TRequest request, Exception ex);

        object Execute(IRequestContext requestContext, object instance, TRequest request);
        object Execute(IRequestContext requestContext, object instance, IMessage<TRequest> request);
        object ExecuteAsync(IRequestContext requestContext, object instance, TRequest request);
    }


    /* Supported signatures:
     
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
    */
}