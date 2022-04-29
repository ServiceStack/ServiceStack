using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using NUnit.Framework;
using ServiceStack.DataAnnotations;
using ServiceStack.OrmLite.Converters;
using ServiceStack.Text;

namespace ServiceStack.OrmLite.Tests
{
    [TestFixtureOrmLite]
    public class ConverterTests : OrmLiteProvidersTestBase
    {
        public ConverterTests(DialectContext context) : base(context) {}
   
        private struct TestStruct {}

        [Test]
        public void FromDbValue_does_not_throw_Exception()
        {
            var dialectProvider = DialectProvider;
            var convertedValue = dialectProvider.FromDbValue(12345, typeof(TestStruct));
            Assert.That(convertedValue, Is.Null);
        }

        [Test]
        public void ToDbValue_does_not_throw_Exception()
        {
            var dialectProvider = DialectProvider;
            var convertedValue = dialectProvider.ToDbValue (89.123456789, typeof(decimal));
            Assert.AreEqual(convertedValue, 89.123456789);
        }

        [Test]
        public void ToQuotedString_FloatConverter_not_throw_Exception()
        {
            var floatConverter = new FloatConverter();
            floatConverter.ToQuotedString(typeof(decimal), 89.123456789);
            floatConverter.ToQuotedString(typeof(float), 89.123456789);
            floatConverter.ToQuotedString(typeof(double), 89.123456789);
        }

        [Test]
        public void ToQuotedString_LegacyFloatConverter_throw_Exception()
        {
            Assert.Catch(typeof(InvalidCastException), () =>
            {
                var floatConverter = new LegacyFloatConverter();
                floatConverter.ToQuotedString(typeof(decimal), 89.123456789);
                floatConverter.ToQuotedString(typeof(float), 89.123456789);
                floatConverter.ToQuotedString(typeof(double), 89.123456789);
            });
        }

        [Test]
        public void Can_insert_update_and_select_AllTypes()
        {
            using var db = OpenDbConnection();
            if (Dialect == Dialect.Firebird) //Exceeds row limit
                DialectProvider.GetStringConverter().MaxColumnDefinition = "BLOB SUB_TYPE 1 SEGMENT SIZE 8192";

            db.DropAndCreateTable<AllTypes>();
            db.GetLastSql().Print();

            var rows = 3.Times(i => AllTypes.Create(i));

            db.InsertAll(rows);

            var lastRow = rows.Last();

            var dbRow = db.SingleById<AllTypes>(lastRow.Id);

            Assert.That(dbRow, Is.EqualTo(lastRow));

            Assert.That(db.Single<AllTypes>(x => x.NullableId == lastRow.NullableId), Is.EqualTo(lastRow));
            Assert.That(db.Single<AllTypes>(x => x.Byte == lastRow.Byte), Is.EqualTo(lastRow));
            Assert.That(db.Single<AllTypes>(x => x.Short == lastRow.Short), Is.EqualTo(lastRow));
            Assert.That(db.Single<AllTypes>(x => x.Int == lastRow.Int), Is.EqualTo(lastRow));
            Assert.That(db.Single<AllTypes>(x => x.Long == lastRow.Long), Is.EqualTo(lastRow));
            Assert.That(db.Single<AllTypes>(x => x.UShort == lastRow.UShort), Is.EqualTo(lastRow));
            Assert.That(db.Single<AllTypes>(x => x.UInt == lastRow.UInt), Is.EqualTo(lastRow));
            Assert.That(db.Single<AllTypes>(x => x.ULong == lastRow.ULong), Is.EqualTo(lastRow));
            Assert.That(db.Single<AllTypes>(x => x.Decimal == lastRow.Decimal), Is.EqualTo(lastRow));
            Assert.That(db.Single<AllTypes>(x => x.String == lastRow.String), Is.EqualTo(lastRow));
            Assert.That(db.Single<AllTypes>(x => x.DateTime == lastRow.DateTime), Is.EqualTo(lastRow));
            Assert.That(db.Single<AllTypes>(x => x.TimeSpan == lastRow.TimeSpan), Is.EqualTo(lastRow));
            Assert.That(db.Single<AllTypes>(x => x.DateTimeOffset == lastRow.DateTimeOffset), Is.EqualTo(lastRow));
            Assert.That(db.Single<AllTypes>(x => x.Guid == lastRow.Guid), Is.EqualTo(lastRow));
            Assert.That(db.Single<AllTypes>(x => x.Char == lastRow.Char), Is.EqualTo(lastRow));
            Assert.That(db.Single<AllTypes>(x => x.NullableDateTime == lastRow.NullableDateTime), Is.EqualTo(lastRow));
            Assert.That(db.Single<AllTypes>(x => x.NullableTimeSpan == lastRow.NullableTimeSpan), Is.EqualTo(lastRow));
            Assert.That(db.Single<AllTypes>(x => x.CustomText == lastRow.CustomText), Is.EqualTo(lastRow));
            Assert.That(db.Single<AllTypes>(x => x.MaxText == lastRow.MaxText), Is.EqualTo(lastRow));
            Assert.That(db.Single<AllTypes>(x => x.CustomDecimal == lastRow.CustomDecimal), Is.EqualTo(lastRow));

            Assert.That(db.Single(db.From<AllTypes>().Where(x => x.Bool == lastRow.Bool).OrderByDescending(x => x.Id)), Is.EqualTo(lastRow));

            var updatedRows = 3.Times(i =>
            {
                var updated = AllTypes.Create(i + 3);
                updated.Id = i + 1;
                db.Update(updated);
                return updated;
            });

            var lastUpdatedRow = updatedRows.Last();
            var dbUpdatedRow = db.SingleById<AllTypes>(lastUpdatedRow.Id);
            Assert.That(dbUpdatedRow, Is.EqualTo(lastUpdatedRow));

            if (Dialect == Dialect.Firebird)
                DialectProvider.GetStringConverter().MaxColumnDefinition = null;
        }
    }

    public class AllTypes
    {
        public int Id { get; set; }
        public int? NullableId { get; set; }
        public byte Byte { get; set; }
        public short Short { get; set; }
        public int Int { get; set; }
        public long Long { get; set; }
        public ushort UShort { get; set; }
        public uint UInt { get; set; }
        public ulong ULong { get; set; }
        public float Float { get; set; }
        public double Double { get; set; }
        public decimal Decimal { get; set; }
        public string String { get; set; }
        public DateTime DateTime { get; set; }
        public TimeSpan TimeSpan { get; set; }
        public DateTimeOffset DateTimeOffset { get; set; }
        public Guid Guid { get; set; }
        public bool Bool { get; set; }
        public char Char { get; set; }
        public DateTime? NullableDateTime { get; set; }
        public TimeSpan? NullableTimeSpan { get; set; }
        public byte[] ByteArray { get; set; }
        public char[] CharArray { get; set; }
        public int[] IntArray { get; set; }
        public long[] LongArray { get; set; }
        public string[] StringArray { get; set; }
        public List<string> StringList { get; set; }
        public Dictionary<string, string> StringMap { get; set; }
        public Dictionary<int, string> IntStringMap { get; set; }
        public SubType SubType { get; set; }
        public List<SubType> SubTypes { get; set; }

        [StringLength(100)]
        public string CustomText { get; set; }

        [StringLength(StringLengthAttribute.MaxText)]
        public string MaxText { get; set; }

        [DecimalLength(10,2)]
        public decimal CustomDecimal { get; set; }

        protected bool Equals(AllTypes other)
        {
            return Id == other.Id &&
                NullableId == other.NullableId &&
                Byte == other.Byte &&
                Short == other.Short &&
                Int == other.Int &&
                Long == other.Long &&
                UShort == other.UShort &&
                UInt == other.UInt &&
                ULong == other.ULong &&
                Float.Equals(other.Float) &&
                Double.Equals(other.Double) &&
                Decimal == other.Decimal &&
                string.Equals(String, other.String) &&
                DateTime.Equals(other.DateTime) &&
                TimeSpan.Equals(other.TimeSpan) &&
                DateTimeOffset.Equals(other.DateTimeOffset) &&
                Guid.Equals(other.Guid) &&
                Bool == other.Bool &&
                Char == other.Char &&
                NullableDateTime.Equals(other.NullableDateTime) &&
                NullableTimeSpan.Equals(other.NullableTimeSpan) &&
                ByteArray.SequenceEqual(other.ByteArray) &&
                CharArray.SequenceEqual(other.CharArray) &&
                IntArray.SequenceEqual(other.IntArray) &&
                LongArray.SequenceEqual(other.LongArray) &&
                StringArray.SequenceEqual(other.StringArray) &&
                StringList.SequenceEqual(other.StringList) &&
                StringMap.SequenceEqual(other.StringMap) &&
                IntStringMap.SequenceEqual(other.IntStringMap) &&
                SubType.Equals(other.SubType) &&
                SubTypes.SequenceEqual(other.SubTypes) &&
                CustomText == other.CustomText &&
                MaxText == other.MaxText &&
                CustomDecimal.Equals(other.CustomDecimal);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((AllTypes)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = Id;
                hashCode = (hashCode * 397) ^ NullableId.GetHashCode();
                hashCode = (hashCode * 397) ^ Byte.GetHashCode();
                hashCode = (hashCode * 397) ^ Short.GetHashCode();
                hashCode = (hashCode * 397) ^ Int;
                hashCode = (hashCode * 397) ^ Long.GetHashCode();
                hashCode = (hashCode * 397) ^ UShort.GetHashCode();
                hashCode = (hashCode * 397) ^ (int)UInt;
                hashCode = (hashCode * 397) ^ ULong.GetHashCode();
                hashCode = (hashCode * 397) ^ Float.GetHashCode();
                hashCode = (hashCode * 397) ^ Double.GetHashCode();
                hashCode = (hashCode * 397) ^ Decimal.GetHashCode();
                hashCode = (hashCode * 397) ^ (String != null ? String.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ DateTime.GetHashCode();
                hashCode = (hashCode * 397) ^ TimeSpan.GetHashCode();
                hashCode = (hashCode * 397) ^ DateTimeOffset.GetHashCode();
                hashCode = (hashCode * 397) ^ Guid.GetHashCode();
                hashCode = (hashCode * 397) ^ Bool.GetHashCode();
                hashCode = (hashCode * 397) ^ Char.GetHashCode();
                hashCode = (hashCode * 397) ^ NullableDateTime.GetHashCode();
                hashCode = (hashCode * 397) ^ NullableTimeSpan.GetHashCode();
                hashCode = (hashCode * 397) ^ (ByteArray != null ? ByteArray.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (CharArray != null ? CharArray.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (StringArray != null ? StringArray.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (IntArray != null ? IntArray.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (LongArray != null ? LongArray.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (StringList != null ? StringList.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (StringMap != null ? StringMap.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (IntStringMap != null ? IntStringMap.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (SubType != null ? SubType.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (SubTypes != null ? SubTypes.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (CustomText != null ? CustomText.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (MaxText != null ? MaxText.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ CustomDecimal.GetHashCode();
                return hashCode;
            }
        }

        public static AllTypes Create(int i)
        {
            return new AllTypes
            {
                Id = i + 1,
                NullableId = i + 2,
                Byte = (byte)(i + 3),
                Short = (short)(i + 4),
                Int = i + 5,
                Long = i + 6,
                UShort = (ushort)(i + 7),
                UInt = (uint)(i + 8),
                ULong = (ulong)(i + 9),
                Float = (float)(i + 10.1),
                Double = i + 11.5,
                Decimal = (decimal)(i + 12.1),
                String = "String" + i,
                DateTime = new DateTime(2000 + i, (i + 1) % 12, (i + 1) % 28),
                TimeSpan = new TimeSpan(i, i, i, i, i),
                DateTimeOffset = new DateTimeOffset(new DateTime(2000 + i, (i + 1) % 12, (i + 1) % 28)),
                Guid = Guid.NewGuid(),
                Bool = i % 2 == 0,
                Char = (char)(i + 65), //TODO: NPGSQL fails on \0
                NullableDateTime = new DateTime(2000 + i, (i + 1) % 12, (i + 1) % 28),
                NullableTimeSpan = new TimeSpan(i, i, i, i, i),
                ByteArray = new[] { (byte)i, (byte)(i + 1) },
                CharArray = new[] { (char)('A' + i), (char)('A' + i + 1) }, //TODO: NPGSQL fails on \u0001
                IntArray = new[] { i, i + 1 },
                LongArray = new[] { (long)i, i + 1 },
                StringArray = new[] { i.ToString() },
                StringList = new List<string> { i.ToString() },
                StringMap = new Dictionary<string, string> { { "Key" + i, "Value" + i } },
                IntStringMap = new Dictionary<int, string> { { i, "Value" + i } },
                SubType = new SubType
                {
                    Id = i,
                    Name = "Name" + i,
                },
                SubTypes = new List<SubType>
                {
                    new SubType
                    {
                        Id = i,
                        Name = "Name" + i,
                    },
                    new SubType
                    {
                        Id = i + 1,
                        Name = "Name" + i + 1,
                    },
                },
                CustomText = "CustomText" + i,
                MaxText = "MaxText" + i,
                CustomDecimal = i + 13.13M,
            };
        }
    }

    public class SubType
    {
        public int Id { get; set; }
        public string Name { get; set; }

        protected bool Equals(SubType other)
        {
            return Id == other.Id && string.Equals(Name, (string)other.Name);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((SubType)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (Id * 397) ^ (Name != null ? Name.GetHashCode() : 0);
            }
        }
    }

    //A copy of the original implementation code 
    internal class LegacyFloatConverter : NativeValueOrmLiteConverter
    {
        public override string ColumnDefinition => "DOUBLE";
        public override DbType DbType => DbType.Single;

        public override object ToDbValue(Type fieldType, object value)
        {
            return this.ConvertNumber(fieldType, value);
        }

        public override object FromDbValue(Type fieldType, object value)
        {
            return this.ConvertNumber(fieldType, value);
        }

        public override string ToQuotedString(Type fieldType, object value)
        {
            var typeCode = fieldType.GetTypeCode();
            switch (typeCode)
            {
                case TypeCode.Single:
                    return ((float)value).ToString(CultureInfo.InvariantCulture);
                case TypeCode.Double:
                    return ((double)value).ToString(CultureInfo.InvariantCulture);
                case TypeCode.Decimal:
                    return ((decimal)value).ToString(CultureInfo.InvariantCulture);
            }

            return base.ToQuotedString(fieldType, value);
        }
    }
}