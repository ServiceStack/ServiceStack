using System;
using System.Runtime.CompilerServices;

namespace ServiceStack.Script
{
    // ReSharper disable InconsistentNaming
    
    public partial class DefaultScripts
    {
        public object add(object lhs, object rhs) => DynamicNumber.Add(lhs, rhs);
        public object sub(object lhs, object rhs) => DynamicNumber.Subtract(lhs, rhs);
        public object subtract(object lhs, object rhs) => DynamicNumber.Subtract(lhs, rhs);
        public object mul(object lhs, object rhs) => DynamicNumber.Multiply(lhs, rhs);
        public object multiply(object lhs, object rhs) => DynamicNumber.Multiply(lhs, rhs);
        public double div(double lhs, double rhs) => lhs / rhs;
        public double divide(double lhs, double rhs) => lhs / rhs;

        public long incr(long value) => value + 1;
        public long increment(long value) => value + 1;
        public long incrBy(long value, long by) => value + by;
        public long incrementBy(long value, long by) => value + by;
        public long decr(long value) => value - 1;
        public long decrement(long value) => value - 1;
        public long decrBy(long value, long by) => value - by;
        public long decrementBy(long value, long by) => value - by;
        public long mod(long value, long divisor) => value % divisor;

        public double pi() => Math.PI;
        public double e() => Math.E;
        public double floor(double value) => Math.Floor(value);
        public double ceiling(double value) => Math.Ceiling(value);
        public double abs(double value) => Math.Abs(value);
        public double acos(double value) => Math.Acos(value);
        public double atan(double value) => Math.Atan(value);
        public double atan2(double y, double x) => Math.Atan2(y, x);
        public double cos(double value) => Math.Cos(value);
        public double exp(double value) => Math.Exp(value);
        public double log(double value) => Math.Log(value);
        public double log(double a, double newBase) => Math.Log(a, newBase);
        public double log10(double value) => Math.Log10(value);
        public double pow(double x, double y) => Math.Pow(x, y);
        public double round(double value) => Math.Round(value);
        public double round(double value, int decimals) => Math.Round(value, decimals);
        public int sign(double value) => Math.Sign(value);
        public double sin(double value) => Math.Sin(value);
        public double sinh(double value) => Math.Sinh(value);
        public double sqrt(double value) => Math.Sqrt(value);
        public double tan(double value) => Math.Tan(value);
        public double tanh(double value) => Math.Tanh(value);
        public double truncate(double value) => Math.Truncate(value);

        public int intAdd(int lhs, int rhs) => lhs + rhs;
        public int intSub(int lhs, int rhs) => lhs - rhs;
        public int intMul(int lhs, int rhs) => lhs * rhs;
        public int intDiv(int lhs, int rhs) => lhs / rhs;

        public long longAdd(long lhs, long rhs) => lhs + rhs;
        public long longSub(long lhs, long rhs) => lhs - rhs;
        public long longMul(long lhs, long rhs) => lhs * rhs;
        public long longDiv(long lhs, long rhs) => lhs / rhs;

        public double doubleAdd(double lhs, double rhs) => lhs + rhs;
        public double doubleSub(double lhs, double rhs) => lhs - rhs;
        public double doubleMul(double lhs, double rhs) => lhs * rhs;
        public double doubleDiv(double lhs, double rhs) => lhs / rhs;

        public decimal decimalAdd(decimal lhs, decimal rhs) => lhs + rhs;
        public decimal decimalSub(decimal lhs, decimal rhs) => lhs - rhs;
        public decimal decimalMul(decimal lhs, decimal rhs) => lhs * rhs;
        public decimal decimalDiv(decimal lhs, decimal rhs) => lhs / rhs;
    }
}