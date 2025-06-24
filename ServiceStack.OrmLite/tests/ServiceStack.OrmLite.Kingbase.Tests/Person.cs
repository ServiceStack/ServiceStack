using System;
using ServiceStack.DataAnnotations;

namespace ServiceStack.OrmLite.Kingbase.Tests;

public class Person
{
    [AutoIncrement, PrimaryKey] public long Id { get; set; }


    // 布尔型
    public bool IsActive { get; set; }

    // 整型
    public byte AgeInByte { get; set; }
    public sbyte SignedByte { get; set; }
    public short ShortValue { get; set; }
    public ushort UShortValue { get; set; }
    public int IntValue { get; set; }
    public uint UIntValue { get; set; }
    public long LongValue { get; set; }
    public ulong ULongValue { get; set; }

    // 浮点型
    public float FloatValue { get; set; }
    public double DoubleValue { get; set; }
    public decimal DecimalValue { get; set; }

    // 字符型
    public char Gender { get; set; }

    // 字符串
    public string Name { get; set; }

    // 日期时间
    [Default(OrmLiteVariables.SystemUtc)] public DateTime BirthDate { get; set; }
    [Default(OrmLiteVariables.SystemUtc)] public DateTimeOffset LastLogin { get; set; }

    // 时间间隔
    public TimeSpan WorkDuration { get; set; }

    // 可空类型示例
    public int? NullableInt { get; set; }

    // 对象类型
    public object Tag { get; set; }

    [Reference] public Address Address { get; set; }
}