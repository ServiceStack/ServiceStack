using System;
using ServiceStack.Text.Tests.DynamicModels.DataModel;

namespace ServiceStack.Text.Tests.DynamicModels
{
	public class ModelWithAllTypes
	{
		public Exception Exception { get; set; }

		public CustomException CustomException { get; set; }

		public Uri UriValue { get; set; }

		public Type TypeValue { get; set; }

		public char CharValue { get; set; }

		public byte ByteValue { get; set; }

		public sbyte SByteValue { get; set; }

		public short ShortValue { get; set; }

		public ushort UShortValue { get; set; }

		public int IntValue { get; set; }

		public uint UIntValue { get; set; }

		public long LongValue { get; set; }

		public ulong ULongValue { get; set; }

		public float FloatValue { get; set; }

		public double DoubleValue { get; set; }

		public decimal DecimalValue { get; set; }

		public DateTime DateTimeValue { get; set; }

		public TimeSpan TimeSpanValue { get; set; }

		public Guid GuidValue { get; set; }

		public static ModelWithAllTypes Create(byte i)
		{
			return new ModelWithAllTypes
			{
				ByteValue = i,
				CharValue = (char)i,
				CustomException = new CustomException("CustomException " + i),
				DateTimeValue = new DateTime(i),
				DecimalValue = i,
				DoubleValue = i,
				Exception = new Exception("Exception " + i),
				FloatValue = i,
				IntValue = i,
				LongValue = i,
				SByteValue = (sbyte)i,
				ShortValue = i,
				TimeSpanValue = new TimeSpan(i),
				TypeValue = typeof(ModelWithAllTypes),
				UIntValue = i,
				ULongValue = i,
				UriValue = new Uri("http://domain.com/" + i),
				UShortValue = i,
				GuidValue = Guid.NewGuid(),
			};
		}

	}
}