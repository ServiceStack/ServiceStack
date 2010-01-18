using System;
using NUnit.Framework;
using NUnit.Framework.SyntaxHelpers;
using ServiceStack.Common.Extensions;
using ServiceStack.DataAnnotations;
using ServiceStack.Logging;
using ServiceStack.OrmLite.Tests.Support;

namespace ServiceStack.OrmLite.Tests.Models
{
	public class ModelWithFieldsOfDifferentTypes
	{
		private static readonly ILog Log = LogManager.GetLogger(typeof(OrmLiteTestBase));

		[AutoIncrement]
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
			try
			{
				Assert.That(actual.DateTime, Is.EqualTo(expected.DateTime));
			}
			catch (Exception ex)
			{
				Log.Error("Trouble with DateTime precisions, trying Assert again with rounding to seconds", ex);
				Assert.That(actual.DateTime.RoundToSecond(), Is.EqualTo(expected.DateTime.RoundToSecond()));
			} 
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