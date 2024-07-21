using System;
using System.Data;
using System.Linq;
using NUnit.Framework;

namespace ServiceStack.OrmLite.Tests.Expression;

[NonParallelizable]
public abstract class ExpressionsTestBase(DialectContext context) : OrmLiteProvidersTestBase(context)
{
    [SetUp]
    public void Setup()
    {
        using (var db = OpenDbConnection())
            db.CreateTable<TestType>(true);

        db = OpenDbConnection();
    }

    //Avoid painful refactor to change all tests to use a using pattern
    private IDbConnection db;

//        public override IDbConnection OpenDbConnection()
//        {
//            try
//            {
//                if (db != null && db.State != ConnectionState.Open)
//                    db = null;
//            }
//            catch (ObjectDisposedException) //PostgreSQL throws when trying to inspect db.State on a disposed connection. WHY???
//            {
//                db = null;
//            }
//
//            return db ?? (db = base.OpenDbConnection());
//        }

    [TearDown]
    public void TearDown()
    {
        if (db == null)
            return;
        db.Dispose();
        db = null;
    }

    public T GetValue<T>(T item)
    {
        return item;
    }

    protected void Init(int numberOfRandomObjects)
    {
        Init(numberOfRandomObjects, null);
    }

    protected void Init(int numberOfRandomObjects, params TestType[] obj)
    {
        Init(null, numberOfRandomObjects, obj);
    }

    protected void Init(IDbConnection db, int numberOfRandomObjects, params TestType[] obj)
    {
        obj ??= Array.Empty<TestType>();

        var con = db ?? OpenDbConnection();
        foreach (var t in obj)
        {
            con.Insert(t);
        }

        var random = new Random((int)(DateTime.UtcNow.Ticks ^ (DateTime.UtcNow.Ticks >> 4)));
        for (var i = 0; i < numberOfRandomObjects; i++)
        {
            TestType o = null;

            while (o == null)
            {
                int intVal = random.Next();

                o = new TestType
                {
                    BoolColumn = random.Next() % 2 == 0,
                    IntColumn = intVal,
                    StringColumn = Guid.NewGuid().ToString()
                };

                if (obj.Any(x => x.IntColumn == intVal))
                    o = null;
            }

            con.Insert(o);
        }

        if (db == null)
            con.Dispose();
    }
}