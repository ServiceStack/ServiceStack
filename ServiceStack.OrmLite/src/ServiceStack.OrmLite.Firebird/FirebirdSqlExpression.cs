using System.Collections.Generic;
using System.Linq.Expressions;

namespace ServiceStack.OrmLite.Firebird
{
    public class FirebirdSqlExpression<T> : SqlExpression<T>
    {
        public FirebirdSqlExpression(IOrmLiteDialectProvider dialectProvider) 
            : base(dialectProvider) {}

        protected override string ToUpdateFromStatement(System.Collections.Generic.List<System.Collections.Generic.KeyValuePair<FieldDefinition, string>> values) =>
            throw new System.NotSupportedException("Firebird does not support UPDATE ... FROM, use UpdateOnly() instead");

        protected override string GetSetOperator(string op) => op is "INTERSECT" or "EXCEPT"
            ? throw new System.NotSupportedException($"Firebird does not support {op}")
            : op;

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

