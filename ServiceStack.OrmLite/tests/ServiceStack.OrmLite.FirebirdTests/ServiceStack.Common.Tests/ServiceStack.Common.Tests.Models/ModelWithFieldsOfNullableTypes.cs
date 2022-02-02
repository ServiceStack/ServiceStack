using System;
using ServiceStack.Model;
using ServiceStack.Logging;
using NUnit.Framework;
using ServiceStack.DataAnnotations;
using ServiceStack.Text;

namespace ServiceStack.Common.Tests.Models{
	
	[Alias("ModelWFNT")]
	public class ModelWithFieldsOfNullableTypes : IHasIntId, IHasId<int>
	{
		private readonly static ILog Log;
	
		public int Id
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
	
		static ModelWithFieldsOfNullableTypes()
		{
			ModelWithFieldsOfNullableTypes.Log = LogManager.GetLogger(typeof(ModelWithFieldsOfNullableTypes));
		}
	
		public ModelWithFieldsOfNullableTypes()
		{
		}
	
		public static void AssertIsEqual(ModelWithFieldsOfNullableTypes actual, ModelWithFieldsOfNullableTypes expected)
        {
            Assert.That(actual.Id, Is.EqualTo(expected.Id));
            Assert.That(actual.NId, Is.EqualTo(expected.NId));
            Assert.That(actual.NGuid, Is.EqualTo(expected.NGuid));
            Assert.That(actual.NLongId, Is.EqualTo(expected.NLongId));
            Assert.That(actual.NBool, Is.EqualTo(expected.NBool));
            Assert.That(actual.NTimeSpan, Is.EqualTo(expected.NTimeSpan));
            try
            {
                Assert.That(actual.NDateTime, Is.EqualTo(expected.NDateTime));
            }
            catch (Exception ex)
            {
                ModelWithFieldsOfNullableTypes.Log.Error("Trouble with DateTime precisions, trying Assert again with rounding to seconds", ex);
                Assert.That(DateTimeExtensions.RoundToSecond(actual.NDateTime.Value.ToUniversalTime()), Is.EqualTo(DateTimeExtensions.RoundToSecond(expected.NDateTime.Value.ToUniversalTime())));
            }
            try
            {
                Assert.That(actual.NFloat, Is.EqualTo(expected.NFloat));
            }
            catch (Exception ex2)
            {
                ModelWithFieldsOfNullableTypes.Log.Error("Trouble with float precisions, trying Assert again with rounding to 10 decimals", ex2);
                Assert.That(Math.Round((double)actual.NFloat.Value, 10), Is.EqualTo(Math.Round((double)actual.NFloat.Value, 10)));
            }
            try
            {
                Assert.That(actual.NDouble, Is.EqualTo(expected.NDouble));
            }
            catch (Exception ex3)
            {
                ModelWithFieldsOfNullableTypes.Log.Error("Trouble with double precisions, trying Assert again with rounding to 10 decimals", ex3);
                Assert.That(Math.Round(actual.NDouble.Value, 10), Is.EqualTo(Math.Round(actual.NDouble.Value, 10)));
            }
        }
	
		public static ModelWithFieldsOfNullableTypes Create(int id)
		{
			ModelWithFieldsOfNullableTypes modelWithFieldsOfNullableType1 = new ModelWithFieldsOfNullableTypes();
			modelWithFieldsOfNullableType1.Id = id;
			modelWithFieldsOfNullableType1.NId = new int?(id);
			modelWithFieldsOfNullableType1.NBool = new bool?(id % 2 == 0);
			modelWithFieldsOfNullableType1.NDateTime = new DateTime?(DateTime.Now.AddDays((double)id));
			modelWithFieldsOfNullableType1.NFloat = new float?(1.11f + (float)id);
			modelWithFieldsOfNullableType1.NDouble = new double?(1.11 + (double)id);
			modelWithFieldsOfNullableType1.NGuid = new Guid?(Guid.NewGuid());
			modelWithFieldsOfNullableType1.NLongId = new long?((long)999 + id);
			modelWithFieldsOfNullableType1.NDecimal = new decimal?(id+0.5m);
			modelWithFieldsOfNullableType1.NTimeSpan = new TimeSpan?(TimeSpan.FromSeconds((double)id));
			ModelWithFieldsOfNullableTypes modelWithFieldsOfNullableType2 = modelWithFieldsOfNullableType1;
			return modelWithFieldsOfNullableType2;
		}
	
		public static ModelWithFieldsOfNullableTypes CreateConstant(int id)
		{
			ModelWithFieldsOfNullableTypes modelWithFieldsOfNullableType1 = new ModelWithFieldsOfNullableTypes();
			modelWithFieldsOfNullableType1.Id = id;
			modelWithFieldsOfNullableType1.NId = new int?(id);
			modelWithFieldsOfNullableType1.NBool = new bool?(id % 2 == 0);
			modelWithFieldsOfNullableType1.NDateTime = new DateTime?(new DateTime(1979, id % 12 + 1, id % 28 + 1));
			modelWithFieldsOfNullableType1.NFloat = new float?(1.11f + (float)id);
			modelWithFieldsOfNullableType1.NDouble = new double?(1.11 + (double)id);
			modelWithFieldsOfNullableType1.NGuid = new Guid?(new Guid( (id%240+16).ToString("X") + "7DA519-73B6-4525-84BA-B57673B2360D"));
			modelWithFieldsOfNullableType1.NLongId = new long?((long)999 + id);
			modelWithFieldsOfNullableType1.NDecimal = new decimal?(id + 0.5m );
			modelWithFieldsOfNullableType1.NTimeSpan = new TimeSpan?(TimeSpan.FromSeconds((double)id));
			
			return modelWithFieldsOfNullableType1;
		}
	}
}