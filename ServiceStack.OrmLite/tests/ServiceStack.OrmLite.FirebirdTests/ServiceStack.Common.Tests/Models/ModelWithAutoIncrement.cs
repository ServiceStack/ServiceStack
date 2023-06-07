using System;
using NUnit.Framework;
using ServiceStack.DataAnnotations;

namespace ServiceStack.Common.Tests.Models;

public class ModelWithAutoIncrement
{
	[AutoIncrement, ReturnOnInsert]
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
	
	public ModelWithAutoIncrement()
	{
	}
	
	public ModelWithAutoIncrement(long id)
	{
		this.Id = id;
		this.Name = string.Concat("Name_", id.ToString());
	}
	
	public static void AssertIsEqual(ModelWithAutoIncrement actual, ModelWithAutoIncrement expected)
	{
		if (actual == null || expected == null)
		{
			Assert.That(actual == expected, Is.True);
			return;
		}
		Assert.That(actual.Id, Is.EqualTo(expected.Id));
		Assert.That(actual.Name, Is.EqualTo(expected.Name));
	}
	
	public static ModelWithAutoIncrement Create(long id)
	{
		return new ModelWithAutoIncrement(id);
	}
	
	public bool Equals(ModelWithAutoIncrement other)
	{
		if (object.ReferenceEquals(null, other))
		{
			return false;
		}
		if (object.ReferenceEquals(this, other))
		{
			return true;
		}
		if (other.Id == this.Id)
		{
			return object.Equals(other.Name, this.Name);
		}
		return false;
	}
	
	public override bool Equals(object obj)
	{
		if (object.ReferenceEquals(null, obj))
		{
			return false;
		}
		if (object.ReferenceEquals(this, obj))
		{
			return true;
		}
		if (obj.GetType() != typeof(ModelWithAutoIncrement))
		{
			return false;
		}
		return this.Equals((ModelWithAutoIncrement)obj);
	}
}