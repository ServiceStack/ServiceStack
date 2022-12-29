namespace ServiceStack.Text.Tests.Shared
{
    public class ModelWithFloatTypes
    {
        public float Float { get; set; }
        public double Double { get; set; }
        public decimal Decimal { get; set; }

        protected bool Equals(ModelWithFloatTypes other)
        {
            return Float.Equals(other.Float) 
                && Double.Equals(other.Double) 
                && Decimal == other.Decimal;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((ModelWithFloatTypes) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = Float.GetHashCode();
                hashCode = (hashCode * 397) ^ Double.GetHashCode();
                hashCode = (hashCode * 397) ^ Decimal.GetHashCode();
                return hashCode;
            }
        }
    }

    public class ModelWithNullableFloatTypes
    {
        public float? Float { get; set; }
        public double? Double { get; set; }
        public decimal? Decimal { get; set; }

        protected bool Equals(ModelWithNullableFloatTypes other)
        {
            return Float.Equals(other.Float) && Double.Equals(other.Double) && Decimal == other.Decimal;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((ModelWithNullableFloatTypes) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = Float.GetHashCode();
                hashCode = (hashCode * 397) ^ Double.GetHashCode();
                hashCode = (hashCode * 397) ^ Decimal.GetHashCode();
                return hashCode;
            }
        }
    }
}