using System;
using NUnit.Framework;
using ServiceStack.DataAnnotations;

namespace ServiceStack.OrmLite.Tests
{
    public class _Adhoc : OrmLiteTestBase
    {
        public _Adhoc() : base(Dialect.PostgreSql11) { }
    }
}