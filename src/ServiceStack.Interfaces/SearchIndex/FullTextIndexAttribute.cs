using System;

namespace ServiceStack.SearchIndex
{
	[Flags]
	public enum FullTextIndexAttribute
	{
		IsDefault = 1,
		IsKey = 2,
		NoIndex = 4,
		IndexTokenized = 8,
		IndexUnTokenized = 16,
		//IndexNoNorms,
		NoStore = 32,
		StoreCompressed = 64,
		StoreUncompressed = 128,
	}
}