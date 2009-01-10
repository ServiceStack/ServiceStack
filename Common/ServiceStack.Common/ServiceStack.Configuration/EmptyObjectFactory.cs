namespace ServiceStack.Configuration
{
	public class EmptyObjectFactory : IObjectFactory
	{
		public T Create<T>()
		{
			return default(T);
		}

		public T Create<T>(string objectName)
		{
			return default(T);
		}

		public bool Contains(string objectName)
		{
			return false;
		}
	}
}