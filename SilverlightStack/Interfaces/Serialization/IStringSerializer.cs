namespace ServiceStack.DesignPatterns.Serialization
{
	public interface IStringSerializer
	{
		string Parse<TFrom>(TFrom from);
	}
}