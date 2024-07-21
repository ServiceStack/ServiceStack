using System;
using NUnit.Framework;

namespace ServiceStack.OrmLite.Tests;

[TestFixtureOrmLite, Explicit, Ignore("OneOff Tasks")]
[Obsolete("Would think this is redundant if spinning up clean dbs for each test run")]
public class AdminTasks(DialectContext context) : OrmLiteProvidersTestBase(context)
{
    [Test]
    public void Clean_Database()
    {
        using (var db = OpenDbConnection())
        {
            //db.DropTable<JoinSqlBuilderTests.User>();
            //db.DropTable<SqlBuilderTests.User>();
            //db.DropTable<AliasedFieldUseCase.User>();
            //db.DropTable<SchemaUseCase.User>();
            //db.DropTable<SimpleUseCase.User>();
        }
    }
}