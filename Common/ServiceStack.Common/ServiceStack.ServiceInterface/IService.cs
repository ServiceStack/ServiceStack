namespace ServiceStack.ServiceInterface
{
	public interface IService
	{
		object Execute(CallContext context);
	}
}