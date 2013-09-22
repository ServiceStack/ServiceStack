namespace ServiceStack.Serialization
{
	public interface IStringSerializer
	{
		string Parse<TFrom>(TFrom from);
	}
}