namespace ServiceStack.DataAccess.Criteria
{
	public interface IOrderDescendingCriteria : ICriteria
	{
		string OrderedDescendingBy { get; }
	}
}