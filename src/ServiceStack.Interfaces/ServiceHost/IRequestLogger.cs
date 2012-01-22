namespace ServiceStack.ServiceHost
{
	/// <summary>
	/// Log every service request
	/// </summary>
	public interface IRequestLogger
	{
		/// <summary>
		/// Log a request
		/// </summary>
		/// <param name="requestContext"></param>
		/// <param name="requestDto"></param>
		void Log(IRequestContext requestContext, object requestDto);
	}
}