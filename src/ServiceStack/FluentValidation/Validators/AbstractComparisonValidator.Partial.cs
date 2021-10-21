using System;

namespace ServiceStack.FluentValidation.Validators
{
    public abstract partial class AbstractComparisonValidator
    {
        // Migration: comment out GetComparisonValue() to use modified version below:
        public IComparable GetComparisonValue(PropertyValidatorContext context) {
            if(_valueToCompareFunc != null) {
                return (IComparable)_valueToCompareFunc(context.InstanceToValidate);
            }

            return GetComparableValue(context, ValueToCompare);
        }

        public IComparable GetComparableValue(PropertyValidatorContext context, object value)
        {
            if (context.PropertyValue == null || context.PropertyValue.GetType() == value.GetType())
                return (IComparable) value;
            return (IComparable)value.ConvertTo(context.PropertyValue.GetType());
        }
    }
}