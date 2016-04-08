//Copyright (c) Service Stack LLC. All Rights Reserved.
//License: https://raw.github.com/ServiceStack/ServiceStack/master/license.txt

namespace ServiceStack
{
    /// <summary>
    /// Marker interfaces
    /// </summary>
    public interface IService { }

    public interface IReturn { }
    public interface IReturn<T> : IReturn { }
    public interface IReturnVoid : IReturn { }

    /* Supported signatures: */
    //Not used or needed, here in-case someone wants to know what the correct signatures should be

    //Empty marker interfaces to enforce correct mappings
    public interface IVerb {}
    public interface IGet : IVerb { }
    public interface IPost : IVerb { }
    public interface IPut : IVerb { }
    public interface IDelete : IVerb { }
    public interface IPatch : IVerb { }
    public interface IOptions : IVerb { }

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