#if !NETCORE_SUPPORT
using System.Data;
using System.IO;
using NUnit.Framework;
using ServiceStack.Auth;
using ServiceStack.OrmLite;
using ServiceStack.Text;

namespace ServiceStack.Common.Tests.OAuth
{
	[TestFixture]
	public class OrmLiteUserAuthTests
	{
        private static IDbConnection OpenDbConnection()
        {
            OrmLiteConfig.DialectProvider = SqliteDialect.Provider;
            var connectionString = "~/App_Data/db.sqlite".MapAbsolutePath();
            if (File.Exists(connectionString))
                File.Delete(connectionString);

            var openDbConnection = connectionString.OpenDbConnection();
            return openDbConnection;
        }

        private static UserAuth GetUserAuth()
        {
            var jsv = "{Id:0,UserName:UserName,Email:as@if.com,PrimaryEmail:as@if.com,FirstName:FirstName,LastName:LastName,DisplayName:DisplayName,Salt:WMQi/g==,PasswordHash:oGdE40yKOprIgbXQzEMSYZe3vRCRlKGuqX2i045vx50=,Roles:[],Permissions:[],CreatedDate:2012-03-20T07:53:48.8720739Z,ModifiedDate:2012-03-20T07:53:48.8720739Z}";
            var userAuth = jsv.To<UserAuth>();
            return userAuth;
        }
        
        [Test]
		public void Can_insert_table_with_UserAuth()
        {
            using (var db = OpenDbConnection())
			{
				db.DropAndCreateTable<UserAuth>();

				var userAuth = GetUserAuth();

			    db.Insert(userAuth);

				var rows = db.Select<UserAuth>(q => q.UserName == "UserName");

				Assert.That(rows[0].UserName, Is.EqualTo(userAuth.UserName));
			}
        }

	}
}
#endif