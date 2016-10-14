using System;
using System.Collections;
using ServiceStack.Text;

namespace ServiceStack
{
    public static class ConditionAlias
    {
        public new const string Equals = "=";
        public const string NotEqual = "<>";
        public const string LessEqual = "<=";
        public const string Less = "<";
        public const string Greater = ">";
        public const string GreaterEqual = ">=";
        public const string StartsWith = "StartsWith";
        public const string Contains = "Contains";
        public const string EndsWith = "EndsWith";
        public const string In = "In";
        public const string Between = "Between";
        public const string Like = "Like";
        public const string False = "false";
    }

    public class EqualsCondition : QueryCondition
    {
        public static EqualsCondition Instance = new EqualsCondition();

        public override string Alias => ConditionAlias.Equals;

        public override bool Match(object a, object b)
        {
            return CompareTo(a, b) == 0;
        }
    }
    public class NotEqualCondition : QueryCondition
    {
        public override string Alias => ConditionAlias.NotEqual;

        public override bool Match(object a, object b)
        {
            return CompareTo(a, b) != 0;
        }
    }
    public class GreaterCondition : QueryCondition
    {
        public override string Alias => ConditionAlias.Greater;

        public override bool Match(object a, object b)
        {
            if (a == null || b == null)
                return false;
            return CompareTo(a, b) > 0;
        }
    }
    public class GreaterEqualCondition : QueryCondition
    {
        public static GreaterEqualCondition Instance = new GreaterEqualCondition();

        public override string Alias => ConditionAlias.GreaterEqual;

        public override bool Match(object a, object b)
        {
            return CompareTo(a, b) >= 0;
        }
    }
    public class LessCondition : QueryCondition
    {
        public override string Alias => ConditionAlias.Less;

        public override bool Match(object a, object b)
        {
            return CompareTo(a, b) < 0;
        }
    }
    public class LessEqualCondition : QueryCondition
    {
        public static LessEqualCondition Instance = new LessEqualCondition();

        public override string Alias => ConditionAlias.LessEqual;

        public override bool Match(object a, object b)
        {
            return CompareTo(a, b) <= 0;
        }
    }
    public class CaseInsensitiveEqualCondition : QueryCondition
    {
        public override string Alias => ConditionAlias.Like;

        public override bool Match(object a, object b)
        {
            var aString = CompareTypeUtils.CoerceString(a);
            var bString = CompareTypeUtils.CoerceString(b);
            return string.Compare(aString, bString, StringComparison.OrdinalIgnoreCase) == 0;
        }
    }
    public class InCollectionCondition : QueryCondition, IQueryMultiple
    {
        public static InCollectionCondition Instance = new InCollectionCondition();

        public override string Alias => ConditionAlias.In;

        public override bool Match(object a, object b)
        {
            var bValues = b as IEnumerable;
            if (bValues == null)
                return EqualsCondition.Instance.Match(a, b);

            foreach (var item in bValues)
            {
                if (EqualsCondition.Instance.Match(a, item))
                    return true;
            }

            return false;
        }
    }
    public class InBetweenCondition : QueryCondition, IQueryMultiple
    {
        public override string Alias => ConditionAlias.Between;

        public override bool Match(object a, object b)
        {
            var bValues = b as IEnumerable;
            if (bValues == null)
                throw new ArgumentException("InBetweenCondition must be queried with multiple values");

            var bList = bValues.Map(x => x);
            if (bList.Count != 2)
                throw new ArgumentException($"InBetweenCondition expected 2 values, got {bList.Count} instead.");

            return GreaterEqualCondition.Instance.Match(a, bList[0]) &&
                   LessEqualCondition.Instance.Match(a, bList[1]);
        }
    }
    public class StartsWithCondition : QueryCondition
    {
        public override string Alias => ConditionAlias.StartsWith;

        public override bool Match(object a, object b)
        {
            var aString = CompareTypeUtils.CoerceString(a);
            var bString = CompareTypeUtils.CoerceString(b);
            if (aString == null || bString == null)
                return false;
            return aString.StartsWith(bString, StringComparison.OrdinalIgnoreCase);
        }
    }
    public class ContainsCondition : QueryCondition
    {
        public override string Alias => ConditionAlias.Contains;

        public override bool Match(object a, object b)
        {
            var aString = CompareTypeUtils.CoerceString(a);
            var bString = CompareTypeUtils.CoerceString(b);
            if (aString == null || bString == null)
                return false;
            return aString.IndexOf(bString, StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }
    public class EndsWithCondition : QueryCondition
    {
        public override string Alias => ConditionAlias.EndsWith;

        public override bool Match(object a, object b)
        {
            var aString = CompareTypeUtils.CoerceString(a);
            var bString = CompareTypeUtils.CoerceString(b);
            if (aString == null || bString == null)
                return false;
            return aString.EndsWith(bString, StringComparison.OrdinalIgnoreCase);
        }
    }
    public class AlwaysFalseCondition : QueryCondition
    {
        public static AlwaysFalseCondition Instance = new AlwaysFalseCondition();

        public override string Alias => ConditionAlias.False;

        public override bool Match(object a, object b)
        {
            return false;
        }
    }

    public interface IQueryMultiple {}

    public abstract class QueryCondition
    {
        public abstract string Alias { get; }

        public QueryTerm Term { get; set; }

        public abstract bool Match(object a, object b);

        public virtual int CompareTo(object a, object b)
        {
            return CompareTypeUtils.CompareTo(a, b);
        }
    }

    public static class CompareTypeUtils
    {
        public static int CompareTo(object a, object b)
        {
            if (a == null || b == null)
            {
                return a == null && b == null
                    ? 0
                    : a == null //NULL is lowest in RDBMS
                        ? 1
                        : -1;
            }

            if (a.GetType() == b.GetType())
            {
                var ac = a as IComparable;
                if (ac != null)
                    return ac.CompareTo(b);
            }

            var aLong = CoerceLong(a);
            if (aLong != null)
            {
                var bLong = CoerceLong(b);
                if (bLong != null)
                    return aLong.Value.CompareTo(bLong.Value);
            }

            var aDouble = CoerceDouble(a);
            if (aDouble != null)
            {
                var bDouble = CoerceDouble(b);
                if (bDouble != null)
                    return aDouble.Value.CompareTo(bDouble.Value);
            }

            var aString = CoerceString(a);
            var bString = CoerceString(b);
            return string.Compare(aString, bString, StringComparison.Ordinal);
        }

        public static long? CoerceLong(object o)
        {
            return (long?)(o.GetType().IsIntegerType()
                ? Convert.ChangeType(o, typeof(long))
                : null);
        }

        public static double? CoerceDouble(object o)
        {
            return (long?)(o.GetType().IsRealNumberType()
                ? Convert.ChangeType(o, typeof(double))
                : null);
        }

        public static string CoerceString(object o)
        {
            return TypeSerializer.SerializeToString(o);
        }

        public static object Add(object a, object b)
        {
            var aLong = CoerceLong(a);
            if (aLong != null)
            {
                var bLong = CoerceLong(b);
                return aLong + bLong ?? aLong;
            }

            var aDouble = CoerceDouble(a);
            if (aDouble != null)
            {
                var bDouble = CoerceDouble(b);
                return aDouble + bDouble ?? aDouble;
            }

            var aString = CoerceString(a);
            var bString = CoerceString(b);
            return aString + bString;
        }

        public static object Min(object a, object b)
        {
            if (a == null)
                return b;

            return CompareTo(a, b) > 0 ? b : a;
        }

        public static object Max(object a, object b)
        {
            if (a == null)
                return b;

            return CompareTo(a, b) < 0 ? b : a;
        }

        public static object Sum(IEnumerable values)
        {
            object sum = null;
            foreach (var value in values)
            {
                sum = sum == null
                    ? value
                    : Add(sum, value);
            }
            return sum;
        }

        public static object Aggregate(IEnumerable source, Func<object, object, object> fn, object seed = null)
        {
            var acc = seed;
            foreach (var item in source)
            {
                acc = fn(acc, item);
            }
            return acc;
        }
    }
}