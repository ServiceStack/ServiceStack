using System;
using System.IO;
using System.Linq;
using NUnit.Framework;

namespace ServiceStack.OrmLite.Tests.Expressions
{
    public class ExpressionsTestBase : OrmLiteTestBase
    {
        [SetUp]
        public void Setup()
        {
            OpenDbConnection().CreateTable<TestType>(true);
        }

        public T GetValue<T>(T item)
        {
            return item;
        }

        protected void EstablishContext(int numberOfRandomObjects)
        {
            EstablishContext(numberOfRandomObjects, null);
        }

        protected void EstablishContext(int numberOfRandomObjects, params TestType[] obj)
        {
            if (obj == null)
                obj = new TestType[0];

            using (var con = OpenDbConnection())
            {
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
                                    BoolColumn = random.Next()%2 == 0,
                                    IntColumn = intVal,
                                    StringColumn = Guid.NewGuid().ToString()
                                };

                        if (obj.Any(x => x.IntColumn == intVal))
                            o = null;
                    }

                    con.Insert(o);
                }
            }
        }

        protected override string GetFileConnectionString()
        {
            var connectionString = Config.SqliteFileDir + this.GetType().Name + ".sqlite";
            if (File.Exists(connectionString))
                File.Delete(connectionString);

            return connectionString;
        }
    }
}