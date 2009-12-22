using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using NUnit.Framework;
using ServiceStack.Common.Extensions;
using ServiceStack.Common.Utils;
using ServiceStack.OrmLite.Tests.Models;

namespace ServiceStack.Common.Tests.Perf
{
	[TestFixture]
	public class StringConverterUtilsPerf
		: PerfTestBase
	{
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
		public void Compare_StringStringMap()
		{
			const string mapValues = "A:1,B:2,C:3,D:4,E:5,F:6,G:7,H:8,I:9,J:0";
			var map = new Dictionary<string, string>();
			CompareMultipleRuns(
				"mapValues.Split(',').ConvertAll", () => mapValues.Split(',').ConvertAll(x => x.Split(':')).ForEach(y => map[y[0]] = y[1]),
				"SCU.Parse<Dictionary<string,int>>", () => StringConverterUtils.Parse<Dictionary<string, string>>(mapValues)
			);
		}

		[Test]
		public void Compare_StringIntMap()
		{
			const string mapValues = "A:1,B:2,C:3,D:4,E:5,F:6,G:7,H:8,I:9,J:0";
			var map = new Dictionary<string, int>();
			CompareMultipleRuns(
				"mapValues.Split(',').ConvertAll", () => mapValues.Split(',').ConvertAll(x => x.Split(':')).ForEach(y => map[y[0]] = int.Parse(y[1])),
				"SCU.Parse<Dictionary<string,int>>", () => StringConverterUtils.Parse<Dictionary<string, int>>(mapValues)
			);
		}
	}
}