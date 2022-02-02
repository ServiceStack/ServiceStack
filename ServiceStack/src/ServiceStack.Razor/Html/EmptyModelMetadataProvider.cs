using System;
using System.Collections.Generic;

namespace ServiceStack.Html
{
	public class EmptyModelMetadataProvider : ModelMetadataProvider
	{
		protected virtual ModelMetadata CreateMetadata(IEnumerable<Attribute> attributes, Type containerType, Func<object> modelAccessor, Type modelType, string propertyName)
		{
			return new ModelMetadata(this, containerType, modelAccessor, modelType, propertyName);
		}

		public override IEnumerable<ModelMetadata> GetMetadataForProperties(object container, Type containerType)
		{
			return new List<ModelMetadata>();
		}

		public override ModelMetadata GetMetadataForProperty(Func<object> modelAccessor, Type containerType, string propertyName)
		{
			return null;
		}

		public override ModelMetadata GetMetadataForType(Func<object> modelAccessor, Type modelType)
		{
			return null;
		}
	}
}
