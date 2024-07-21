using System;
using ServiceStack.DataAnnotations;

namespace ServiceStack.OrmLite.Tests.Expression;

public class TestType
{
    public int IntColumn { get; set; }
    public bool BoolColumn { get; set; }
    public string StringColumn { get; set; }
    public object NullableCol { get; set; }
    public TimeSpan TimeSpanColumn { get; set; }
    public DateTime? Date { get; set; }

    [AutoIncrement]
    public int Id { get; set; }

    public bool Equals(TestType other)
    {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;
        return other.IntColumn == IntColumn && other.BoolColumn.Equals(BoolColumn) && Equals(other.StringColumn, StringColumn);
    }

    public override bool Equals(object obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != typeof (TestType)) return false;
        return Equals((TestType) obj);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            int result = IntColumn;
            result = (result*397) ^ BoolColumn.GetHashCode();
            result = (result*397) ^ (StringColumn != null ? StringColumn.GetHashCode() : 0);
            return result;
        }
    }
}