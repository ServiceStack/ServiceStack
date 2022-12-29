using System;
using System.Globalization;
using System.Threading;
using NUnit.Framework;
using ServiceStack.DataAnnotations;
using ServiceStack.OrmLite.Firebird;
using ServiceStack.Text;

namespace ServiceStack.OrmLite.FirebirdTests
{
	[TestFixture]
	public class LocalizationTests
		: OrmLiteTestBase
	{
		private readonly CultureInfo CurrentCulture = Thread.CurrentThread.CurrentCulture;
		private readonly CultureInfo CurrentUICulture = Thread.CurrentThread.CurrentUICulture;

		[OneTimeSetUp]
		public new  void TestFixtureSetUp()
		{
			Thread.CurrentThread.CurrentCulture = new CultureInfo("vi-VN");
			Thread.CurrentThread.CurrentUICulture = new CultureInfo("vi-VN");
		}

		[OneTimeTearDown]
		public void TestFixtureTearDown()
		{
			Thread.CurrentThread.CurrentCulture = CurrentCulture;
			Thread.CurrentThread.CurrentUICulture = CurrentUICulture;
		}

		public class Point
		{
			[AutoIncrement]
			public int Id { get; set; }
			public short Width { get; set; }
			public float Height { get; set; }
			public double Top { get; set; }
			public decimal Left { get; set; }
		}

		[Test]
		public void Can_query_using_float_in_alernate_culuture()
		{
            using (var db = new OrmLiteConnectionFactory(ConnectionString, FirebirdDialect.Provider).Open())
			{
				db.CreateTable<Point>(true);

				db.Insert(new Point { Width = 4, Height = 1.123f, Top = 3.456d, Left = 2.345m});
								
				var points = db.Select<Point>();

				Console.WriteLine(points.Dump());

				Assert.That(points[0].Width, Is.EqualTo(4));
				Assert.That(points[0].Height, Is.EqualTo(1.123f));
				Assert.That(points[0].Top, Is.EqualTo(3.456d));
				Assert.That(points[0].Left, Is.EqualTo(2.345m));

                // returns no rows! FirebirdSql bug?
                points = db.Select<Point>("Height=@height", new { height = 1.123f });  
				
				Assert.That(points.Count>0);
			}
		}

	}
}
