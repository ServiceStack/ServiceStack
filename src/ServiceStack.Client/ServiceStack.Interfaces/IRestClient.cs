namespace ServiceStack.Service
{
	public interface IRestClient : IRestClientAsync
	{
		TResponse Get<TResponse>(string relativeOrAbsoluteUrl);
		TResponse Delete<TResponse>(string relativeOrAbsoluteUrl);

		TResponse Post<TResponse>(string relativeOrAbsoluteUrl, object request);
		TResponse Put<TResponse>(string relativeOrAbsoluteUrl, object request);
	}
}