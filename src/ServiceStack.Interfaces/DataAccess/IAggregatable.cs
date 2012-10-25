namespace ServiceStack.DataAccess
{
	public interface IAggregatable
	{
		double GetAvg<T>(T entity, string fieldName);
		long GetCount<T>(T entity, string fieldName);
		T GetMin<T>(T entity, string fieldName);
		T GetMax<T>(T entity, string fieldName);
		long GetSum<T>(T entity, string fieldName);
	}
}