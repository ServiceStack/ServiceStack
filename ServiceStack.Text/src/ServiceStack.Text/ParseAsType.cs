using System;

namespace ServiceStack.Text
{
    [Flags]
    public enum ParseAsType
    {
        None = 0,
        Bool = 2,
        Byte = 4,
        SByte = 8,
        Int16 = 16,
        Int32 = 32,
        Int64 = 64,
        UInt16 = 128,
        UInt32 = 256,
        UInt64 = 512,
        Decimal = 1024,
        Double = 2048,
        Single = 4096
    }
}