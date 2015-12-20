The complete `IServiceClient` API with methods of all Interfaces combined below:

```csharp
public interface IServiceClient : 
    IServiceClientAsync, IOneWayClient, IRestClient, IReplyClient, IHasSessionId, IHasVersion
{
    int Version { get; set; }      //IHasVersion
    string SessionId { get; set; } //IHasSessionId

    //IReplyClient:
    TResponse Send<TResponse>(object request);
    TResponse Send<TResponse>(IReturn<TResponse> request);
    void Send(IReturnVoid request);
    List<TResponse> SendAll<TResponse>(IEnumerable<IReturn<TResponse>> requests);

    //IRestClient
    void ClearCookies();
    Dictionary<string, string> GetCookieValues();

    void Get(IReturnVoid request);
    TResponse Get<TResponse>(IReturn<TResponse> requestDto);
    TResponse Get<TResponse>(object requestDto);
    TResponse Get<TResponse>(string relativeOrAbsoluteUrl);
    IEnumerable<TResponse> GetLazy<TResponse>(IReturn<QueryResponse<TResponse>> queryDto);

    void Delete(IReturnVoid requestDto);
    TResponse Delete<TResponse>(IReturn<TResponse> request);
    TResponse Delete<TResponse>(object request);
    TResponse Delete<TResponse>(string relativeOrAbsoluteUrl);

    void Post(IReturnVoid requestDto);
    TResponse Post<TResponse>(IReturn<TResponse> requestDto);
    TResponse Post<TResponse>(object requestDto);
    TResponse Post<TResponse>(string relativeOrAbsoluteUrl, object request);

    void Put(IReturnVoid requestDto);
    TResponse Put<TResponse>(IReturn<TResponse> requestDto);
    TResponse Put<TResponse>(object requestDto);
    TResponse Put<TResponse>(string relativeOrAbsoluteUrl, object requestDto);

    void Patch(IReturnVoid requestDto);
    TResponse Patch<TResponse>(IReturn<TResponse> requestDto);
    TResponse Patch<TResponse>(object requestDto);
    TResponse Patch<TResponse>(string relativeOrAbsoluteUrl, object requestDto);

    void CustomMethod(string httpVerb, IReturnVoid requestDto);
    TResponse CustomMethod<TResponse>(string httpVerb, IReturn<TResponse> requestDto);
    TResponse CustomMethod<TResponse>(string httpVerb, object requestDto);

    TResponse PostFile<TResponse>(string relativeOrAbsoluteUrl, Stream fileToUpload, string fileName, string mimeType);
    TResponse PostFileWithRequest<TResponse>(Stream fileToUpload, string fileName, object request, string fieldName = "upload");
    TResponse PostFileWithRequest<TResponse>(string relativeOrAbsoluteUrl, Stream fileToUpload, string fileName, object request, string fieldName = "upload");

    //IOneWayClient
    void SendOneWay(object requestDto);
    void SendOneWay(string relativeOrAbsoluteUri, object requestDto);
    void SendAllOneWay(IEnumerable<object> requests);

    //IServiceClientAsync
    Task<TResponse> SendAsync<TResponse>(IReturn<TResponse> requestDto);
    Task<TResponse> SendAsync<TResponse>(object requestDto);
    Task<List<TResponse>> SendAllAsync<TResponse>(IEnumerable<IReturn<TResponse>> requests);

    //IRestClientAsync
    void SetCredentials(string userName, string password);

    Task<TResponse> GetAsync<TResponse>(IReturn<TResponse> requestDto);
    Task<TResponse> GetAsync<TResponse>(object requestDto);
    Task<TResponse> GetAsync<TResponse>(string relativeOrAbsoluteUrl);
    Task GetAsync(IReturnVoid requestDto);

    Task<TResponse> DeleteAsync<TResponse>(IReturn<TResponse> requestDto);
    Task<TResponse> DeleteAsync<TResponse>(object requestDto);
    Task<TResponse> DeleteAsync<TResponse>(string relativeOrAbsoluteUrl);
    Task DeleteAsync(IReturnVoid requestDto);

    Task<TResponse> PostAsync<TResponse>(IReturn<TResponse> requestDto);
    Task<TResponse> PostAsync<TResponse>(object requestDto);
    Task<TResponse> PostAsync<TResponse>(string relativeOrAbsoluteUrl, object request);
    Task PostAsync(IReturnVoid requestDto);

    Task<TResponse> PutAsync<TResponse>(IReturn<TResponse> requestDto);
    Task<TResponse> PutAsync<TResponse>(object requestDto);
    Task<TResponse> PutAsync<TResponse>(string relativeOrAbsoluteUrl, object request);
    Task PutAsync(IReturnVoid requestDto);

    Task<TResponse> CustomMethodAsync<TResponse>(string httpVerb, IReturn<TResponse> requestDto);
    Task<TResponse> CustomMethodAsync<TResponse>(string httpVerb, object requestDto);
    Task CustomMethodAsync(string httpVerb, IReturnVoid requestDto);
    Task<TResponse> CustomMethodAsync<TResponse>(string httpVerb, string relativeOrAbsoluteUrl, object request);

    void CancelAsync();
}
```