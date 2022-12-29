using System;
using ServiceStack.DataAnnotations;

namespace ServiceStack.Common.Tests.Models{
	
	[Alias("ModelWLISF")]
	public class ModelWithLongIdAndStringFields
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
	
		public long Id
		{
			get;
			set;
		}
	
		public string Name
		{
			get;
			set;
		}
	
		public ModelWithLongIdAndStringFields()
		{
		}
	}
}