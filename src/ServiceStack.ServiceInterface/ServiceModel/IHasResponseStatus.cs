namespace ServiceStack.ServiceInterface.ServiceModel
{
	/// <summary>
	/// Contract indication that the Response DTO has a ResponseStatus
	/// </summary>
	public interface IHasResponseStatus
	{
		ResponseStatus ResponseStatus { get; set; }
	}
}