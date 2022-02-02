using System;
using System.Collections.Generic;
using NUnit.Framework;
using ServiceStack.Common;
using ServiceStack.Text;

namespace ServiceStack.Common.Tests.Perf
{
    [Ignore("Benchmarks for deserializing basic .NET types")]
    [TestFixture]
    public class StringParsePerf
        : PerfTestBase
    {
        public StringParsePerf()
        {
            this.MultipleIterations = new List<int> { 10000 };
        }

        public List<string> CreateList(Func<int, string> createStringFn, int noOfTimes)
        {
            var list = new List<string>();
            for (var i = 0; i < noOfTimes; i++)
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
                "SCU.Parse<int>", () => TypeSerializer.DeserializeFromString<int>("1")
            );
        }

        [Test]
        public void Compare_longs()
        {
            CompareMultipleRuns(
                "long.Parse", () => long.Parse("1"),
                "SCU.Parse<long>", () => TypeSerializer.DeserializeFromString<long>("1")
            );
        }

        [Test]
        public void Compare_Guids()
        {
            CompareMultipleRuns(
                "new Guid", () => new Guid("AC800C9C-B8BE-4829-868A-B43CFF7B2AFD"),
                "SCU.Parse<Guid>", () => TypeSerializer.DeserializeFromString<Guid>("AC800C9C-B8BE-4829-868A-B43CFF7B2AFD")
            );
        }

        [Test]
        public void Compare_DateTime()
        {
            const string dateTimeStr = "2009-12-20T19:24:37.4379982Z";
            CompareMultipleRuns(
                "DateTime.Parse", () => DateTime.Parse(dateTimeStr),
                "SCU.Parse<DateTime>", () => TypeSerializer.DeserializeFromString<DateTime>(dateTimeStr)
            );
        }

        private static string[] SplitList(string listStr)
        {
            return listStr.Substring(1, listStr.Length - 2).Split(',');
        }

        [Test]
        public void Compare_IntList()
        {
            const string intValues = "[0,1,2,3,4,5,6,7,8,9]";
            CompareMultipleRuns(
                "intValues.Split(',').ConvertAll", () => SplitList(intValues).Map(int.Parse),
                "SCU.Parse<List<int>>", () => TypeSerializer.DeserializeFromString<List<int>>(intValues)
            );
        }

        [Test]
        public void Compare_LongList()
        {
            const string longValues = "[0,1,2,3,4,5,6,7,8,9]";
            CompareMultipleRuns(
                "intValues.Split(',').ConvertAll", () => SplitList(longValues).Map(long.Parse),
                "SCU.Parse<List<long>>", () => TypeSerializer.DeserializeFromString<List<long>>(longValues)
            );
        }

        [Test]
        public void Compare_StringArray()
        {
            const string stringValues = "[a,b,c,d,e,f,g,h,i,j]";
            CompareMultipleRuns(
                "TextExtensions.FromCsvFields", () => TextExtensions.FromCsvFields(stringValues.Split(',')),
                "SCU.Parse<string[]>", () => TypeSerializer.DeserializeFromString<string[]>(stringValues)
            );
        }

        [Test]
        public void Compare_DoubleArray()
        {
            const string stringValues = "[1.1,2.2,3.3,4.4,5.5,6.6,7.7,8.8,9.9,0.1]";
            CompareMultipleRuns(
                ".Split(',').ConvertAll(x => double.Parse(x))", () => SplitList(stringValues).Map(double.Parse),
                "SCU.Parse<double[]>", () => TypeSerializer.DeserializeFromString<double[]>(stringValues)
            );
        }

        [Test]
        public void Compare_GuidArray()
        {
            const string stringValues = "[8F403A5E-CDFC-4C6F-B0EB-C055C1C8BA60,5673BAC7-BAC5-4B3F-9B69-4180E6227508,B0CA730F-14C9-4D00-AC7F-07E7DE8D566E,4E26AF94-6B13-4F89-B192-36C6ABE73DAE,08491B16-2270-4DF9-8AEE-A8861A791C50]";
            CompareMultipleRuns(
                ".Split(',').ConvertAll(x => new Guid(x))", () => SplitList(stringValues).Map(x => new Guid(x)),
                "SCU.Parse<Guid[]>", () => TypeSerializer.DeserializeFromString<Guid[]>(stringValues)
            );
        }

        [Test]
        public void Compare_StringList()
        {
            const string stringValues = "[a,b,c,d,e,f,g,h,i,j]";
            CompareMultipleRuns(
                "stringValues.Split(',').FromCsvFields()", () => SplitList(stringValues).FromCsvFields(),
                "SCU.Parse<List<string>>", () => TypeSerializer.DeserializeFromString<List<string>>(stringValues)
            );
        }

        [Test]
        public void Compare_DoubleList()
        {
            const string stringValues = "[1.1,2.2,3.3,4.4,5.5,6.6,7.7,8.8,9.9,0.1]";
            CompareMultipleRuns(
                ".Split(',').ConvertAll(x => double.Parse(x))", () => SplitList(stringValues).Map(double.Parse),
                "SCU.Parse<List<double>>", () => TypeSerializer.DeserializeFromString<List<double>>(stringValues)
            );
        }

        [Test]
        public void Compare_GuidList()
        {
            const string stringValues = "[8F403A5E-CDFC-4C6F-B0EB-C055C1C8BA60,5673BAC7-BAC5-4B3F-9B69-4180E6227508,B0CA730F-14C9-4D00-AC7F-07E7DE8D566E,4E26AF94-6B13-4F89-B192-36C6ABE73DAE,08491B16-2270-4DF9-8AEE-A8861A791C50]";
            CompareMultipleRuns(
                ".Split(',').ConvertAll(x => new Guid(x))", () => SplitList(stringValues).Map(x => new Guid(x)),
                "SCU.Parse<List<Guid>>", () => TypeSerializer.DeserializeFromString<List<Guid>>(stringValues)
            );
        }

        [Test]
        public void Compare_StringHashSet()
        {
            const string stringValues = "[a,b,c,d,e,f,g,h,i,j]";
            CompareMultipleRuns(
                "new HashSet<string>(.Split(',').FromCsvFields())", () => new HashSet<string>(SplitList(stringValues).FromCsvFields()),
                "SCU.Parse<HashSet<string>>", () => TypeSerializer.DeserializeFromString<HashSet<string>>(stringValues)
            );
        }

        [Test]
        public void Compare_IntHashSet()
        {
            const string stringValues = "[0,1,2,3,4,5,6,7,8,9]";
            CompareMultipleRuns(
                "new HashSet<int>(.Split(',').ConvertAll(x => int.Parse(x))", () => new HashSet<int>(SplitList(stringValues).Map(int.Parse)),
                "SCU.Parse<HashSet<int>>", () => TypeSerializer.DeserializeFromString<HashSet<int>>(stringValues)
            );
        }

        [Test]
        public void Compare_DoubleHashSet()
        {
            const string stringValues = "[1.1,2.2,3.3,4.4,5.5,6.6,7.7,8.8,9.9,0.1]";
            CompareMultipleRuns(
                "new HashSet<double>(.ConvertAll(x => double.Parse(x)))", () => new HashSet<double>(SplitList(stringValues).Map(double.Parse)),
                "SCU.Parse<HashSet<double>>", () => TypeSerializer.DeserializeFromString<HashSet<double>>(stringValues)
            );
        }

        [Test]
        public void Compare_StringStringMap()
        {
            const string mapValues = "{A:1,B:2,C:3,D:4,E:5,F:6,G:7,H:8,I:9,J:0}";
            var map = new Dictionary<string, string>();
            CompareMultipleRuns(
                "mapValues.Split(',').ConvertAll", () => SplitList(mapValues).Map(x => x.Split(':')).ForEach(y => map[y[0].FromCsvField()] = y[1].FromCsvField()),
                "SCU.Parse<Dictionary<string, string>>", () => TypeSerializer.DeserializeFromString<Dictionary<string, string>>(mapValues)
            );
        }

        [Test]
        public void Compare_StringIntMap()
        {
            const string mapValues = "{A:1,B:2,C:3,D:4,E:5,F:6,G:7,H:8,I:9,J:0}";
            var map = new Dictionary<string, int>();
            CompareMultipleRuns(
                "mapValues.Split(',').ConvertAll", () => SplitList(mapValues).Map(x => x.Split(':')).ForEach(y => map[y[0].FromCsvField()] = int.Parse(y[1])),
                "SCU.Parse<Dictionary<string, int>>", () => TypeSerializer.DeserializeFromString<Dictionary<string, int>>(mapValues)
            );
        }

        [Test]
        public void Compare_StringInt_SortedDictionary()
        {
            const string mapValues = "{A:1,B:2,C:3,D:4,E:5,F:6,G:7,H:8,I:9,J:0}";
            var map = new SortedDictionary<string, int>();
            CompareMultipleRuns(
                "mapValues.Split(',').ConvertAll", () => SplitList(mapValues).Map(x => x.Split(':')).ForEach(y => map[y[0].FromCsvField()] = int.Parse(y[1])),
                "SCU.Parse<Dictionary<string, int>>", () => TypeSerializer.DeserializeFromString<SortedDictionary<string, int>>(mapValues)
            );
        }

        [Test]
        public void Compare_ByteArray()
        {
            var byteArrayValue = new byte[] { 0, 65, 97, 255, 0, 65, 97, 255, 0, 65, 97, 255, 0, 65, 97, 255, 0, 65, 97, 255, 0, 65, 97, 255, 0, 65, 97, 255, 0, 65, 97, 255, };
            var byteArrayString = Convert.ToBase64String(byteArrayValue);

            CompareMultipleRuns(
                "Encoding.Default.GetBytes", () => System.Text.Encoding.GetEncoding(0).GetBytes(byteArrayString),
                "SCU.Parse<byte[]>", () => TypeSerializer.DeserializeFromString<byte[]>(byteArrayString)
            );
        }
    }
}