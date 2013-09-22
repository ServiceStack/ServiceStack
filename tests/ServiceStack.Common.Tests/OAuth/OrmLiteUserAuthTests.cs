using System;
using System.IO;
using NUnit.Framework;
using ServiceStack.OrmLite;
using ServiceStack.OrmLite.Sqlite;
using ServiceStack.ServiceInterface.Auth;
using ServiceStack.Text;
using ServiceStack.Utils;

namespace ServiceStack.Common.Tests.OAuth
{
	[TestFixture]
	public class OrmLiteUserAuthTests
	{
		[Test]
		public void Can_insert_table_with_UserAuth()
		{
			OrmLiteConfig.DialectProvider = SqliteOrmLiteDialectProvider.Instance;
			var connectionString = "~/App_Data/db.sqlite".MapAbsolutePath();
			if (File.Exists(connectionString))
				File.Delete(connectionString);

			using (var db = connectionString.OpenDbConnection())
			{
				db.CreateTable<UserAuth>(true);

				//var userAuth = new UserAuth {
				//    Id = 1,
				//    UserName = "UserName",
				//    Email = "a@b.com",
				//    PrimaryEmail = "c@d.com",
				//    FirstName = "FirstName",
				//    LastName = "LastName",
				//    DisplayName = "DisplayName",
				//    Salt = "Salt",
				//    PasswordHash = "PasswordHash",
				//    CreatedDate = DateTime.Now,
				//    ModifiedDate = DateTime.UtcNow,
				//};

				var jsv = "{Id:0,UserName:UserName,Email:as@if.com,PrimaryEmail:as@if.com,FirstName:FirstName,LastName:LastName,DisplayName:DisplayName,Salt:WMQi/g==,PasswordHash:oGdE40yKOprIgbXQzEMSYZe3vRCRlKGuqX2i045vx50=,Roles:[],Permissions:[],CreatedDate:2012-03-20T07:53:48.8720739Z,ModifiedDate:2012-03-20T07:53:48.8720739Z}";
				var userAuth = jsv.To<UserAuth>();

				db.Insert(userAuth);

				var rows = db.Select<UserAuth>(q => q.UserName == "UserName");

				Console.WriteLine(rows[0].Dump());

				Assert.That(rows[0].UserName, Is.EqualTo(userAuth.UserName));
			}
		}
	}
}