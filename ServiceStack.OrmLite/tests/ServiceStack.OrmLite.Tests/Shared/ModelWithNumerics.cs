using System.Collections.Generic;

namespace ServiceStack.OrmLite.Tests.Shared
{
    public class ModelWithNumerics
    {
        public int Id { get; set; }

        public byte Byte { get; set; }
        public sbyte SByte { get; set; }
        public short Short { get; set; }
        public ushort UShort { get; set; }
        public int Int { get; set; }
        public uint UInt { get; set; }
        public long Long { get; set; }
        public ulong ULong { get; set; }
        public float Float { get; set; }
        public double Double { get; set; }
        public decimal Decimal { get; set; }

        private sealed class ModelWithNumericsEqualityComparer : IEqualityComparer<ModelWithNumerics>
        {
            public bool Equals(ModelWithNumerics x, ModelWithNumerics y)
            {
                if (ReferenceEquals(x, y)) return true;
                if (ReferenceEquals(x, null)) return false;
                if (ReferenceEquals(y, null)) return false;
                if (x.GetType() != y.GetType()) return false;
                return x.Byte == y.Byte
                    && x.SByte == y.SByte
                    && x.Short == y.Short
                    && x.UShort == y.UShort
                    && x.Int == y.Int
                    && x.UInt == y.UInt
                    && x.Long == y.Long
                    && x.ULong == y.ULong
                    && x.Float.Equals(y.Float)
                    && x.Double.Equals(y.Double)
                    && x.Decimal == y.Decimal;
            }

            public int GetHashCode(ModelWithNumerics obj)
            {
                unchecked
                {
                    var hashCode = obj.Byte.GetHashCode();
                    hashCode = (hashCode * 397) ^ obj.SByte.GetHashCode();
                    hashCode = (hashCode * 397) ^ obj.Short.GetHashCode();
                    hashCode = (hashCode * 397) ^ obj.UShort.GetHashCode();
                    hashCode = (hashCode * 397) ^ obj.Int;
                    hashCode = (hashCode * 397) ^ (int)obj.UInt;
                    hashCode = (hashCode * 397) ^ obj.Long.GetHashCode();
                    hashCode = (hashCode * 397) ^ obj.ULong.GetHashCode();
                    hashCode = (hashCode * 397) ^ obj.Float.GetHashCode();
                    hashCode = (hashCode * 397) ^ obj.Double.GetHashCode();
                    hashCode = (hashCode * 397) ^ obj.Decimal.GetHashCode();
                    return hashCode;
                }
            }
        }

        private static readonly IEqualityComparer<ModelWithNumerics> ModelWithNumericsComparerInstance = new ModelWithNumericsEqualityComparer();

        public static IEqualityComparer<ModelWithNumerics> ModelWithNumericsComparer
        {
            get { return ModelWithNumericsComparerInstance; }
        }
    }
}