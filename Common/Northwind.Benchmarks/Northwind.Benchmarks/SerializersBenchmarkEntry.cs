using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Northwind.Benchmarks
{
	public class SerializersBenchmarkEntry
	{
		public int Iterations { get; set; }
		public string ModelName { get; set; }
		public string SerializerName { get; set; }
		public int SerializedBytesLength { get; set; }
		public long TotalSerializationTicks { get; set; }
		public long TotalDeserializationTicks { get; set; }
		
		public bool Success
		{
			get
			{
				return this.TotalSerializationTicks > -1
				       && this.TotalDeserializationTicks > -1;
			}
		}

		public decimal AvgTicksPerIteration
		{
			get
			{
				return Math.Round(
					(this.TotalSerializationTicks + this.TotalDeserializationTicks)
					/ (decimal)this.Iterations, 4);
			}
		}
		
		public long TotalTicks
		{
			get
			{
				return this.TotalSerializationTicks + this.TotalDeserializationTicks;
			}
		}

		public decimal TimesSlowerThanBest { get; set; }
		public decimal TimesLargerThanBest { get; set; }
	}
}
