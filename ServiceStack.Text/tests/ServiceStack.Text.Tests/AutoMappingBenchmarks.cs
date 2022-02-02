using NUnit.Framework;
using ServiceStack.Text;

namespace ServiceStack.Common.Tests
{
    public class BenchSource
    {
        public class Int1
        {
            public string str1 = "1";
            public string str2 = null;
            public int i = 10;
        }

        public class Int2
        {
            public Int1 i1 = new Int1();
            public Int1 i2 = new Int1();
            public Int1 i3 = new Int1();
            public Int1 i4 = new Int1();
            public Int1 i5 = new Int1();
            public Int1 i6 = new Int1();
            public Int1 i7 = new Int1();
        }

        public Int2 i1 = new Int2();
        public Int2 i2 = new Int2();
        public Int2 i3 = new Int2();
        public Int2 i4 = new Int2();
        public Int2 i5 = new Int2();
        public Int2 i6 = new Int2();
        public Int2 i7 = new Int2();
        public Int2 i8 = new Int2();

        public int n2;
        public long n3;
        public byte n4;
        public short n5;
        public uint n6;
        public int n7;
        public int n8;
        public int n9;

        public string s1 = "1";
        public string s2 = "2";
        public string s3 = "3";
        public string s4 = "4";
        public string s5 = "5";
        public string s6 = "6";
        public string s7 = "7";

    }

    public class BenchDestination
    {
        public class Int1
        {
            public string str1;
            public string str2;
            public int i;
        }

        public class Int2
        {
            public Int1 i1;
            public Int1 i2;
            public Int1 i3;
            public Int1 i4;
            public Int1 i5;
            public Int1 i6;
            public Int1 i7;
        }

        public Int2 i1 { get; set; }
        public Int2 i2 { get; set; }
        public Int2 i3 { get; set; }
        public Int2 i4 { get; set; }
        public Int2 i5 { get; set; }
        public Int2 i6 { get; set; }
        public Int2 i7 { get; set; }
        public Int2 i8 { get; set; }

        public long n2 = 2;
        public long n3 = 3;
        public long n4 = 4;
        public long n5 = 5;
        public long n6 = 6;
        public long n7 = 7;
        public long n8 = 8;
        public long n9 = 9;

        public string s1;
        public string s2;
        public string s3;
        public string s4;
        public string s5;
        public string s6;
        public string s7;
    }

    public class HandwrittenMapper
    {
        public static BenchDestination ToBenchDestination(BenchSource from)
        {
            return new BenchDestination
            {
                i1 = ToInt2(from.i1),
                i2 = ToInt2(from.i2),
                i3 = ToInt2(from.i3),
                i4 = ToInt2(from.i4),
                i5 = ToInt2(from.i5),
                i6 = ToInt2(from.i6),
                i7 = ToInt2(from.i7),
                i8 = ToInt2(from.i8),

                n2 = from.n2,
                n3 = from.n3,
                n4 = from.n4,
                n5 = from.n5,
                n6 = from.n6,
                n7 = from.n7,
                n8 = from.n8,
                n9 = from.n9,

                s1 = from.s1,
                s2 = from.s2,
                s3 = from.s3,
                s4 = from.s4,
                s5 = from.s5,
                s6 = from.s6,
                s7 = from.s7,
            };
        }

        public static BenchDestination.Int2 ToInt2(BenchSource.Int2 from)
        {
            return new BenchDestination.Int2
            {
                i1 = ToInt1(from.i1),
                i2 = ToInt1(from.i2),
                i3 = ToInt1(from.i3),
                i4 = ToInt1(from.i4),
                i5 = ToInt1(from.i5),
                i6 = ToInt1(from.i6),
                i7 = ToInt1(from.i7),
            };
        }

        public static BenchDestination.Int1 ToInt1(BenchSource.Int1 from)
        {
            return new BenchDestination.Int1 { i = from.i, str1 = from.str1, str2 = from.str2 };
        }
    }

    [Ignore("Perf test"), TestFixture]
    public class AutoMappingPerfTests : PerfTestBase
    {
        [Test]
        public void Compare_handwritten_vs_AutoMapping()
        {
            CompareMultipleRuns(
                "Handwritten",
                () => HandwrittenMapper.ToBenchDestination(new BenchSource()),
                "Auto Mapping",
                () => new BenchSource().ConvertTo<BenchDestination>());
        }

        [Test]
        public void Does_Convert_BenchSource()
        {
            var from = new BenchSource();
            var to = from.ConvertTo<BenchDestination>(); //warmup
            to = from.ConvertTo<BenchDestination>();

            using (JsConfig.With(new Config { IncludePublicFields = true }))
            {
                to.PrintDump();
                from.PrintDump();
            }
        }
    }
}

