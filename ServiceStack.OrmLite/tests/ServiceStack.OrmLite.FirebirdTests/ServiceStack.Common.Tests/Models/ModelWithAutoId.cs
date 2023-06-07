using System;
using NUnit.Framework;
using ServiceStack.DataAnnotations;

namespace ServiceStack.Common.Tests.Models;

public class ModelWithAutoId
{
	[AutoId, ReturnOnInsert]
	public Guid Id
	{
		get;
		set;
	}
	
	public string Name
	{
		get;
		set;
	}
	
	public ModelWithAutoId()
	{
	}
	
	public ModelWithAutoId(Guid id)
	{
		this.Id = id;
		this.Name = string.Concat("Name_", id.ToString());
	}
	
	public static void AssertIsEqual(ModelWithAutoId actual, ModelWithAutoId expected)
	{
		if (actual == null || expected == null)
		{
			Assert.That(actual == expected, Is.True);
			return;
		}
		Assert.That(actual.Id, Is.EqualTo(expected.Id));
		Assert.That(actual.Name, Is.EqualTo(expected.Name));
	}
	
	public static ModelWithAutoId Create(Guid id)
	{
		return new ModelWithAutoId(id);
	}
	
	public bool Equals(ModelWithAutoId other)
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
		if (obj.GetType() != typeof(ModelWithAutoId))
		{
			return false;
		}
		return this.Equals((ModelWithAutoId)obj);
	}
}