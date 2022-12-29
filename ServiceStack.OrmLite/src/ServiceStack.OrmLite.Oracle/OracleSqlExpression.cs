using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace ServiceStack.OrmLite.Oracle
{
    public class OracleSqlExpression<T> : SqlExpression<T>
    {
        public OracleSqlExpression(IOrmLiteDialectProvider dialectProvider)
            : base(dialectProvider) {}

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
            var paramName = Params.Count.ToString();
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

