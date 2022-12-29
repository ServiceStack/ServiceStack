using System;
using ServiceStack.DataAnnotations;

namespace ServiceStack.Common.Tests.Models{
	
	[Alias("ModelWCIF")]
	[CompositeIndex(true, new string[] { "Comp1", "Comp2" })]
	public class ModelWithCompositeIndexFields
	{
		public string AlbumId
		{
			get;
			set;
		}
	
		[Alias("Comp1")]
		public string Composite1
		{
			get;
			set;
		}
	
		[Alias("Comp2")]
		public string Composite2
		{
			get;
			set;
		}
	
		public string Id
		{
			get;
			set;
		}
	
		[Index]
		public string Name
		{
			get;
			set;
		}
	
		[Index(true)]
		public string UniqueName
		{
			get;
			set;
		}
	
		public ModelWithCompositeIndexFields()
		{
		}
	}
}