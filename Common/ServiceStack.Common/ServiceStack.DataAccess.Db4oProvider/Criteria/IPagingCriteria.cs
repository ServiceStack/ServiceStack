namespace ServiceStack.DataAccess.Db4oProvider.Criteria
{
	public interface IPagingCriteria : ICriteria
	{
		int ResultOffset { get; }
		int ResultLimit { get; }
	}
}