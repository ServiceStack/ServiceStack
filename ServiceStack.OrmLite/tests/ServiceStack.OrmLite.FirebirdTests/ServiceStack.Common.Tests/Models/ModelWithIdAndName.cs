using System;
using NUnit.Framework;
using ServiceStack.DataAnnotations;

namespace ServiceStack.Common.Tests.Models;

[Alias("ModelWIN")]
public class ModelWithIdAndName
{
	[Sequence("ModelWIN_Id_GEN")]
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
	
	public ModelWithIdAndName()
	{
	}
	
	public ModelWithIdAndName(int id)
	{
		this.Id = id;
		this.Name = string.Concat("Name", id);
	}
	
	public static void AssertIsEqual(ModelWithIdAndName actual, ModelWithIdAndName expected)
	{
		if (actual == null || expected == null)
		{
			Assert.That(actual == expected, Is.True);
			return;
		}
		Assert.That(actual.Id, Is.EqualTo(expected.Id));
		Assert.That(actual.Name, Is.EqualTo(expected.Name));
	}
	
	public static ModelWithIdAndName Create(int id)
	{
		return new ModelWithIdAndName(id);
	}
	
	public bool Equals(ModelWithIdAndName other)
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
		if (obj.GetType() != typeof(ModelWithIdAndName))
		{
			return false;
		}
		return this.Equals((ModelWithIdAndName)obj);
	}
	
	public override int GetHashCode()
	{
		return this.Id * 397 ^  ( (this.Name != null) ? this.Name.GetHashCode() : 0) ;
	}
}