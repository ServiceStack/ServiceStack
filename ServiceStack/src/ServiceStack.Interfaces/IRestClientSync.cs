namespace ServiceStack;

public interface IRestClientSync : IServiceClientCommon
{
    TResponse Get<TResponse>(IReturn<TResponse> requestDto);
    TResponse Get<TResponse>(object requestDto);
    void Get(IReturnVoid requestDto);

    TResponse Delete<TResponse>(IReturn<TResponse> requestDto);
    TResponse Delete<TResponse>(object requestDto);
    void Delete(IReturnVoid requestDto);

    TResponse Post<TResponse>(IReturn<TResponse> requestDto);
    TResponse Post<TResponse>(object requestDto);
    void Post(IReturnVoid requestDto);

    TResponse Put<TResponse>(IReturn<TResponse> requestDto);
    TResponse Put<TResponse>(object requestDto);
    void Put(IReturnVoid requestDto);

    TResponse Patch<TResponse>(IReturn<TResponse> requestDto);
    TResponse Patch<TResponse>(object requestDto);
    void Patch(IReturnVoid requestDto);

    TResponse CustomMethod<TResponse>(string httpVerb, IReturn<TResponse> requestDto);
    TResponse CustomMethod<TResponse>(string httpVerb, object requestDto);
    void CustomMethod(string httpVerb, IReturnVoid requestDto);
}