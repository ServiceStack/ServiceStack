namespace Northwind.Common.DataModel
{
	public class OrderDetailBlob
	{
		public int ProductId { get; set; }

		public decimal UnitPrice { get; set; }

		public short Quantity { get; set; }

		public double Discount { get; set; }
	}
}