using System;
using ServiceStack.Text;

namespace ServiceStack
{
    public class GreaterCondition : QueryCondition
    {
        public override bool Match(object a, object b)
        {
            return CompareTo(a, b) > 0;
        }
    }
    public class GreaterEqualCondition : QueryCondition
    {
        public override bool Match(object a, object b)
        {
            return CompareTo(a, b) >= 0;
        }
    }
    public class LessCondition : QueryCondition
    {
        public override bool Match(object a, object b)
        {
            return CompareTo(a, b) < 0;
        }
    }
    public class LessEqualCondition : QueryCondition
    {
        public override bool Match(object a, object b)
        {
            return CompareTo(a, b) <= 0;
        }
    }
    public class NotEqualCondition : QueryCondition
    {
        public override bool Match(object a, object b)
        {
            return CompareTo(a, b) != 0;
        }
    }
    public class CaseInsensitiveEqualCondition : QueryCondition
    {
        public override bool Match(object a, object b)
        {
            var aString = CoerceString(a);
            var bString = CoerceString(b);
            return string.Compare(aString, bString, StringComparison.InvariantCultureIgnoreCase) == 0;
        }
    }
    public class InCollectionCondition : QueryCondition, IHasMultipleValues
    {
        public override bool Match(object a, object b)
        {
            return false;
        }
    }
    public class InBetweenCondition : QueryCondition, IHasMultipleValues
    {
        public override bool Match(object a, object b)
        {
            return false;
        }
    }
    public class StartsWithCondition : QueryCondition
    {
        public override bool Match(object a, object b)
        {
            var aString = CoerceString(a);
            var bString = CoerceString(b);
            return aString.StartsWith(bString, StringComparison.InvariantCultureIgnoreCase);
        }
    }
    public class ContainsCondition : QueryCondition
    {
        public override bool Match(object a, object b)
        {
            var aString = CoerceString(a);
            var bString = CoerceString(b);
            return aString.IndexOf(bString, StringComparison.InvariantCultureIgnoreCase) >= 0;
        }
    }
    public class EndsWithCondition : QueryCondition
    {
        public override bool Match(object a, object b)
        {
            var aString = CoerceString(a);
            var bString = CoerceString(b);
            return aString.EndsWith(bString, StringComparison.InvariantCultureIgnoreCase);
        }
    }

    public interface IHasMultipleValues {}

    public abstract class QueryCondition
    {
        public virtual QueryTerm Term { get; set; }

        public abstract bool Match(object a, object b);

        public virtual int CompareTo(object a, object b)
        {
            if (a == null || b == null)
            {
                return a == null && b == null
                    ? 0
                    : a == null
                        ? -1
                        : 1;
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

        public virtual long? CoerceLong(object o)
        {
            return (long?)(o.GetType().IsIntegerType()
                ? Convert.ChangeType(o, TypeCode.Int64)
                : null);
        }

        public virtual double? CoerceDouble(object o)
        {
            return (long?)(o.GetType().IsRealNumberType()
                ? Convert.ChangeType(o, TypeCode.Double)
                : null);
        }

        public virtual string CoerceString(object o)
        {
            return TypeSerializer.SerializeToString(o);
        }
    }
}