namespace ServiceStack.WebHost.Endpoints.Tests.Support.Services
{
    public abstract class TestServiceBase<TRequest>
        : IService
    {
        protected abstract object Run(TRequest request);

        public object Any(TRequest request)
        {
            return Run(request);
        }
    }
}