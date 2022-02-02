using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ServiceStack.DataAnnotations;

namespace ServiceStack.OrmLite.SqlServerTests.UseCase
{
    public class TestEntity
    {
        #region Properties

        [AutoIncrement]
        public int Id { get; set; }

        public String Foo { get; set; }
        public String Bar { get; set; }
        public int? NullInt { get; set; }

        [Index]
        public Decimal Baz { get; set; }

        #endregion
    }
}
