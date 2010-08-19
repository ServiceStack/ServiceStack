using System;
using System.Linq;
using System.Text;
using NUnit.Framework;
using ServiceStack.Common.Extensions;
using ServiceStack.Common.Tests;

namespace ServiceStack.Text.Tests.Support
{
	[Ignore]
	[TestFixture]
	public class BenchmarkTests
		: PerfTestBase
	{
		[Test]
		public void Test_string_parsing()
		{
			const int stringSampleSize = 1024 * 10;
			var testString = CreateRandomString(stringSampleSize);
			var copyTo = new char[stringSampleSize];

			CompareMultipleRuns(
				"As char array",
				() =>
				{
					var asChars = testString.ToCharArray();
					for (var i = 0; i < stringSampleSize; i++)
					{
						copyTo[i] = asChars[i];
					}
				},
				"As string",
				() =>
				{
					for (var i = 0; i < stringSampleSize; i++)
					{
						copyTo[i] = testString[i];
					}
				});
		}

		public string CreateRandomString(int size)
		{
			var randString = new char[size];
			for (var i = 0; i < size; i++)
			{
				randString[i] = (char)((i % 10) + '0');
			}
			return new string(randString);
		}


		static readonly char[] EscapeChars = new char[]
		{
			'"', '\n', '\r', '\t', '"', '\\', '\f', '\b',
		};
		public static readonly char[] JsvEscapeChars = new[]
    	{
    		'"', ',', '{', '}', '[', ']',
    	};

		private const int LengthFromLargestChar = '\\' + 1;
		private static readonly bool[] HasEscapeChars = new bool[LengthFromLargestChar];

		[Test]
		public void PrintEscapeChars()
		{
			//EscapeChars.ToList().OrderBy(x => (int)x).ForEach(x => Console.WriteLine(x + ": " + (int)x));
			JsvEscapeChars.ToList().OrderBy(x => (int)x).ForEach(x => Console.WriteLine(x + ": " + (int)x));
		}

		[Test]
		public void MeasureIndexOfEscapeChars()
		{
			foreach (var escapeChar in EscapeChars)
			{
				HasEscapeChars[escapeChar] = true;
			}

			var value = CreateRandomString(100);
			var len = value.Length;
			var hasEscapeChars = false;

			CompareMultipleRuns(
				"With bool flags",
				() =>
				{
					for (var i = 0; i < len; i++)
					{
						var c = value[i];
						if (c >= LengthFromLargestChar || !HasEscapeChars[c]) continue;
						hasEscapeChars = true;
						break;
					}
				},
				"With IndexOfAny",
				() =>
				{
					hasEscapeChars = value.IndexOfAny(EscapeChars) != -1;
				});

			Console.WriteLine(hasEscapeChars);
		}

		public class RuntimeType<T>
		{
			private static Type type = typeof (T);

			internal static bool TestVarType()
			{
				return type == typeof(byte) || type == typeof(byte?)
						|| type == typeof(short) || type == typeof(short?)
						|| type == typeof(ushort) || type == typeof(ushort?)
						|| type == typeof(int) || type == typeof(int?)
						|| type == typeof(uint) || type == typeof(uint?)
						|| type == typeof(long) || type == typeof(long?)
						|| type == typeof(ulong) || type == typeof(ulong?)
						|| type == typeof(bool) || type == typeof(bool?)
						|| type != typeof(DateTime)
						|| type != typeof(DateTime?)
						|| type != typeof(Guid)
						|| type != typeof(Guid?)
						|| type != typeof(float) || type != typeof(float?)
						|| type != typeof(double) || type != typeof(double?)
						|| type != typeof(decimal) || type != typeof(decimal?);
			}

			internal static bool TestGenericType()
			{
				return typeof(T) == typeof(byte) || typeof(T) == typeof(byte?)
						|| typeof(T) == typeof(short) || typeof(T) == typeof(short?)
						|| typeof(T) == typeof(ushort) || typeof(T) == typeof(ushort?)
						|| typeof(T) == typeof(int) || typeof(T) == typeof(int?)
						|| typeof(T) == typeof(uint) || typeof(T) == typeof(uint?)
						|| typeof(T) == typeof(long) || typeof(T) == typeof(long?)
						|| typeof(T) == typeof(ulong) || typeof(T) == typeof(ulong?)
						|| typeof(T) == typeof(bool) || typeof(T) == typeof(bool?)
						|| typeof(T) != typeof(DateTime)
						|| typeof(T) != typeof(DateTime?)
						|| typeof(T) != typeof(Guid)
						|| typeof(T) != typeof(Guid?)
						|| typeof(T) != typeof(float) || typeof(T) != typeof(float?)
						|| typeof(T) != typeof(double) || typeof(T) != typeof(double?)
						|| typeof(T) != typeof(decimal) || typeof(T) != typeof(decimal?);
			}
		}


		[Test]
		public void TestVarOrGenericType()
		{
			var matchingTypesCount = 0;

			CompareMultipleRuns(
				"With var type",
				() =>
				{
					if (RuntimeType<BenchmarkTests>.TestVarType())
					{
						matchingTypesCount++;
					}
				},
				"With generic type",
				() =>
				{
					if (RuntimeType<BenchmarkTests>.TestGenericType())
					{
						matchingTypesCount++;
					}
				});

			Console.WriteLine(matchingTypesCount);
		}

		[Test]
		public void Test_for_numeric_type()
		{
			var value1 = Guid.NewGuid();
			var value2 = "1.2345";
			var results = 0;

			CompareMultipleRuns(
				"With Type Checking",
				() =>
				{
					if (value1.GetType().IsNumericType())
					{
						results++;
					}
				},
				"With Double Parsing",
				() =>
				{
					int d;
					if (int.TryParse(value1.ToString(), out d))
					{
						results++;
					}
				});

			Console.WriteLine(results);
		}

	}
}