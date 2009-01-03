using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Sakila.ServiceModel.Version100.Types
{
	[DataContract(Namespace = "http://schemas.servicestack.net/types/")]
	public class Film 
	{
		public Film()
		{
			this.Actors = new List<Actor>();
		}

		[DataMember]
		public int Id { get; set; }
		[DataMember]
		public string Title { get; set; }
		[DataMember]
		public string Description { get; set; }
		[DataMember]
		public int ReleaseYear { get; set; }
		[DataMember]
		public int RentalDuration { get; set; }
		[DataMember]
		public decimal RentalRate { get; set; }
		[DataMember]
		public int Length { get; set; }
		[DataMember]
		public decimal ReplacementCost { get; set; }
		[DataMember]
		public string Rating { get; set; }
		[DataMember]
		public string SpecialFeature { get; set; }
		[DataMember]
		public DateTime LastUpdate { get; set; }

		[DataMember]
		public string Language { get; set; }
		[DataMember]
		public string OriginalLanguage { get; set; }

		[DataMember]
		public List<Actor> Actors { get; set; }
		[DataMember]
		public List<string> Categories { get; set; }
	}
}