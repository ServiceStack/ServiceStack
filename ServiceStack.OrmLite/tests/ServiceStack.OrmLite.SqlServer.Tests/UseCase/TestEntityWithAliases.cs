using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ServiceStack.DataAnnotations;

namespace ServiceStack.OrmLite.SqlServerTests.UseCase
{
    public class TestEntityWithAliases
    {
        #region Properties

        [AutoIncrement]
        [Alias("Id Column")]
        public int Id { get; set; }

        [Alias("Foo Column")]
        public String Foo { get; set; }

        [Alias("Bar Column °")]
        public String Bar { get; set; }

        //[Index]
        [Alias("Baz Column")]
        public Decimal Baz { get; set; }
        
        #endregion
    }
}
