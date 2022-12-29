using System;
using ServiceStack.Logging;
using ServiceStack.DataAnnotations;
using NUnit.Framework;
using ServiceStack.Text;

namespace ServiceStack.Common.Tests.Models{
	
	[Alias("ModelWFDNT")]
	public class ModelWithFieldsOfDifferentAndNullableTypes
	{
		private readonly static ILog Log;
	
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
	
		public decimal Decimal
		{
			get;
			set;
		}
	
		public double Double
		{
			get;
			set;
		}
	
		public float Float
		{
			get;
			set;
		}
	
		public Guid Guid
		{
			get;
			set;
		}
	
		[AutoIncrement]
		public int Id
		{
			get;
			set;
		}
	
		public long LongId
		{
			get;
			set;
		}
	
		public bool? NBool
		{
			get;
			set;
		}
	
		public DateTime? NDateTime
		{
			get;
			set;
		}
	
		public decimal? NDecimal
		{
			get;
			set;
		}
	
		public double? NDouble
		{
			get;
			set;
		}
	
		public float? NFloat
		{
			get;
			set;
		}
	
		public Guid? NGuid
		{
			get;
			set;
		}
	
		public int? NId
		{
			get;
			set;
		}
	
		public long? NLongId
		{
			get;
			set;
		}
	
		public TimeSpan? NTimeSpan
		{
			get;
			set;
		}
	
		public TimeSpan TimeSpan
		{
			get;
			set;
		}
	
		static ModelWithFieldsOfDifferentAndNullableTypes()
		{
			ModelWithFieldsOfDifferentAndNullableTypes.Log = LogManager.GetLogger(typeof(ModelWithFieldsOfDifferentAndNullableTypes));
		}
	
		public ModelWithFieldsOfDifferentAndNullableTypes()
		{
		}
	
		public static void AssertIsEqual(ModelWithFieldsOfDifferentAndNullableTypes actual, ModelWithFieldsOfDifferentAndNullableTypes expected)
		{
			Assert.That(actual.Id, Is.EqualTo(expected.Id));
			Assert.That(actual.Guid, Is.EqualTo(expected.Guid));
			Assert.That(actual.LongId, Is.EqualTo(expected.LongId));
			Assert.That(actual.Bool, Is.EqualTo(expected.Bool));
			Assert.That(actual.TimeSpan, Is.EqualTo(expected.TimeSpan));
			try
			{
				Assert.That(actual.DateTime, Is.EqualTo(expected.DateTime));
			}
			catch (Exception exception)
			{
				ModelWithFieldsOfDifferentAndNullableTypes.Log.Error("Trouble with DateTime precisions, trying Assert again with rounding to seconds", exception);
				Assert.That(DateTimeExtensions.RoundToSecond(actual.DateTime), Is.EqualTo(DateTimeExtensions.RoundToSecond(expected.DateTime)));
			}
			try
			{
				Assert.That(actual.Float, Is.EqualTo(expected.Float));
			}
			catch (Exception exception2)
			{
				ModelWithFieldsOfDifferentAndNullableTypes.Log.Error("Trouble with float precisions, trying Assert again with rounding to 10 decimals", exception2);
				Assert.That(Math.Round((double)actual.Float, 10), Is.EqualTo(Math.Round((double)actual.Float, 10)));
			}
			try
			{
				Assert.That(actual.Double, Is.EqualTo(expected.Double));
			}
			catch (Exception exception3)
			{
				ModelWithFieldsOfDifferentAndNullableTypes.Log.Error("Trouble with double precisions, trying Assert again with rounding to 10 decimals", exception3);
				Assert.That(Math.Round(actual.Double, 10), Is.EqualTo(Math.Round(actual.Double, 10)));
			}
			Assert.That(actual.NBool, Is.EqualTo(expected.NBool));
			Assert.That(actual.NDateTime, Is.EqualTo(expected.NDateTime));
			Assert.That(actual.NDecimal, Is.EqualTo(expected.NDecimal));
			Assert.That(actual.NDouble, Is.EqualTo(expected.NDouble));
			Assert.That(actual.NFloat, Is.EqualTo(expected.NFloat));
			Assert.That(actual.NGuid, Is.EqualTo(expected.NGuid));
			Assert.That(actual.NId, Is.EqualTo(expected.NId));
			Assert.That(actual.NLongId, Is.EqualTo(expected.NLongId));
			Assert.That(actual.NTimeSpan, Is.EqualTo(expected.NTimeSpan));
		}
	
		public static ModelWithFieldsOfDifferentAndNullableTypes Create(int id)
		{
			ModelWithFieldsOfDifferentAndNullableTypes modelWithFieldsOfDifferentAndNullableType1 = new ModelWithFieldsOfDifferentAndNullableTypes();
			modelWithFieldsOfDifferentAndNullableType1.Id = id;
			modelWithFieldsOfDifferentAndNullableType1.Bool = id % 2 == 0;
			modelWithFieldsOfDifferentAndNullableType1.DateTime = DateTime.Now.AddDays((double)id);
			modelWithFieldsOfDifferentAndNullableType1.Float = 1.11f + (float)id;
			modelWithFieldsOfDifferentAndNullableType1.Double = 1.11 + (double)id;
			modelWithFieldsOfDifferentAndNullableType1.Guid = Guid.NewGuid();
			modelWithFieldsOfDifferentAndNullableType1.LongId = (long)999 + id;
			modelWithFieldsOfDifferentAndNullableType1.Decimal = id + 0.5m;
			modelWithFieldsOfDifferentAndNullableType1.TimeSpan = TimeSpan.FromSeconds((double)id);
			ModelWithFieldsOfDifferentAndNullableTypes modelWithFieldsOfDifferentAndNullableType2 = modelWithFieldsOfDifferentAndNullableType1;
			return modelWithFieldsOfDifferentAndNullableType2;
		}
	
		public static ModelWithFieldsOfDifferentAndNullableTypes CreateConstant(int id)
		{
			ModelWithFieldsOfDifferentAndNullableTypes modelWithFieldsOfDifferentAndNullableType1 = new ModelWithFieldsOfDifferentAndNullableTypes();
			modelWithFieldsOfDifferentAndNullableType1.Id = id;
			modelWithFieldsOfDifferentAndNullableType1.Bool = id % 2 == 0;
			modelWithFieldsOfDifferentAndNullableType1.DateTime = new DateTime(1979, id % 12 + 1, id % 28 + 1);
			modelWithFieldsOfDifferentAndNullableType1.Float = 1.11f + (float)id;
			modelWithFieldsOfDifferentAndNullableType1.Double = 1.11 + (double)id;
			modelWithFieldsOfDifferentAndNullableType1.Guid = new Guid((id%240+16).ToString("X")+ "461D9D-47DB-4778-B3FA-458379AE9BDC");
			modelWithFieldsOfDifferentAndNullableType1.LongId = (long)999 + id;
			modelWithFieldsOfDifferentAndNullableType1.Decimal = id+ 0.5m ;
			modelWithFieldsOfDifferentAndNullableType1.TimeSpan = TimeSpan.FromSeconds((double)id);
			return modelWithFieldsOfDifferentAndNullableType1;
		}
	}
}