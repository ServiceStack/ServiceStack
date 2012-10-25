using System;

namespace ServiceStack.SearchIndex
{
	public class FullTextIndexDocumentAttribute : Attribute
	{
		public Type ForType { get; set; }

		public FullTextIndexDocumentAttribute()
		{}

		public FullTextIndexDocumentAttribute(Type forType)
		{
			this.ForType = forType;
		}
	}
}