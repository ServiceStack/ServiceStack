namespace ServiceStack.DataAccess.Db4oProvider.Criteria
{
	public interface IOrderAscendingCriteria : ICriteria
	{
		string OrderedAscendingBy { get; }
	}
}