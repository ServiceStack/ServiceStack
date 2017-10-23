
namespace ServiceStack.Html
{
	public class ModelMetadataProviders
	{
		private ModelMetadataProvider currentProvider;
		private static readonly ModelMetadataProviders Instance = new ModelMetadataProviders();

		internal ModelMetadataProviders()
		{
			currentProvider = new PocoMetadataProvider();
		}

		public static ModelMetadataProvider Current
		{
			get { return Instance.CurrentInternal; }
			set { Instance.CurrentInternal = value; }
		}

		internal ModelMetadataProvider CurrentInternal
		{
			get { return currentProvider; }
			set { currentProvider = value ?? new EmptyModelMetadataProvider(); }
		}
	}

}