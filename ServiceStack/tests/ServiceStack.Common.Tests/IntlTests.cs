using System;
using System.Linq;
using NUnit.Framework;
using ServiceStack.NativeTypes;
using ServiceStack.Testing;

namespace ServiceStack.Common.Tests;

public class IntlDateExamples
{
    [IntlDateTime]
    public DateTime Example1 { get; set; }
    [IntlDateTime(DateStyle.Medium, TimeStyle.Short, Locale = "en-AU")]
    public DateTime Example2 { get; set; }
    [IntlDateTime(DateStyle.Short)]
    public DateTime Example3 { get; set; }
    [IntlDateTime(Year = DatePart.Digits2, Month = DateMonth.Short, Day = DatePart.Numeric)]
    public DateTime Example4 { get; set; }
}

public class IntlNumberExamples
{
    [IntlNumber]
    public int Example1 { get; set; }
    [IntlNumber(NumberStyle.Decimal, RoundingMode = RoundingMode.HalfCeil, SignDisplay = SignDisplay.ExceptZero, Locale = "en-AU")]
    public int Example2 { get; set; }
    [IntlNumber(Currency = NumberCurrency.USD)]
    public int Example3 { get; set; }
    [IntlNumber(Currency = NumberCurrency.USD, CurrencyDisplay = CurrencyDisplay.NarrowSymbol, CurrencySign = CurrencySign.Accounting)]
    public int Example4 { get; set; }
    [IntlNumber(Unit = NumberUnit.Kilobyte)]
    public int Example5 { get; set; }
}

public class IntlRelativeTimeExamples
{
    [IntlRelativeTime]
    public int Example1 { get; set; }
    [IntlRelativeTime(Numeric.Always)]
    public int Example2 { get; set; }
}

public class CustomFormatExamples
{
    [Format(Method = "currency", Locale = "en-AU")]
    public decimal Example1 { get; set; }
    [Format(Method = "Intl.NumberFormat", Options = "{style:'currency',currency:'USD'}")]
    public decimal Example2 { get; set; }
}

public class IntlRequest
{
    public IntlDateExamples Date { get; set; }
    public IntlNumberExamples Number { get; set; }
    public CustomFormatExamples Custom { get; set; }
}

public class IntlServices : Service
{
    public object Any(IntlRequest request) => request;
}

public class IntlTests
{
    private ServiceStackHost appHost;

    public IntlTests() => appHost = new BasicAppHost(typeof(IntlServices).Assembly)
    {
        Plugins = { new NativeTypesFeature() },
    }.Init();

    [OneTimeTearDown]
    public void OneTimeTearDown() => appHost.Dispose();

    void AssertFormat(FormatInfo actual, string expectedMethod, string expectedOptions = null, string expectedLocale = null)
    {
        Assert.That(actual, Is.Not.Null);
        Assert.That(actual.Method, Is.EqualTo(expectedMethod));
        Assert.That(actual.Options, Is.EqualTo(expectedOptions));
        Assert.That(actual.Locale, Is.EqualTo(expectedLocale));
    }

    FormatInfo Format(MetadataType dto, string name) => dto.Properties.First(x => x.Name == name).Format; 

    [Test]
    public void Does_IntlDateExamples()
    {
        var gen = appHost.Resolve<INativeTypesMetadata>().GetGenerator();
        var dto = gen.ToType(typeof(IntlDateExamples));
        AssertFormat(Format(dto,nameof(IntlDateExamples.Example1)), "Intl.DateTimeFormat");
        AssertFormat(Format(dto,nameof(IntlDateExamples.Example2)), "Intl.DateTimeFormat", "{dateStyle:'medium',timeStyle:'short'}", "en-AU");
        AssertFormat(Format(dto,nameof(IntlDateExamples.Example3)), "Intl.DateTimeFormat", "{dateStyle:'short'}");
        AssertFormat(Format(dto,nameof(IntlDateExamples.Example4)), "Intl.DateTimeFormat", "{year:'2-digit',month:'short',day:'numeric'}");
    }

    [Test]
    public void Does_IntlNumberExamples()
    {
        var gen = appHost.Resolve<INativeTypesMetadata>().GetGenerator();
        var dto = gen.ToType(typeof(IntlNumberExamples));
        AssertFormat(Format(dto,nameof(IntlNumberExamples.Example1)), "Intl.NumberFormat");
        AssertFormat(Format(dto,nameof(IntlNumberExamples.Example2)), "Intl.NumberFormat", "{style:'decimal',roundingMode:'halfCeil',signDisplay:'exceptZero'}", "en-AU");
        AssertFormat(Format(dto,nameof(IntlNumberExamples.Example3)), "Intl.NumberFormat", "{style:'currency',currency:'USD'}");
        AssertFormat(Format(dto,nameof(IntlNumberExamples.Example4)), "Intl.NumberFormat", "{style:'currency',currency:'USD',currencyDisplay:'narrowSymbol',currencySign:'accounting'}");
        AssertFormat(Format(dto,nameof(IntlNumberExamples.Example5)), "Intl.NumberFormat", "{style:'unit',unit:'kilobyte'}");
    }

    [Test]
    public void Does_IntlRelativeTimeExamples()
    {
        var gen = appHost.Resolve<INativeTypesMetadata>().GetGenerator();
        var dto = gen.ToType(typeof(IntlRelativeTimeExamples));
        AssertFormat(Format(dto,nameof(IntlRelativeTimeExamples.Example1)), "Intl.RelativeTimeFormat");
        AssertFormat(Format(dto,nameof(IntlRelativeTimeExamples.Example2)), "Intl.RelativeTimeFormat", "{numeric:'always'}");
    }

    [Test]
    public void Does_CustomFormatExamples_Examples()
    {
        var gen = appHost.Resolve<INativeTypesMetadata>().GetGenerator();
        var dto = gen.ToType(typeof(CustomFormatExamples));
        AssertFormat(Format(dto,nameof(CustomFormatExamples.Example1)), "currency", expectedLocale:"en-AU");
        AssertFormat(Format(dto,nameof(CustomFormatExamples.Example2)), "Intl.NumberFormat", "{style:'currency',currency:'USD'}");
    }
}