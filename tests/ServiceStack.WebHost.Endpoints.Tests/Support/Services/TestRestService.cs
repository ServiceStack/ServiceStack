using ServiceStack.Text;

namespace ServiceStack.WebHost.Endpoints.Tests.Support.Services
{
    public class TestRestService<TRequest> : IService
    {
        public object Get(TRequest request)
        {
            return Run(request, ApplyTo.Get);
        }

        public object Put(TRequest request)
        {
            return Run(request, ApplyTo.Put);
        }

        public object Post(TRequest request)
        {
            return Run(request, ApplyTo.Post);
        }

        public object Patch(TRequest request)
        {
            return Run(request, ApplyTo.Patch);
        }

        public object Delete(TRequest request)
        {
            return Run(request, ApplyTo.Delete);
        }

        public object Head(TRequest request)
        {
            return Run(request, ApplyTo.Head);
        }

        public object Options(TRequest request)
        {
            return Run(request, ApplyTo.Options);
        }

        protected virtual object Run(TRequest request, ApplyTo method)
        {
            return request.AsTypeString();
        }
    }

    public static class ObjectExtensions
    {
        public static string AsTypeString(this object request)
        {
            return request.GetType().ToTypeString() + "\n" + request.Dump();
        }
    }
}
