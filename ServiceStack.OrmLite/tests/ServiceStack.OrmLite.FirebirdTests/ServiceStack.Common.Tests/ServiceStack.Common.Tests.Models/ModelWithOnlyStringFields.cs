using System;

namespace ServiceStack.Common.Tests.Models{
	
	public class ModelWithOnlyStringFields
	{
		public string AlbumId
		{
			get;
			set;
		}
	
		public string AlbumName
		{
			get;
			set;
		}
	
		public string Id
		{
			get;
			set;
		}
	
		public string Name
		{
			get;
			set;
		}
	
		public ModelWithOnlyStringFields()
		{
		}
	
		public static ModelWithOnlyStringFields Create(string id)
		{
			ModelWithOnlyStringFields modelWithOnlyStringField = new ModelWithOnlyStringFields();
			modelWithOnlyStringField.Id = id;
			modelWithOnlyStringField.Name = "Name";
			modelWithOnlyStringField.AlbumId = "AlbumId";
			modelWithOnlyStringField.AlbumName = "AlbumName";
			return modelWithOnlyStringField;
		}
	}
}