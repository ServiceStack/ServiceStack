namespace ServiceStack.DataAccess.Db4oProvider.Criteria
{
	public interface IOrderDescendingCriteria : ICriteria
	{
		string OrderedDescendingBy { get; }
	}
}