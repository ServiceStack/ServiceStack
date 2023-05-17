using System;
using ServiceStack.DataAnnotations;

namespace ServiceStack.Common.Tests.Models;

[Alias("ModelWIF")]
public class ModelWithIndexFields
{
	public string AlbumId
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
	
	public ModelWithIndexFields()
	{
	}
}