using System;
using System.Reflection;

namespace ServiceStack;

/// <summary>
/// Configure result field to use JavaScript's Intl.NumberFormat formatter 
/// </summary>
public class IntlNumber : Intl
{
    public IntlNumber() : base(IntlFormat.Number) {}
    public IntlNumber(NumberStyle style) : base(IntlFormat.Number)
    {
        Number = style;
    }
    public override bool ShouldInclude(PropertyInfo pi, string value) => pi.Name != nameof(Type) && base.ShouldInclude(pi, value);
}

/// <summary>
/// Configure result field to use JavaScript's Intl.DateTimeFormat formatter 
/// </summary>
public class IntlDateTime : Intl
{
    public IntlDateTime() : base(IntlFormat.DateTime) {}
    public IntlDateTime(DateStyle date, TimeStyle time = TimeStyle.Undefined) : base(IntlFormat.DateTime)
    {
        Date = date;
        Time = time;
    }
    public override bool ShouldInclude(PropertyInfo pi, string value) => pi.Name != nameof(Type) && base.ShouldInclude(pi, value);

}

/// <summary>
/// Configure result field to use JavaScript's Intl.RelativeTimeFormat formatter 
/// </summary>
public class IntlRelativeTime : Intl
{
    public IntlRelativeTime() : base(IntlFormat.RelativeTime) {}
    public IntlRelativeTime(Numeric numeric) : base(IntlFormat.RelativeTime)
    {
        Numeric = numeric;
    }

    public override bool ShouldInclude(PropertyInfo pi, string value) => pi.Name != nameof(Type) && base.ShouldInclude(pi, value);
}

/// <summary>
/// Configure result field to use a JavaScript Intl formatter 
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
public class Intl : MetadataAttributeBase
{
    public Intl() {}
    public Intl(IntlFormat type) => Type = type;

    public IntlFormat Type { get; set; }
    public string Locale { get; set; }
    public string Options { get; set; }
    
    public NumberStyle Number { get; set; }
    public DateStyle Date { get; set; }
    public TimeStyle Time { get; set; }
    public RelativeTimeStyle RelativeTime { get; set; }
    public Numeric Numeric { get; set; }
    /// <summary>
    /// Use <see cref="NumberCurrency"/> for typed values
    /// </summary>
    public string Currency { get; set; }
    public CurrencyDisplay CurrencyDisplay { get; set; }
    public CurrencySign CurrencySign { get; set; }
    public SignDisplay SignDisplay { get; set; }
    public RoundingMode RoundingMode { get; set; }
    /// <summary>
    /// Use <see cref="NumberUnit"/> for typed values
    /// </summary>
    public string Unit { get; set; }
    public UnitDisplay UnitDisplay { get; set; }
    public Notation Notation { get; set; }
    public int MinimumIntegerDigits { get; set; } = int.MinValue;
    public int MinimumFractionDigits { get; set; } = int.MinValue;
    public int MaximumFractionDigits { get; set; } = int.MinValue;
    public int MinimumSignificantDigits { get; set; } = int.MinValue;
    public int MaximumSignificantDigits { get; set; } = int.MinValue;
    public int FractionalSecondDigits { get; set; } = int.MinValue;
    
    public DateText Weekday { get; set; }
    public DateText Era { get; set; }
    public DatePart Year { get; set; }
    public DateMonth Month { get; set; }
    public DatePart Day { get; set; }
    public DatePart Hour { get; set; }
    public DatePart Minute { get; set; }
    public DatePart Second { get; set; }
    public DateText TimeZoneName { get; set; }
    public string TimeZone { get; set; }
    public bool Hour12 { get; set; }
}

public enum IntlFormat
{
    /// <summary>
    /// Intl.NumberFormat
    /// </summary>
    Number,
    
    /// <summary>
    /// Intl.DateTimeFormat
    /// </summary>
    DateTime,
    
    /// <summary>
    /// Intl.RelativeTimeFormat
    /// </summary>
    RelativeTime,
}

public enum DateStyle { Undefined=0, Full, Long, Medium, Short, }
public enum TimeStyle { Undefined=0, Full, Long, Medium, Short, }
public enum NumberStyle { Undefined=0, Decimal, Currency, Percent, Unit, }
public enum RelativeTimeStyle { Undefined=0, Long, Short, Narrow, }
public enum Numeric { Undefined=0, Always, Auto, }

public enum DatePart { Undefined=0, Numeric, Digits2, }
public enum DateMonth { Undefined=0, Numeric, Digits2, Narrow, Short, Long, }
public enum DateText { Undefined=0, Narrow, Short, Long }
public enum UnitDisplay { Undefined=0, Long, Short, Narrow }
public enum Notation { Undefined=0, Standard, Scientific, Engineering, Compact, }
public enum CurrencyDisplay { Undefined=0, Symbol, NarrowSymbol, Code, Name, }
public enum CurrencySign { Undefined=0, Accounting, Standard, }
public enum SignDisplay { Undefined=0, Always, Auto, ExceptZero, Negative, Never, }
public enum RoundingMode { Undefined=0, Ceil, Floor, Expand, Trunc, HalfCeil, HalfFloor, HalfExpand, HalfTrunc, HalfEven, }

public static class NumberCurrency
{
    public const string USD = nameof(USD);
    public const string EUR = nameof(EUR);
    public const string JPY = nameof(JPY);
    public const string GBP = nameof(GBP);
    public const string CHF = nameof(CHF);
    public const string CAD = nameof(CAD);
    public const string AUD = nameof(AUD);
    public const string ZAR = nameof(ZAR);
    public const string CNY = nameof(CNY);
    public const string HKD = nameof(HKD);
    public const string NZD = nameof(NZD);
    public const string SEK = nameof(SEK);
    public const string KRW = nameof(KRW);
    public const string SGD = nameof(SGD);
    public const string NOK = nameof(NOK);
    public const string MXN = nameof(MXN);
    public const string INR = nameof(INR);
    public const string RUB = nameof(RUB);
    public const string TRY = nameof(TRY);
    public const string BRL = nameof(BRL);
    public const string TWD = nameof(TWD);
    public const string DKK = nameof(DKK);
    public const string PLN = nameof(PLN);
    public const string THB = nameof(THB);
    public const string IDR = nameof(IDR);
    public const string HUF = nameof(HUF);
    public const string CZK = nameof(CZK);
    public const string ILS = nameof(ILS);
    public const string CLP = nameof(CLP);
    public const string PHP = nameof(PHP);
    public const string AED = nameof(AED);
    public const string COP = nameof(COP);
    public const string SAR = nameof(SAR);
    public const string MYR = nameof(MYR);
    public const string RON = nameof(RON);

    public static string[] All = new[]
    {
        USD,EUR,JPY,GBP,CHF,CAD,AUD,ZAR,CNY,HKD,NZD,KRW,SGD,NOK,MXN,INR,RUB,
        TRY,BRL,TWD,DKK,PLN,THB,IDR,HUF,CZK,ILS,CLP,PHP,AED,COP,SAR,MYR,RON,
    };
}

public static class NumberUnit
{
    public const string Acre  = "acre";
    public const string Bit  = "bit";
    public const string Byte  = "byte";
    public const string Celsius  = "celsius";
    public const string Centimeter  = "centimeter";
    public const string Day  = "day";
    public const string Degree  = "degree";
    public const string Fahrenheit  = "fahrenheit";
    public const string Foot  = "foot";
    public const string Gallon  = "gallon";
    public const string Gigabit  = "gigabit";
    public const string Gigabyte  = "gigabyte";
    public const string Gram  = "gram";
    public const string Hectare  = "hectare";
    public const string Hour  = "hour";
    public const string Inch  = "inch";
    public const string Kilobit  = "kilobit";
    public const string Kilobyte  = "kilobyte";
    public const string Kilogram  = "kilogram";
    public const string Kilometer  = "kilometer";
    public const string Liter  = "liter";
    public const string Megabit  = "megabit";
    public const string Megabyte  = "megabyte";
    public const string Meter  = "meter";
    public const string Mile  = "mile";
    public const string Milliliter  = "milliliter";
    public const string Millimeter  = "millimeter";
    public const string Millisecond  = "millisecond";
    public const string Minute  = "minute";
    public const string Month  = "month";
    public const string Ounce  = "ounce";
    public const string Percent  = "percent";
    public const string Petabyte  = "petabyte";
    public const string Pound  = "pound";
    public const string Second  = "second";
    public const string Stone  = "stone";
    public const string Terabit  = "terabit";
    public const string Terabyte  = "terabyte";
    public const string Week  = "week";
    public const string Yard  = "yard";
    public const string Year  = "year";
}
