using NUnit.Framework;

namespace ServiceStack.OrmLite.Kingbase.Tests;

public class KingbaseTestBase
{
    public OrmLiteConnectionFactory BuildOrmLiteConnectionFactory(IOrmLiteDialectProvider kingbaseDialectProvider)
    {
        var factory = new OrmLiteConnectionFactory(
            "User Id=kingbase;Password=Jnvision_2022_Kb;Server=192.168.110.231;Port=54321;Database=ormlite-test;",
            kingbaseDialectProvider);
        return factory;
    }
}