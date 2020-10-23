using System;

namespace ServiceStack.FluentValidation.Validators
{
    public abstract partial class AbstractComparisonValidator
    {
        // Migration: replace GetComparisonValue(): `(IComparable)ValueToCompare` with `return GetComparableValue(context, ValueToCompare)`
        public IComparable GetComparableValue(PropertyValidatorContext context, object value)
        {
            if (context.PropertyValue == null || context.PropertyValue.GetType() == value.GetType())
                return (IComparable) value;
            return (IComparable)value.ConvertTo(context.PropertyValue.GetType());
        }
    }
}