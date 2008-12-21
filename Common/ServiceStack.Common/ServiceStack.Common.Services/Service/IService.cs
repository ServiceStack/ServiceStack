namespace ServiceStack.Common.Services.Service
{
    public interface IService
    {
        object Execute(object dtoRequest);
    }
}