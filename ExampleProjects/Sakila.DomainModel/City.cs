namespace Sakila.DomainModel
{
	public class City : Entity
	{
		public string Name { get; set; }
		public Country Country { get; set; }
	}
}