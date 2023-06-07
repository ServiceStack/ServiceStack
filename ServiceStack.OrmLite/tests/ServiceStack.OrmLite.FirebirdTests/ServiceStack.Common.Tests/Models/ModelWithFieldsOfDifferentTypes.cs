using System;
using NUnit.Framework;
using ServiceStack.DataAnnotations;
using ServiceStack.Logging;
using ServiceStack.Text;

namespace ServiceStack.Common.Tests.Models;

[Alias("ModelWFDT")]
public class ModelWithFieldsOfDifferentTypes
{
	private static readonly ILog Log = LogManager.GetLogger(typeof(ModelWithFieldsOfDifferentTypes));
	[AutoIncrement]
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
	public long LongId
	{
		get;
		set;
	}
	public Guid Guid
	{
		get;
		set;
	}
	public bool Bool
	{
		get;
		set;
	}
	public DateTime DateTime
	{
		get;
		set;
	}
	public double Double
	{
		get;
		set;
	}
	public static ModelWithFieldsOfDifferentTypes Create(int id)
	{
		return new ModelWithFieldsOfDifferentTypes
		{
			Id = id, 
			Bool = id % 2 == 0, 
			DateTime = DateTime.Now.AddDays((double)id), 
			Double = 1.11 + (double)id, 
			Guid = Guid.NewGuid(), 
			LongId = (long)(999 + id), 
			Name = "Name" + id
		};
	}
	public static ModelWithFieldsOfDifferentTypes CreateConstant(int id)
	{
		return new ModelWithFieldsOfDifferentTypes
		{
			Id = id, 
			Bool = id % 2 == 0, 
			DateTime = new DateTime(1979, id % 12 + 1, id % 28 + 1), 
			Double = 1.11 + (double)id, 
			Guid = new Guid((id % 240 + 16).ToString("X") + "726E3B-9983-40B4-A8CB-2F8ADA8C8760"), 
			LongId = (long)(999 + id), 
			Name = "Name" + id
		};
	}
	public override bool Equals(object obj)
	{
		ModelWithFieldsOfDifferentTypes other = obj as ModelWithFieldsOfDifferentTypes;
		if (other == null)
		{
			return false;
		}
		bool result;
		try
		{
			ModelWithFieldsOfDifferentTypes.AssertIsEqual(this, other);
			result = true;
		}
		catch (Exception)
		{
			result = false;
		}
		return result;
	}
	public override int GetHashCode()
	{
		return (this.Id + this.Guid.ToString()).GetHashCode();
	}
	public static void AssertIsEqual(ModelWithFieldsOfDifferentTypes actual, ModelWithFieldsOfDifferentTypes expected)
	{
		Assert.That(actual.Id, Is.EqualTo(expected.Id));
		Assert.That(actual.Name, Is.EqualTo(expected.Name));
		Assert.That(actual.Guid, Is.EqualTo(expected.Guid));
		Assert.That(actual.LongId, Is.EqualTo(expected.LongId));
		Assert.That(actual.Bool, Is.EqualTo(expected.Bool));
		try
		{
			Assert.That(actual.DateTime, Is.EqualTo(expected.DateTime));
		}
		catch (Exception ex)
		{
			ModelWithFieldsOfDifferentTypes.Log.Error("Trouble with DateTime precisions, trying Assert again with rounding to seconds", ex);
			Assert.That(DateTimeExtensions.RoundToSecond(actual.DateTime), Is.EqualTo(DateTimeExtensions.RoundToSecond(expected.DateTime)));
		}
		try
		{
			Assert.That(actual.Double, Is.EqualTo(expected.Double));
		}
		catch (Exception ex2)
		{
			ModelWithFieldsOfDifferentTypes.Log.Error("Trouble with double precisions, trying Assert again with rounding to 10 decimals", ex2);
			Assert.That(Math.Round(actual.Double, 10), Is.EqualTo(Math.Round(actual.Double, 10)));
		}
	}
}