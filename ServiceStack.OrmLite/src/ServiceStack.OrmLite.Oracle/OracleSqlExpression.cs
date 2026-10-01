using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace ServiceStack.OrmLite.Oracle
{
    public class OracleSqlExpression<T> : SqlExpression<T>
    {
        public OracleSqlExpression(IOrmLiteDialectProvider dialectProvider)
            : base(dialectProvider) {}

        protected override string ToUpdateFromStatement(System.Collections.Generic.List<System.Collections.Generic.KeyValuePair<FieldDefinition, string>> values) =>
            throw new System.NotSupportedException("Oracle does not support UPDATE ... FROM, use UpdateOnly() instead");

        // Oracle doesn't use the RECURSIVE keyword
        protected override string WithRecursiveKeyword => "WITH";

        // Oracle uses MINUS for EXCEPT
        protected override string GetSetOperator(string op) => op == "EXCEPT" ? "MINUS" : op;

        protected override object VisitColumnAccessMethod(MethodCallExpression m)
        {
            if (m.Method.Name == "Substring")
            {
                List<Object> args = VisitExpressionList(m.Arguments);
                var quotedColName = Visit(m.Object);
                var startIndex = Int32.Parse(args[0].ToString()) + 1;
                if (args.Count == 2)
                {
                    var length = Int32.Parse(args[1].ToString());
                    return new PartialSqlString(string.Format(
                        "subStr({0},{1},{2})", quotedColName, startIndex, length));
                }

                return new PartialSqlString(string.Format(
                    "subStr({0},{1})", quotedColName, startIndex));
            }
            return base.VisitColumnAccessMethod(m);
        }

        protected override void VisitFilter(string operand, object originalLeft, object originalRight, ref object left, ref object right)
        {
            if (originalRight is DateTimeOffset)
                return;

            base.VisitFilter(operand, originalLeft, originalRight, ref left, ref right);
        }

        protected override void ConvertToPlaceholderAndParameter(ref object right)
        {
            var paramName = NextParamName();
            var paramValue = right;

            var parameter = CreateParam(paramName, paramValue);
            Params.Add(parameter);

            right = parameter.ParameterName;
        }

        protected override PartialSqlString ToLengthPartialString(object arg)
        {
            return new PartialSqlString($"LENGTH({arg})");
        }
    }
}

