namespace ServiceStack.ServiceModel.Serialization
{
	public interface IStringSerializer
	{
		string Parse<TFrom>(TFrom from);
	}
}