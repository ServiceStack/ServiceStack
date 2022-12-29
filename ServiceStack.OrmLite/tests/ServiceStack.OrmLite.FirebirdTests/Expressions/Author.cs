using System;
using ServiceStack.DataAnnotations;

namespace ServiceStack.OrmLite.FirebirdTests.Expressions
{
	public class Author
	{
		public Author(){}
		[AutoIncrement]
		[Alias("AuthorID")]
		public Int32 Id { get; set;}
		[Index(Unique = true)]
		[StringLength(40)]
		public string Name { get; set;}
		public DateTime Birthday { get; set;}
		public DateTime ? LastActivity  { get; set;}
		public Decimal? Earnings { get; set;}  
		public bool Active { get; set; } 
		[StringLength(80)]
		[Alias("JobCity")]
		public string City { get; set;}
		[StringLength(80)]
		[Alias("Comment")]
		public string Comments { get; set;}
		public Int16 Rate{ get; set;}
	}
}

