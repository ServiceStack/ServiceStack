using System;
using System.Runtime.Serialization;

namespace ServiceStack.WebHost.Endpoints.Tests.Support.Operations
{
	[DataContract]
	public class RequestOfAllTypes
	{
		[DataMember]
		public char Char { get; set; }

		[DataMember]
		public string String { get; set; }

		[DataMember]
		public byte Byte { get; set; }

		[DataMember]
		public short Short { get; set; }

		[DataMember]
		public int Int { get; set; }

		[DataMember]
		public uint UInt { get; set; }

		[DataMember]
		public long Long { get; set; }

		[DataMember]
		public ulong ULong { get; set; }

		[DataMember]
		public float Float { get; set; }

		[DataMember]
		public double Double { get; set; }

		[DataMember]
		public decimal Decimal { get; set; }

		[DataMember]
		public Guid Guid { get; set; }

		[DataMember]
		public DateTime DateTime { get; set; }

		[DataMember]
		public TimeSpan TimeSpan { get; set; }

		public static RequestOfAllTypes Create(int i)
		{
			return new RequestOfAllTypes {
				Byte = (byte)i,
				Char = (char)i,
				DateTime = new DateTime(i % 2000, 1, 1),
				Decimal = i,
				Double = i,
				Float = i,
				Guid = System.Guid.NewGuid(),
				Int = i,
				Long = i,
				Short = (short)i,
				String = i.ToString(),
				TimeSpan = new TimeSpan(i, 1, 1, 1),
				UInt = (uint)i,
				ULong = (ulong)i,
			};
		}

		public override bool Equals(object obj)
		{
			var other = obj as RequestOfAllTypes;
			if (other == null) return false;

			return this.Byte == other.Byte
				   && this.Char == other.Char
				   && this.DateTime == other.DateTime
				   && this.Decimal == other.Decimal
				   && this.Double == other.Double
				   && this.Float == other.Float
				   && this.Guid == other.Guid
				   && this.Int == other.Int
				   && this.Long == other.Long
				   && this.Short == other.Short
				   && this.String == other.String
				   && this.TimeSpan == other.TimeSpan
				   && this.UInt == other.UInt
				   && this.ULong == other.ULong;
		}

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
	}

	[DataContract]
	public class RequestOfAllTypesResponse
	{
		[DataMember]
		public char Char { get; set; }

		[DataMember]
		public string String { get; set; }

		[DataMember]
		public byte Byte { get; set; }

		[DataMember]
		public short Short { get; set; }

		[DataMember]
		public int Int { get; set; }

		[DataMember]
		public uint UInt { get; set; }

		[DataMember]
		public long Long { get; set; }

		[DataMember]
		public ulong ULong { get; set; }

		[DataMember]
		public float Float { get; set; }

		[DataMember]
		public double Double { get; set; }

		[DataMember]
		public decimal Decimal { get; set; }

		[DataMember]
		public Guid Guid { get; set; }

		[DataMember]
		public DateTime DateTime { get; set; }

		[DataMember]
		public TimeSpan TimeSpan { get; set; }
	}
}
