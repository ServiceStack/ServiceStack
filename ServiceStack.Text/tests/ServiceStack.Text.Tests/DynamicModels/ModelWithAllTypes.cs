using System;
using ServiceStack.Text.Tests.DynamicModels.DataModel;

namespace ServiceStack.Text.Tests.DynamicModels
{
    public class ModelWithAllTypes
    {
        public Exception Exception { get; set; }

        public CustomException CustomException { get; set; }

        public Uri UriValue { get; set; }

        public Type TypeValue { get; set; }

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

        public static ModelWithAllTypes Create(byte i)
        {
            return new ModelWithAllTypes
            {
                ByteValue = i,
                CharValue = (char)i,
                CustomException = new CustomException("CustomException " + i),
                DateTimeValue = new DateTime(2000, 1, 1 + i),
                DecimalValue = i,
                DoubleValue = i,
                Exception = new Exception("Exception " + i),
                FloatValue = i,
                IntValue = i,
                LongValue = i,
                SByteValue = (sbyte)i,
                ShortValue = i,
                TimeSpanValue = new TimeSpan(i),
                TypeValue = typeof(ModelWithAllTypes),
                UIntValue = i,
                ULongValue = i,
                UriValue = new Uri("http://domain.com/" + i),
                UShortValue = i,
                GuidValue = Guid.NewGuid(),
            };
        }

        public override bool Equals(object obj)
        {
            if (!(obj is ModelWithAllTypes to))
                return false;

            if (ByteValue != to.ByteValue)
                return false;
            if (CharValue != to.CharValue)
                return false;
            if (CustomException.Message != to.CustomException.Message)
                return false;
            if (DateTimeValue != to.DateTimeValue)
                return false;
            if (DecimalValue != to.DecimalValue)
                return false;
            if (DoubleValue != to.DoubleValue)
                return false;
            if (Exception.Message != to.Exception.Message)
                return false;
            if (FloatValue != to.FloatValue)
                return false;
            if (IntValue != to.IntValue)
                return false;
            if (LongValue != to.LongValue)
                return false;
            if (SByteValue != to.SByteValue)
                return false;
            if (ShortValue != to.ShortValue)
                return false;
            if (TimeSpanValue != to.TimeSpanValue)
                return false;
            if (TypeValue != to.TypeValue)
                return false;
            if (UIntValue != to.UIntValue)
                return false;
            if (ULongValue != to.ULongValue)
                return false;
            if (UriValue.ToString() != to.UriValue.ToString())
                return false;
            if (UShortValue != to.UShortValue)
                return false;
            if (GuidValue != to.GuidValue)
                return false;

            return true;
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }
}