namespace ServiceStack.DesignPatterns.Model
{
	public interface IHasId<T>
	{
		T Id { get; }
	}
}