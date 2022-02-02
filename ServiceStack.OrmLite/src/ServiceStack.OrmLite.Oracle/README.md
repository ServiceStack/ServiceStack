ServiceStack.OrmLite.Firebird : DialectProvider for FirebirdSql (http://firebirdsql.org), 
based on Demis Bellot's ServiceStack.OrmLite (http://servicestack.net)

Firebird  DialectProvider can be used in the same way as used SQLite or SqlServer provider.

Additionally, Firebird  DialectProvider implements class "SqlExpressionVisitor <T>" to translate  from csharp expressions to SQL code

Below is a complete example of how to use "SqlExpressionVisitor <T>" in OrmLite:

	public class Author{
		public Author(){}
		[AutoIncrement]
		public Int32 Id { get; set;}
		[Index(Unique = true)]
		[StringLength(40)]
		public string Name { get; set;}
		public DateTime Birthday { get; set;}
		public DateTime ? LastActivity  { get; set;}
		public Decimal? Earnings { get; set;}  
		public bool Active { get; set; } 
		[StringLength(80)]
		public string City { get; set;}
		[StringLength(80)]
		public string Comments { get; set;}
		public Int16 Rate{ get; set;}
	}
		
	class MainClass
	{
		public static void Main (string[] args)
		{
			Console.WriteLine ("Hello World!");
			
			OrmLiteConfig.DialectProvider = new FirebirdOrmLiteDialectProvider();
			SqlExpressionVisitor<Author> ev = OrmLiteConfig.DialectProvider.ExpressionVisitor<Author>();
									
			using (IDbConnection db =
			       "User=SYSDBA;Password=masterkey;Database=employee.fdb;DataSource=localhost;Dialect=3;charset=ISO8859_1;".OpenDbConnection())
			using ( IDbCommand dbCmd = db.CreateCommand())
			{
				dbCmd.DropTable<Author>();
				dbCmd.CreateTable<Author>();
				dbCmd.DeleteAll<Author>();
				
				List<Author> authors = new List<Author>();
				authors.Add(new Author(){Name="Demis Bellot",Birthday= DateTime.Today.AddYears(-20),Active=true,Earnings= 99.9m,Comments="CSharp books", Rate=10, City="London"});
				authors.Add(new Author(){Name="Angel Colmenares",Birthday= DateTime.Today.AddYears(-25),Active=true,Earnings= 50.0m,Comments="CSharp books", Rate=5, City="Bogota"});
				authors.Add(new Author(){Name="Adam Witco",Birthday= DateTime.Today.AddYears(-20),Active=true,Earnings= 80.0m,Comments="Math Books", Rate=9, City="London"});
				authors.Add(new Author(){Name="Claudia Espinel",Birthday= DateTime.Today.AddYears(-23),Active=true,Earnings= 60.0m,Comments="Cooking books", Rate=10, City="Bogota"});
				authors.Add(new Author(){Name="Libardo Pajaro",Birthday= DateTime.Today.AddYears(-25),Active=true,Earnings= 80.0m,Comments="CSharp books", Rate=9, City="Bogota"});
				authors.Add(new Author(){Name="Jorge Garzon",Birthday= DateTime.Today.AddYears(-28),Active=true,Earnings= 70.0m,Comments="CSharp books", Rate=9, City="Bogota"});
				authors.Add(new Author(){Name="Alejandro Isaza",Birthday= DateTime.Today.AddYears(-20),Active=true,Earnings= 70.0m,Comments="Java books", Rate=0, City="Bogota"});
				authors.Add(new Author(){Name="Wilmer Agamez",Birthday= DateTime.Today.AddYears(-20),Active=true,Earnings= 30.0m,Comments="Java books", Rate=0, City="Cartagena"});
				authors.Add(new Author(){Name="Rodger Contreras",Birthday= DateTime.Today.AddYears(-25),Active=true,Earnings= 90.0m,Comments="CSharp books", Rate=8, City="Cartagena"});
				authors.Add(new Author(){Name="Chuck Benedict",Birthday= DateTime.Today.AddYears(-22),Active=true,Earnings= 85.5m,Comments="CSharp books", Rate=8, City="London"});
				authors.Add(new Author(){Name="James Benedict II",Birthday= DateTime.Today.AddYears(-22),Active=true,Earnings= 85.5m,Comments="Java books", Rate=5, City="Berlin"});
				authors.Add(new Author(){Name="Ethan Brown",Birthday= DateTime.Today.AddYears(-20),Active=true,Earnings= 45.0m,Comments="CSharp books", Rate=5, City="Madrid"});
				authors.Add(new Author(){Name="Xavi Garzon",Birthday= DateTime.Today.AddYears(-22),Active=true,Earnings= 75.0m,Comments="CSharp books", Rate=9, City="Madrid"});
				authors.Add(new Author(){Name="Luis garzon",Birthday= DateTime.Today.AddYears(-22),Active=true,Earnings= 85.0m,Comments="CSharp books", Rate=10, City="Mexico"});
				
				dbCmd.InsertAll(authors);
							
				// lets start !
				
				// select authors born 20 year ago
				int year = DateTime.Today.AddYears(-20).Year;
				int expected=5;
				
				ev.Where(rn=> rn.Birthday>=new DateTime(year, 1,1) && rn.Birthday<=new DateTime(year, 12,31));
				List<Author> result=dbCmd.Select(ev);
				Console.WriteLine(ev.WhereExpression);
				Console.WriteLine("Expected:{0} ; Selected:{1}, OK? {2}", expected, result.Count, expected==result.Count);
				
				// select authors from London, Berlin and Madrid : 6
				expected=6;
				ev.Where(rn=> Sql.In( rn.City, new object[]{"London", "Madrid", "Berlin"}) );
				result=dbCmd.Select(ev);
				Console.WriteLine(ev.WhereExpression);
				Console.WriteLine("Expected:{0} ; Selected:{1}, OK? {2}", expected, result.Count, expected==result.Count);
				
				// select authors from Bogota and Cartagena : 7
				expected=7;
				List<object> cities = new List<object>(new object[]{"Bogota", "Cartagena"}  ); //works only object..
				ev.Where(rn=> Sql.In( rn.City, cities) );
				result=dbCmd.Select(ev);
				Console.WriteLine(ev.WhereExpression);
				Console.WriteLine("Expected:{0} ; Selected:{1}, OK? {2}", expected, result.Count, expected==result.Count);
				
				
				// select authors which name starts with A
				expected=3;
				ev.Where(rn=>  rn.Name.StartsWith("A") );
				result=dbCmd.Select(ev);
				Console.WriteLine(ev.WhereExpression);
				Console.WriteLine("Expected:{0} ; Selected:{1}, OK? {2}", expected, result.Count, expected==result.Count);
				
				// select authors which name ends with Garzon o GARZON o garzon ( no case sensitive )
				expected=3;
				ev.Where(rn=>  rn.Name.ToUpper().EndsWith("GARZON") );
				result=dbCmd.Select(ev);
				Console.WriteLine(ev.WhereExpression);
				Console.WriteLine("Expected:{0} ; Selected:{1}, OK? {2}", expected, result.Count, expected==result.Count);
				
				// select authors which name ends with garzon ( case sensitive )
				expected=1;
				ev.Where(rn=>  rn.Name.EndsWith("garzon") );
				result=dbCmd.Select(ev);
				Console.WriteLine(ev.WhereExpression);
				Console.WriteLine("Expected:{0} ; Selected:{1}, OK? {2}", expected, result.Count, expected==result.Count);
				
				
				// select authors which name contains  Benedict 
				expected=2;
				ev.Where(rn=>  rn.Name.Contains("Benedict") );
				result=dbCmd.Select(ev);
				Console.WriteLine(ev.WhereExpression);
				Console.WriteLine("Expected:{0} ; Selected:{1}, OK? {2}", expected, result.Count, expected==result.Count);
				
				
				// select authors with Earnings <= 50 
				expected=3;
				ev.Where(rn=>  rn.Earnings<=50 );
				result=dbCmd.Select(ev);
				Console.WriteLine(ev.WhereExpression);
				Console.WriteLine("Expected:{0} ; Selected:{1}, OK? {2}", expected, result.Count, expected==result.Count);
				
				// select authors with Rate = 10 and city=Mexio 
				expected=1;
				ev.Where(rn=>  rn.Rate==10 && rn.City=="Mexico");
				result=dbCmd.Select(ev);
				Console.WriteLine(ev.WhereExpression);
				Console.WriteLine("Expected:{0} ; Selected:{1}, OK? {2}", expected, result.Count, expected==result.Count);
			
				//  enough selecting, lets udpate;
				
				// set Active=false where rate =0
				expected=2;
				ev.Where(rn=>  rn.Rate==0 ).Update(rn=> rn.Active);
				var rows = dbCmd.Update( new Author(){ Active=false }, ev);
				Console.WriteLine(ev.WhereExpression);
				Console.WriteLine("Expected:{0} ; Selected:{1}, OK? {2}", expected, rows, expected==rows);
			
				// insert values  only in Id, Name, Birthday, Rate and Active fields 
				expected=4;
				ev.Insert(rn =>new { rn.Id, rn.Name, rn.Birthday, rn.Active, rn.Rate} );
				dbCmd.Insert( new Author(){Active=false, Rate=0, Name="Victor Grozny", Birthday=DateTime.Today.AddYears(-18)   }, ev);
				dbCmd.Insert( new Author(){Active=false, Rate=0, Name="Ivan Chorny", Birthday=DateTime.Today.AddYears(-19)   }, ev);
				ev.Where(rn=> !rn.Active);
				result=dbCmd.Select(ev);
				Console.WriteLine(ev.WhereExpression);
				Console.WriteLine("Expected:{0} ; Selected:{1}, OK? {2}", expected, result.Count, expected==result.Count);
				
				// delete where City is null 
				expected=2;
				ev.Where( rn => rn.City==null );
				rows = dbCmd.Delete( ev);
				Console.WriteLine(ev.WhereExpression);
				Console.WriteLine("Expected:{0} ; Selected:{1}, OK? {2}", expected, rows, expected==rows);
			
				
				//   lets select  all records ordered by Rate Descending and Name Ascending
				expected=14;
				ev.Where().OrderBy(rn=> new{ at=Sql.Desc(rn.Rate), rn.Name }); // clear where condition
				result=dbCmd.Select(ev);
				Console.WriteLine(ev.WhereExpression);
				Console.WriteLine("Expected:{0} ; Selected:{1}, OK? {2}", expected, result.Count, expected==result.Count);
				Console.WriteLine(ev.OrderByExpression);
				var author = result.FirstOrDefault();
				Console.WriteLine("Expected:{0} ; Selected:{1}, OK? {2}", "Claudia Espinel", author.Name, "Claudia Espinel"==author.Name);
				
				// select  only first 5 rows ....
				expected=5;
				ev.Limit(5); // note: order is the same as in the last sentence
				result=dbCmd.Select(ev);
				Console.WriteLine(ev.WhereExpression);
				Console.WriteLine("Expected:{0} ; Selected:{1}, OK? {2}", expected, result.Count, expected==result.Count);
						
				// and finally lets select only Name and City (name will be "UPPERCASED" )
				ev.Select(rn=> new { at= Sql.As( rn.Name.ToUpper(), "Name" ), rn.City} );
				Console.WriteLine(ev.SelectExpression);
				result=dbCmd.Select(ev);
				author = result.FirstOrDefault();
				Console.WriteLine("Expected:{0} ; Selected:{1}, OK? {2}", "Claudia Espinel".ToUpper(), author.Name, "Claudia Espinel".ToUpper()==author.Name);
				
				
				Console.ReadLine();
				Console.WriteLine("Press Enter to continue");
				
			}
			
			Console.WriteLine ("This is The End my friend!");
		}
	}

see full project in :
https://github.com/aicl/ServiceStack.OrmLite/tree/master/src/FirebirdTests/TestExpressions

For usage samples see :

https://github.com/aicl/ServiceStack.OrmLite/tree/master/src/FirebirdTests/TestLiteFirebird00

https://github.com/aicl/ServiceStack.OrmLite/tree/master/src/FirebirdTests/TestSimpleFirebird01

https://github.com/aicl/ServiceStack.OrmLite/tree/master/src/FirebirdTests/TestSimpleFirebird02

https://github.com/aicl/ServiceStack.OrmLite/tree/master/src/FirebirdTests/TestSimpleFirebird03

https://github.com/aicl/ServiceStack.OrmLite/tree/master/src/FirebirdTests/TestSimpleFirebird04

https://github.com/aicl/ServiceStack.OrmLite/tree/master/src/FirebirdTests/TestSimpleFirebirdProcedures


If you already have a firebird database you can use ClassWriter to generate CSharp code for each table.
see sample at : https://github.com/aicl/ServiceStack.OrmLite/tree/master/src/FirebirdTests/TestClassWriter

To run samples you must use employee.fdb from https://github.com/aicl/ServiceStack.OrmLite/tree/master/src/ServiceStack.OrmLite.Firebird/App_Data/employee.fdb.tar.gz

add this line to firebird aliases.conf: employee.fdb = /path/to/your/employee.fdb



Unit tests at:

https://github.com/aicl/ServiceStack.OrmLite/tree/master/tests/ServiceStack.OrmLite.FirebirdTests

for unit test you need ormlite-tests.fdb from https://github.com/aicl/ServiceStack.OrmLite/tree/master/src/ServiceStack.OrmLite.Firebird/App_Data/ormlite-tests.fdb.tar.gz

add this line to firebird aliases.conf: ormlite-tests.fdb = /path/to/your/ormlite-tests.fdb
