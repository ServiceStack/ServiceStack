using System;
using System.Collections.Generic;
using NUnit.Framework;
using ServiceStack.Common.Extensions;
using ServiceStack.Common.Utils;

namespace ServiceStack.Common.Tests.Perf
{
	[Ignore]
	[TestFixture]
	public class StringParsePerf
		: PerfTestBase
	{
		public StringParsePerf()
		{
			this.MultipleIterations = new List<int> { 1000000 };
		}

		public List<string> CreateList(Func<int, string> createStringFn, int noOfTimes)
		{
			var list = new List<string>();
			for (var i=0; i < noOfTimes; i++)
			{
				list.Add(createStringFn(i));
			}
			return list;
		}

		[Test]
		public void Compare_ints()
		{
			CompareMultipleRuns(
				"int.Parse", () => int.Parse("1"),
				"SCU.Parse<int>", () => StringConverterUtils.Parse<int>("1")
			);
		}

		[Test]
		public void Compare_longs()
		{
			CompareMultipleRuns(
				"long.Parse", () => long.Parse("1"),
				"SCU.Parse<long>", () => StringConverterUtils.Parse<long>("1")
			);
		}

		[Test]
		public void Compare_Guids()
		{
			CompareMultipleRuns(
				"new Guid", () => new Guid("AC800C9C-B8BE-4829-868A-B43CFF7B2AFD"),
				"SCU.Parse<Guid>", () => StringConverterUtils.Parse<Guid>("AC800C9C-B8BE-4829-868A-B43CFF7B2AFD")
			);
		}

		[Test]
		public void Compare_DateTime()
		{
			CompareMultipleRuns(
				"DateTime.Parse", () => DateTime.Parse("20/12/2009 19:24:37"),
				"SCU.Parse<DateTime>", () => StringConverterUtils.Parse<DateTime>("20/12/2009 19:24:37")
			);
		}

		[Test]
		public void Compare_IntList()
		{
			const string intValues = "0,1,2,3,4,5,6,7,8,9";
			CompareMultipleRuns(
				"intValues.Split(',').ConvertAll", () => intValues.Split(',').ConvertAll(x => int.Parse(x)),
				"SCU.Parse<List<int>>", () => StringConverterUtils.Parse<List<int>>(intValues)
			);
		}

		[Test]
		public void Compare_LongList()
		{
			const string longValues = "0,1,2,3,4,5,6,7,8,9";
			CompareMultipleRuns(
				"intValues.Split(',').ConvertAll", () => longValues.Split(',').ConvertAll(x => long.Parse(x)),
				"SCU.Parse<List<long>>", () => StringConverterUtils.Parse<List<long>>(longValues)
			);
		}

		[Test]
		public void Compare_StringArray()
		{
			const string stringValues = "a,b,c,d,e,f,g,h,i,j";
			CompareMultipleRuns(
				"TextExtensions.FromSafeStrings", () => TextExtensions.FromSafeStrings(stringValues.Split(',')),
				"SCU.Parse<string[]>", () => StringConverterUtils.Parse<string[]>(stringValues)
			);
		}

		[Test]
		public void Compare_DoubleArray()
		{
			const string stringValues = "1.1,2.2,3.3,4.4,5.5,6.6,7.7,8.8,9.9,0.1";
			CompareMultipleRuns(
				".Split(',').ConvertAll(x => double.Parse(x))", () => stringValues.Split(',').ConvertAll(x => double.Parse(x)),
				"SCU.Parse<double[]>", () => StringConverterUtils.Parse<double[]>(stringValues)
			);
		}

		[Test]
		public void Compare_GuidArray()
		{
			const string stringValues = "8F403A5E-CDFC-4C6F-B0EB-C055C1C8BA60,5673BAC7-BAC5-4B3F-9B69-4180E6227508,B0CA730F-14C9-4D00-AC7F-07E7DE8D566E,4E26AF94-6B13-4F89-B192-36C6ABE73DAE,08491B16-2270-4DF9-8AEE-A8861A791C50";
			CompareMultipleRuns(
				".Split(',').ConvertAll(x => new Guid(x))", () => stringValues.Split(',').ConvertAll(x => new Guid(x)),
				"SCU.Parse<Guid[]>", () => StringConverterUtils.Parse<Guid[]>(stringValues)
			);
		}

		[Test]
		public void Compare_StringList()
		{
			const string stringValues = "a,b,c,d,e,f,g,h,i,j";
			CompareMultipleRuns(
				"stringValues.Split(',').FromSafeStrings()", () => stringValues.Split(',').FromSafeStrings(),
				"SCU.Parse<List<string>>", () => StringConverterUtils.Parse<List<string>>(stringValues)
			);
		}

		[Test]
		public void Compare_DoubleList()
		{
			const string stringValues = "1.1,2.2,3.3,4.4,5.5,6.6,7.7,8.8,9.9,0.1";
			CompareMultipleRuns(
				".Split(',').ConvertAll(x => double.Parse(x))", () => stringValues.Split(',').ConvertAll(x => double.Parse(x)),
				"List<double>", () => StringConverterUtils.Parse<List<double>>(stringValues)
			);
		}

		[Test]
		public void Compare_GuidList()
		{
			const string stringValues = "8F403A5E-CDFC-4C6F-B0EB-C055C1C8BA60,5673BAC7-BAC5-4B3F-9B69-4180E6227508,B0CA730F-14C9-4D00-AC7F-07E7DE8D566E,4E26AF94-6B13-4F89-B192-36C6ABE73DAE,08491B16-2270-4DF9-8AEE-A8861A791C50";
			CompareMultipleRuns(
				".Split(',').ConvertAll(x => new Guid(x))", () => stringValues.Split(',').ConvertAll(x => new Guid(x)),
				"SCU.Parse<List<Guid>>", () => StringConverterUtils.Parse<List<Guid>>(stringValues)
			);
		}

		[Test]
		public void Compare_StringHashSet()
		{
			const string stringValues = "a,b,c,d,e,f,g,h,i,j";
			CompareMultipleRuns(
				"new HashSet<string>(.Split(',').FromSafeStrings())", () => new HashSet<string>(stringValues.Split(',').FromSafeStrings()),
				"SCU.Parse<HashSet<string>>", () => StringConverterUtils.Parse<HashSet<string>>(stringValues)
			);
		}

		[Test]
		public void Compare_IntHashSet()
		{
			const string stringValues = "0,1,2,3,4,5,6,7,8,9";
			CompareMultipleRuns(
				"new HashSet<int>(.Split(',').ConvertAll(x => int.Parse(x))", () => new HashSet<int>(stringValues.Split(',').ConvertAll(x => int.Parse(x))),
				"SCU.Parse<HashSet<int>>", () => StringConverterUtils.Parse<HashSet<int>>(stringValues)
			);
		}

		[Test]
		public void Compare_DoubleHashSet()
		{
			const string stringValues = "1.1,2.2,3.3,4.4,5.5,6.6,7.7,8.8,9.9,0.1";
			CompareMultipleRuns(
				"new HashSet<double>(.ConvertAll(x => double.Parse(x)))", () => new HashSet<double>(stringValues.Split(',').ConvertAll(x => double.Parse(x))),
				"SCU.Parse<HashSet<double>>", () => StringConverterUtils.Parse<HashSet<double>>(stringValues)
			);
		}

		[Test]
		public void Compare_StringStringMap()
		{
			const string mapValues = "A:1,B:2,C:3,D:4,E:5,F:6,G:7,H:8,I:9,J:0";
			var map = new Dictionary<string, string>();
			CompareMultipleRuns(
				"mapValues.Split(',').ConvertAll", () => mapValues.Split(',').ConvertAll(x => x.Split(':')).ForEach(y => map[y[0].FromSafeString()] = y[1].FromSafeString()),
				"SCU.Parse<Dictionary<string, string>>", () => StringConverterUtils.Parse<Dictionary<string, string>>(mapValues)
			);
		}

		[Test]
		public void Compare_StringIntMap()
		{
			const string mapValues = "A:1,B:2,C:3,D:4,E:5,F:6,G:7,H:8,I:9,J:0";
			var map = new Dictionary<string, int>();
			CompareMultipleRuns(
				"mapValues.Split(',').ConvertAll", () => mapValues.Split(',').ConvertAll(x => x.Split(':')).ForEach(y => map[y[0].FromSafeString()] = int.Parse(y[1])),
				"SCU.Parse<Dictionary<string, int>>", () => StringConverterUtils.Parse<Dictionary<string, int>>(mapValues)
			);
		}

		[Test]
		public void Compare_StringInt_SortedDictionary()
		{
			const string mapValues = "A:1,B:2,C:3,D:4,E:5,F:6,G:7,H:8,I:9,J:0";
			var map = new SortedDictionary<string, int>();
			CompareMultipleRuns(
				"mapValues.Split(',').ConvertAll", () => mapValues.Split(',').ConvertAll(x => x.Split(':')).ForEach(y => map[y[0].FromSafeString()] = int.Parse(y[1])),
				"SCU.Parse<Dictionary<string, int>>", () => StringConverterUtils.Parse<SortedDictionary<string, int>>(mapValues)
			);
		}

		[Test]
		public void Compare_ByteArray()
		{
			var byteArrayValue = new byte[] { 0, 65, 97, 255, 0, 65, 97, 255, 0, 65, 97, 255, 0, 65, 97, 255, 0, 65, 97, 255, 0, 65, 97, 255, 0, 65, 97, 255, 0, 65, 97, 255, };
			var byteArrayString = System.Text.Encoding.Default.GetString(byteArrayValue);

			CompareMultipleRuns(
				"Encoding.Default.GetBytes", () => System.Text.Encoding.Default.GetBytes(byteArrayString),
				"SCU.Parse<byte[]>", () => StringConverterUtils.Parse<byte[]>(byteArrayString)
			);
		}
	}
}