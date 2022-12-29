using System;
using ServiceStack.Text;

namespace ServiceStack.OrmLite.Firebird
{
    public class Firebird4NamingStrategy : FirebirdNamingStrategy
    {
        public Firebird4NamingStrategy() : base(63) { }
    }
}