namespace ServiceStack.Text.Tests.Support
{
    public class NumberTypes
    {
        public int Int { get; set; }
        public float Float { get; set; }
        public double Double { get; set; }
        public decimal Decimal { get; set; }

        public NumberTypes(double num = 0)
        {
            Int = (int) num;
            Float = (float) num;
            Double = num;
            Decimal = (decimal) num;
        }

        protected bool Equals(NumberTypes other)
        {
            return Int == other.Int && Float.Equals(other.Float) && Double.Equals(other.Double) && Decimal == other.Decimal;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((NumberTypes) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = Int;
                hashCode = (hashCode*397) ^ Float.GetHashCode();
                hashCode = (hashCode*397) ^ Double.GetHashCode();
                hashCode = (hashCode*397) ^ Decimal.GetHashCode();
                return hashCode;
            }
        }
    }
}