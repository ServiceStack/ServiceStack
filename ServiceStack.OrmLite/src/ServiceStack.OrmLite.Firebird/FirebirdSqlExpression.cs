using System.Collections.Generic;
using System.Linq.Expressions;

namespace ServiceStack.OrmLite.Firebird
{
    public class FirebirdSqlExpression<T> : SqlExpression<T>
    {
        public FirebirdSqlExpression(IOrmLiteDialectProvider dialectProvider) 
            : base(dialectProvider) {}

        protected override object VisitColumnAccessMethod(MethodCallExpression m)
        {
            var args = this.VisitExpressionList(m.Arguments);
            var quotedColName = Visit(m.Object);
            var statement = "";

            switch (m.Method.Name)
            {
                case "Trim":
                    statement = $"trim({quotedColName})";
                    break;
                case "LTrim":
                    statement = $"trim(leading from {quotedColName})";
                    break;
                case "RTrim":
                    statement = $"trim(trailing from {quotedColName})";
                    break;
                default:
                    return base.VisitColumnAccessMethod(m);
            }
            return new PartialSqlString(statement);
        }
    }
}

