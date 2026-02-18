using System;
using NUnit.Framework;
using ServiceStack.DataAnnotations;

namespace ServiceStack.Common.Tests.Models;

[Alias("GROUP")]
public class ModelWithReservedWords
{
	[AutoId, ReturnOnInsert]
	public int Id
	{
		get;
		set;
	}
	
	public string Name
	{
		get;
		set;
	}

    public int User
    {
        get;
        set;
    }

    public int Group
    {
        get;
        set;
    }
}