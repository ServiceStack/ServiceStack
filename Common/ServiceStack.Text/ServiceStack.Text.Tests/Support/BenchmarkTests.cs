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


	}
}