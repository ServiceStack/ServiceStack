namespace ServiceStack.Text.Tests.Shared
{
    public class ModelWithIntegerTypes
    {
        public byte Byte { get; set; }
        public short Short { get; set; }
        public int Int { get; set; }
        public long Long { get; set; }

        protected bool Equals(ModelWithIntegerTypes other)
        {
            return Byte == other.Byte && Short == other.Short && Int == other.Int && Long == other.Long;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((ModelWithIntegerTypes) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = Byte.GetHashCode();
                hashCode = (hashCode * 397) ^ Short.GetHashCode();
                hashCode = (hashCode * 397) ^ Int;
                hashCode = (hashCode * 397) ^ Long.GetHashCode();
                return hashCode;
            }
        }
    }
}