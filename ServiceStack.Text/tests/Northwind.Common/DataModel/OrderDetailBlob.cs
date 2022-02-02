using System;

namespace Northwind.Common.DataModel
{
    public class OrderDetailBlob : IEquatable<OrderDetailBlob>
    {
        public int ProductId { get; set; }

        public decimal UnitPrice { get; set; }

        public short Quantity { get; set; }

        public double Discount { get; set; }

        public bool Equals(OrderDetailBlob other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return ProductId == other.ProductId && UnitPrice == other.UnitPrice && Quantity == other.Quantity && Discount.Equals(other.Discount);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((OrderDetailBlob) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = ProductId;
                hashCode = (hashCode*397) ^ UnitPrice.GetHashCode();
                hashCode = (hashCode*397) ^ Quantity.GetHashCode();
                hashCode = (hashCode*397) ^ Discount.GetHashCode();
                return hashCode;
            }
        }
    }
}