namespace Sakila.DomainModel
{
	public class Address : Entity
	{
		public string Line1 { get; set; }
		public string Line2 { get; set; }
		public string Town { get; set; }
		public City City { get; set; }
		public string PostCode { get; set; }
	}
}