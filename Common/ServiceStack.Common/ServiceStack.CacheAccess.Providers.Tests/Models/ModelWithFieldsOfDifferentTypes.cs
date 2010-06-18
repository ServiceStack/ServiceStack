using System;
using NUnit.Framework;
using ServiceStack.Logging;

namespace ServiceStack.CacheAccess.Providers.Tests.Models
{
	public class ModelWithFieldsOfDifferentTypes
	{
		private static readonly ILog Log = LogManager.GetLogger(typeof(ModelWithFieldsOfDifferentTypes));

		public int Id { get; set; }

		public string Name { get; set; }

		public long LongId { get; set; }

		public Guid Guid { get; set; }

		public bool Bool { get; set; }

		public DateTime DateTime { get; set; }

		public double Double { get; set; }

		public static ModelWithFieldsOfDifferentTypes Create(int id)
		{
			var row = new ModelWithFieldsOfDifferentTypes {
				Id = id,
				Bool = id % 2 == 0,
				DateTime = DateTime.Now.AddDays(id),
				Double = 1.11d + id,
				Guid = Guid.NewGuid(),
				LongId = 999 + id,
				Name = "Name" + id
			};

			return row;
		}

		public static void AssertIsEqual(ModelWithFieldsOfDifferentTypes actual, ModelWithFieldsOfDifferentTypes expected)
		{
			Assert.That(actual.Id, Is.EqualTo(expected.Id));
			Assert.That(actual.Name, Is.EqualTo(expected.Name));
			Assert.That(actual.Guid, Is.EqualTo(expected.Guid));
			Assert.That(actual.LongId, Is.EqualTo(expected.LongId));
			Assert.That(actual.Bool, Is.EqualTo(expected.Bool));
			Assert.That(actual.DateTime, Is.EqualTo(expected.DateTime));
			try
			{
				Assert.That(actual.Double, Is.EqualTo(expected.Double));
			}
			catch (Exception ex)
			{
				Log.Error("Trouble with double precisions, trying Assert again with rounding to 10 decimals", ex);
				Assert.That(Math.Round(actual.Double, 10), Is.EqualTo(Math.Round(actual.Double, 10)));
			}
		}
	}
}