using System;

namespace ServiceStack.DataAnnotations
{
    public class RangeAttribute : AttributeBase
    {
        public object Minimum { get; private set; }
        public object Maximum { get; private set; }
        public Type OperandType { get; private set; }

        public RangeAttribute(int min, int max)
        {
            OperandType = typeof(int);
            Minimum = min;
            Maximum = max;
        }

        public RangeAttribute(double min, double max)
        {
            OperandType = typeof(double);
            Minimum = min;
            Maximum = max;
        }

        public RangeAttribute(Type type, string min, string max)
        {
            OperandType = type;
            Minimum = min;
            Maximum = max;
        }
    }
}
