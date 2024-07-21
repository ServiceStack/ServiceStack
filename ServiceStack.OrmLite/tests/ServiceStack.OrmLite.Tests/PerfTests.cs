using System.Diagnostics;
using NUnit.Framework;
using ServiceStack.DataAnnotations;
using ServiceStack.Text;

namespace ServiceStack.OrmLite.Tests;

public class ModelWithDifferentNumTypes
{
    [AutoIncrement]
    public int Id { get; set; }

    public short Short { get; set; }
    public int Int { get; set; }
    public long Long { get; set; }
    public float Float { get; set; }
    public double Double { get; set; }
    public decimal Decimal { get; set; }

    public static ModelWithDifferentNumTypes Create(int i)
    {
        return new ModelWithDifferentNumTypes
        {
            Short = (short)i,
            Int = i,
            Long = i,
            Float = (float)i * i * .1f,
            Double = (double)i * i * .1d,
            Decimal = (decimal)i * i * .1m,
        };
    }
}

[TestFixtureOrmLite, Explicit]
public class PerfTests(DialectContext context) : OrmLiteProvidersTestBase(context)
{
    [Test] 
    public void Is_GetValue_Slow()
    {
        using (var db = OpenDbConnection())
        {
            db.DropAndCreateTable<ModelWithDifferentNumTypes>();

            100.Times(x =>
                db.Insert(ModelWithDifferentNumTypes.Create(x)));

            int count = 0;

            var sw = Stopwatch.StartNew();
            100.Times(i => count += db.Select<ModelWithDifferentNumTypes>().Count);

            sw.ElapsedMilliseconds.ToString().Print();
        }
    }
}