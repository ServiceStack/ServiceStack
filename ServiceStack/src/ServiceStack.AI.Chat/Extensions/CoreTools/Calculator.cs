using System.Globalization;

namespace ServiceStack.AI;

/// <summary>
/// Safe math expression evaluator (port of core_tools' AST-based calc): numbers, arithmetic,
/// comparisons, and/or/not, and a whitelist of math functions and constants. No code execution.
/// </summary>
public static class Calculator
{
    public static readonly string[] Constants = ["pi", "e", "inf", "tau", "nan"];

    static readonly Dictionary<string, double> ConstantValues = new(StringComparer.Ordinal)
    {
        ["pi"] = Math.PI, ["e"] = Math.E, ["tau"] = Math.Tau,
        ["inf"] = double.PositiveInfinity, ["nan"] = double.NaN,
    };

    static readonly Dictionary<string, Func<double[], double>> Functions = new(StringComparer.Ordinal)
    {
        ["abs"] = a => Math.Abs(a[0]),
        ["min"] = a => a.Min(),
        ["max"] = a => a.Max(),
        ["sum"] = a => a.Sum(),
        ["round"] = a => a.Length > 1 ? Math.Round(a[0], (int)a[1]) : Math.Round(a[0]),
        ["mod"] = a => a[0] % a[1],
        ["mean"] = a => a.Average(),
        ["median"] = Median,
        ["stdev"] = a => Stdev(a, sample: true),
        ["variance"] = a => Variance(a, sample: true),
        ["sqrt"] = a => Math.Sqrt(a[0]),
        ["cbrt"] = a => Math.Cbrt(a[0]),
        ["exp"] = a => Math.Exp(a[0]),
        ["log"] = a => a.Length > 1 ? Math.Log(a[0], a[1]) : Math.Log(a[0]),
        ["log2"] = a => Math.Log2(a[0]),
        ["log10"] = a => Math.Log10(a[0]),
        ["pow"] = a => Math.Pow(a[0], a[1]),
        ["sin"] = a => Math.Sin(a[0]),
        ["cos"] = a => Math.Cos(a[0]),
        ["tan"] = a => Math.Tan(a[0]),
        ["asin"] = a => Math.Asin(a[0]),
        ["acos"] = a => Math.Acos(a[0]),
        ["atan"] = a => Math.Atan(a[0]),
        ["atan2"] = a => Math.Atan2(a[0], a[1]),
        ["sinh"] = a => Math.Sinh(a[0]),
        ["cosh"] = a => Math.Cosh(a[0]),
        ["tanh"] = a => Math.Tanh(a[0]),
        ["ceil"] = a => Math.Ceiling(a[0]),
        ["floor"] = a => Math.Floor(a[0]),
        ["trunc"] = a => Math.Truncate(a[0]),
        ["degrees"] = a => a[0] * 180 / Math.PI,
        ["radians"] = a => a[0] * Math.PI / 180,
        ["hypot"] = a => a.Length > 1 ? Math.Sqrt(a[0] * a[0] + a[1] * a[1]) : Math.Abs(a[0]),
        ["fabs"] = a => Math.Abs(a[0]),
        ["factorial"] = Factorial,
        ["gcd"] = a => Gcd((long)a[0], (long)a[1]),
        ["fmod"] = a => Math.IEEERemainder(a[0], a[1]) is var r && Math.Sign(r) != Math.Sign(a[1]) && r != 0 ? r + a[1] : r,
        ["copysign"] = a => Math.CopySign(a[0], a[1]),
        ["isqrt"] = a => Math.Floor(Math.Sqrt(a[0])),
    };

    public static List<string> FunctionNames => Functions.Keys.OrderBy(x => x, StringComparer.Ordinal).ToList();

    /// <summary>Evaluate the expression, returning Python-style repr ("True"/"False" for bools)</summary>
    public static string Evaluate(string expression)
    {
        var parser = new Parser(expression);
        var result = parser.ParseExpression();
        parser.ExpectEnd();
        return Format(result);
    }

    static string Format(object value) => value switch
    {
        bool b => b ? "True" : "False",
        double d when double.IsPositiveInfinity(d) => "inf",
        double d when double.IsNegativeInfinity(d) => "-inf",
        double d when double.IsNaN(d) => "nan",
        double d when d == Math.Floor(d) && Math.Abs(d) < 1e15 && !d.ToString(CultureInfo.InvariantCulture).Contains('E')
            => ((long)d).ToString(CultureInfo.InvariantCulture),
        double d => d.ToString("R", CultureInfo.InvariantCulture),
        _ => value.ToString() ?? "",
    };

    static double Median(double[] a)
    {
        var sorted = a.OrderBy(x => x).ToArray();
        var mid = sorted.Length / 2;
        return sorted.Length % 2 == 1 ? sorted[mid] : (sorted[mid - 1] + sorted[mid]) / 2;
    }

    static double Variance(double[] a, bool sample)
    {
        if (a.Length < 2) throw new ArgumentException("variance requires at least two values");
        var mean = a.Average();
        return a.Sum(x => (x - mean) * (x - mean)) / (a.Length - (sample ? 1 : 0));
    }

    static double Stdev(double[] a, bool sample) => Math.Sqrt(Variance(a, sample));

    static double Factorial(double[] a)
    {
        var n = (long)a[0];
        if (n < 0 || n != a[0]) throw new ArgumentException("factorial requires a non-negative integer");
        if (n > 170) return double.PositiveInfinity;
        double result = 1;
        for (var i = 2; i <= n; i++) result *= i;
        return result;
    }

    static long Gcd(long a, long b)
    {
        a = Math.Abs(a); b = Math.Abs(b);
        while (b != 0) (a, b) = (b, a % b);
        return a;
    }

    /// <summary>Recursive descent parser: or → and → not → comparison → add → mul → unary → power → atom</summary>
    class Parser(string input)
    {
        int pos;

        public object ParseExpression() => ParseOr();

        public void ExpectEnd()
        {
            SkipWhitespace();
            if (pos < input.Length)
                throw new ArgumentException($"Unexpected input at position {pos}: '{input[pos..]}'");
        }

        object ParseOr()
        {
            var left = ParseAnd();
            while (MatchWord("or"))
            {
                var right = ParseAnd();
                left = Truthy(left) || Truthy(right);
            }
            return left;
        }

        object ParseAnd()
        {
            var left = ParseNot();
            while (MatchWord("and"))
            {
                var right = ParseNot();
                left = Truthy(left) && Truthy(right);
            }
            return left;
        }

        object ParseNot() => MatchWord("not") ? !Truthy(ParseNot()) : ParseComparison();

        object ParseComparison()
        {
            var left = ParseAdditive();
            SkipWhitespace();
            foreach (var (op, fn) in ComparisonOps)
            {
                if (Match(op))
                {
                    var right = ParseAdditive();
                    return fn(ToNumber(left), ToNumber(right));
                }
            }
            return left;
        }

        static readonly (string Op, Func<double, double, bool> Fn)[] ComparisonOps =
        [
            ("==", (a, b) => a == b), ("!=", (a, b) => a != b),
            ("<=", (a, b) => a <= b), (">=", (a, b) => a >= b),
            ("<", (a, b) => a < b), (">", (a, b) => a > b),
        ];

        object ParseAdditive()
        {
            var left = ParseMultiplicative();
            while (true)
            {
                SkipWhitespace();
                if (Match("+")) left = ToNumber(left) + ToNumber(ParseMultiplicative());
                else if (Match("-")) left = ToNumber(left) - ToNumber(ParseMultiplicative());
                else return left;
            }
        }

        object ParseMultiplicative()
        {
            var left = ParseUnary();
            while (true)
            {
                SkipWhitespace();
                if (Match("*")) left = ToNumber(left) * ToNumber(ParseUnary());
                else if (Match("/")) left = ToNumber(left) / ToNumber(ParseUnary());
                else if (Match("%")) left = ToNumber(left) % ToNumber(ParseUnary());
                else return left;
            }
        }

        object ParseUnary()
        {
            SkipWhitespace();
            if (Match("-")) return -ToNumber(ParseUnary());
            if (Match("+")) return ToNumber(ParseUnary());
            return ParsePower();
        }

        object ParsePower()
        {
            var left = ParseAtom();
            SkipWhitespace();
            if (Match("**") || Match("^")) // accept both Python ** and calculator ^
            {
                var right = ParseUnary(); // right-associative
                return Math.Pow(ToNumber(left), ToNumber(right));
            }
            return left;
        }

        object ParseAtom()
        {
            SkipWhitespace();
            if (pos >= input.Length)
                throw new ArgumentException("Unexpected end of expression");

            if (Match("("))
            {
                var value = ParseOr();
                SkipWhitespace();
                if (!Match(")"))
                    throw new ArgumentException("Expected ')'");
                return value;
            }

            var c = input[pos];
            if (char.IsDigit(c) || c == '.')
                return ParseNumber();
            if (char.IsLetter(c) || c == '_')
                return ParseIdentifier();

            throw new ArgumentException($"Unexpected character '{c}' at position {pos}");
        }

        double ParseNumber()
        {
            var start = pos;
            while (pos < input.Length && (char.IsDigit(input[pos]) || input[pos] is '.' or 'e' or 'E'
                || (input[pos] is '+' or '-' && pos > start && input[pos - 1] is 'e' or 'E')))
            {
                pos++;
            }
            return double.Parse(input[start..pos], CultureInfo.InvariantCulture);
        }

        object ParseIdentifier()
        {
            var start = pos;
            while (pos < input.Length && (char.IsLetterOrDigit(input[pos]) || input[pos] == '_'))
                pos++;
            var name = input[start..pos];

            if (name is "True" or "true") return true;
            if (name is "False" or "false") return false;
            if (ConstantValues.TryGetValue(name, out var constant))
                return constant;

            SkipWhitespace();
            if (!Match("("))
                throw new ArgumentException($"Unknown identifier '{name}'");

            var args = new List<double>();
            SkipWhitespace();
            if (!Match(")"))
            {
                do
                {
                    args.Add(ToNumber(ParseOr()));
                    SkipWhitespace();
                } while (Match(","));
                if (!Match(")"))
                    throw new ArgumentException("Expected ')'");
            }

            if (!Functions.TryGetValue(name, out var fn))
                throw new ArgumentException($"Unknown function '{name}'");
            return fn(args.ToArray());
        }

        static double ToNumber(object value) => value switch
        {
            double d => d,
            bool b => b ? 1 : 0,
            _ => throw new ArgumentException($"Expected a number, got {value}"),
        };

        static bool Truthy(object value) => value switch
        {
            bool b => b,
            double d => d != 0,
            _ => false,
        };

        void SkipWhitespace()
        {
            while (pos < input.Length && char.IsWhiteSpace(input[pos]))
                pos++;
        }

        bool Peek(string token) =>
            pos + token.Length <= input.Length && input.AsSpan(pos, token.Length).SequenceEqual(token);

        bool Match(string token)
        {
            SkipWhitespace();
            if (!Peek(token))
                return false;
            // don't confuse '*' with '**', or comparison '<' with '<='
            if (token is "*" && Peek("**")) return false;
            if (token is "<" && Peek("<=")) return false;
            if (token is ">" && Peek(">=")) return false;
            pos += token.Length;
            return true;
        }

        bool MatchWord(string word)
        {
            SkipWhitespace();
            if (!Peek(word))
                return false;
            var end = pos + word.Length;
            if (end < input.Length && (char.IsLetterOrDigit(input[end]) || input[end] == '_'))
                return false;
            pos = end;
            return true;
        }
    }
}
