using System;

namespace ServiceStack.Text.Tests.Benchmarks
{
    public class ModelWithCommonTypes
    {
        public char CharValue { get; set; }

        public byte ByteValue { get; set; }

        public sbyte SByteValue { get; set; }

        public short ShortValue { get; set; }

        public ushort UShortValue { get; set; }

        public int IntValue { get; set; }

        public uint UIntValue { get; set; }

        public long LongValue { get; set; }

        public ulong ULongValue { get; set; }

        public float FloatValue { get; set; }

        public double DoubleValue { get; set; }

        public decimal DecimalValue { get; set; }

        public DateTime DateTimeValue { get; set; }

        public TimeSpan TimeSpanValue { get; set; }

        public Guid GuidValue { get; set; }

        public static ModelWithCommonTypes Create(byte i)
        {
            return new ModelWithCommonTypes
            {
                ByteValue = i,
                CharValue = (char)i,
                DateTimeValue = new DateTime(2000, 1, 1 + i),
                DecimalValue = i,
                DoubleValue = i,
                FloatValue = i,
                IntValue = i,
                LongValue = i,
                SByteValue = (sbyte)i,
                ShortValue = i,
                TimeSpanValue = new TimeSpan(i),
                UIntValue = i,
                ULongValue = i,
                UShortValue = i,
                GuidValue = Guid.NewGuid(),
            };
        }
    }

    public class StringType
    {
        public string Value1 { get; set; }
        public string Value2 { get; set; }
        public string Value3 { get; set; }
        public string Value4 { get; set; }
        public string Value5 { get; set; }
        public string Value6 { get; set; }
        public string Value7 { get; set; }

        public static StringType Create()
        {
            var st = new StringType();
            st.Value1 = st.Value2 = st.Value3 = st.Value4 = st.Value5 = st.Value6 = st.Value7 = "Hello, world";
            return st;
        }
    }
}