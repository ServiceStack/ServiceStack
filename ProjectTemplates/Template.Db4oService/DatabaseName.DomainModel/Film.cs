using System;
using System.Collections.Generic;

namespace @DomainModelNamespace@
{
	public class Film : Entity 
	{
		public Film()
		{
			this.Actors = new List<Actor>();
		}

		public string Title { get; set; } 
		public string Description { get; set; }
		public int ReleaseYear { get; set; }
		public int RentalDuration { get; set; }
		public decimal RentalRate { get; set; }
		public int Length { get; set; }
		public decimal ReplacementCost { get; set; }
		public string Rating { get; set; }
		public string SpecialFeature { get; set; }
		public DateTime LastUpdate { get; set; }

		public string Language { get; set; }
		public string OriginalLanguage { get; set; }

		public List<Actor> Actors { get; set; }
		public List<string> Categories { get; set; }
	}

}