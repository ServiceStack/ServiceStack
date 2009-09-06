using System;

namespace ServiceStack.Common.Tests.Support
{
	public class ModelWithFieldsOfDifferentTypes
	{
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

	}
}