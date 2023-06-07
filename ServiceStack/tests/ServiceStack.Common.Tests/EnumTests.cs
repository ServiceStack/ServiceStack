using System;
using System.Linq;
using NUnit.Framework;

namespace ServiceStack.Common.Tests;

[Flags]
public enum MailingList
{
    None = 0,
    [ServiceStack.DataAnnotations.Description("Test Group")]
    TestGroup = 1 << 0,
    [ServiceStack.DataAnnotations.Description("Monthly Newsletter")]
    MonthlyNewsletter = 1 << 1,
    [ServiceStack.DataAnnotations.Description("New Blog Posts")]
    BlogPostReleases = 1 << 2,
    [ServiceStack.DataAnnotations.Description("New Videos")]
    VideoReleases = 1 << 3,
    [ServiceStack.DataAnnotations.Description("New Product Releases")]
    ProductReleases = 1 << 4,
    [ServiceStack.DataAnnotations.Description("Yearly Updates")]
    YearlyUpdates = 1 << 5,
}

public class EnumTests
{
    [Test]
    public void Does_convert_string_list_to_enum_flag()
    {
        var enums = new[] { "Test Group", "New Videos", "MonthlyNewsletter" }.ToList();
        var result = EnumUtils.FromEnumFlagsList<MailingList>(enums);
        Assert.That(result, Is.EqualTo(MailingList.TestGroup | MailingList.VideoReleases | MailingList.MonthlyNewsletter));
    }

    [Test]
    public void Does_convert_enum_flag_to_string_list()
    {
        var enums = MailingList.TestGroup | MailingList.VideoReleases | MailingList.MonthlyNewsletter;
        var result = enums.ToEnumFlagsList();
        Assert.That(result, Is.EquivalentTo(new[] { "Test Group", "New Videos", "Monthly Newsletter" }));
    }
}
