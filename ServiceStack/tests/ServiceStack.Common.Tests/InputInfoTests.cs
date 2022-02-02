#nullable enable

using System;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using NUnit.Framework;
using ServiceStack.Auth;
using ServiceStack.Configuration;
using ServiceStack.DataAnnotations;
using ServiceStack.Html;
using ServiceStack.Text;

namespace ServiceStack.Common.Tests;

class MultiTypes
{
    public int Id { get; set; }
    public DateTime Date { get; set; }
    public DateTime? NDate { get; set; }
    public bool Bool { get; set; }
    public string String { get; set; }
}

public class InputTests
{
    void AssertProp(PropertyInfo? pi, Type type, string name)
    {
        Assert.That(pi, Is.Not.Null);
        Assert.That(pi!.Name, Is.EqualTo(name));
        Assert.That(pi!.PropertyType, Is.EqualTo(type));
    }
    
    [Test]
    public void Does_resolve_properties()
    {
        AssertProp(InspectUtils.PropertyFromExpression<MultiTypes>(x => x.Id), typeof(int), nameof(MultiTypes.Id));
        AssertProp(InspectUtils.PropertyFromExpression<MultiTypes>(x => x.Date), typeof(DateTime), nameof(MultiTypes.Date));
        AssertProp(InspectUtils.PropertyFromExpression<MultiTypes>(x => x.NDate), typeof(DateTime?), nameof(MultiTypes.NDate));
        AssertProp(InspectUtils.PropertyFromExpression<MultiTypes>(x => x.Bool), typeof(bool), nameof(MultiTypes.Bool));
        AssertProp(InspectUtils.PropertyFromExpression<MultiTypes>(x => x.String), typeof(String), nameof(MultiTypes.String));
    }

    public enum EnumMemberTest
    {
        [EnumMember(Value = "No ne")] None = 0,
        [EnumMember(Value = "Template")] Template = 1,
        [EnumMember(Value = "Rule")] Rule = 3,
    }
    public enum EnumWithValues
    {
        None = 0,
        [EnumMember(Value = "Member 1")]
        Value1 = 1,
        [DataAnnotations.Description("Member 2")]
        Value2 = 2,
    }

    [Flags]
    public enum EnumFlags
    {
        Value0 = 0,
        [EnumMember(Value = "Value 1")]
        Value1 = 1,
        [DataAnnotations.Description("Value 2")]
        Value2 = 2,
        Value3 = 4,
        Value123 = Value1 | Value2 | Value3,
    }

    [EnumAsInt]
    public enum EnumAsInt
    {
        Value1 = 1000,
        Value2 = 2000,
        Value3 = 3000,
    }

    public enum EnumStyle
    {
        lower,
        UPPER,
        PascalCase,
        camelCase,
        camelUPPER,
        PascalUPPER,
    }

    [Test]
    public void Print_GetEnumPairs()
    {
        void Print(Type enumType)
        {
            Input.GetEnumEntries(enumType, out var entries);
            entries.PrintDump();
        }
        
        Print(typeof(Lang));
        Print(typeof(EnumMemberTest));
        Print(typeof(EnumWithValues));
        Print(typeof(EnumFlags));
        Print(typeof(EnumAsInt));
        Print(typeof(EnumStyle));
    }

    [Test]
    public void Does_resolve_enum_properties()
    {
        Input.GetEnumEntries(typeof(Lang), out var enumEntries);
        Assert.That(enumEntries[0].Key, Is.EqualTo($"{(int)Lang.CSharp}"));
        Assert.That(enumEntries[0].Value, Is.EqualTo(nameof(Lang.CSharp)));

        Input.GetEnumEntries(typeof(EnumMemberTest), out enumEntries);
        Assert.That(enumEntries[0].Key, Is.EqualTo($"{(int)EnumMemberTest.None}"));
        Assert.That(enumEntries[0].Value, Is.EqualTo("No ne"));
    }

    [Test]
    public void Does_resolve_property_names()
    {
        Assert.That(InspectUtils.GetFieldNames<MultiTypes>(x => x.Id), Is.EquivalentTo(new[]{ nameof(MultiTypes.Id) }));
        Assert.That(InspectUtils.GetFieldNames<MultiTypes>(x => x.Date), Is.EquivalentTo(new[]{ nameof(MultiTypes.Date) }));
        Assert.That(InspectUtils.GetFieldNames<MultiTypes>(x => x.NDate), Is.EquivalentTo(new[]{ nameof(MultiTypes.NDate) }));
        Assert.That(InspectUtils.GetFieldNames<MultiTypes>(x => x.String), Is.EquivalentTo(new[]{ nameof(MultiTypes.String) }));

        Assert.That(InspectUtils.GetFieldNames<MultiTypes>(x => new { x.String }), Is.EquivalentTo(new[]{ nameof(MultiTypes.String) }));
        Assert.That(InspectUtils.GetFieldNames<MultiTypes>(x => new { x.Id, x.Date, x.NDate, x.String }), Is.EquivalentTo(new[] {
            nameof(MultiTypes.Id), nameof(MultiTypes.Date), nameof(MultiTypes.NDate), nameof(MultiTypes.String), 
        }));
    }

    [Test]
    public void Can_create_MediaRule()
    {
        var rule = MediaRules.Small.Show<MultiTypes>(x => new { x.Id, x.Date, x.NDate, x.String });
        Assert.That(rule.Size, Is.EqualTo(MediaSizes.Small));
        Assert.That(rule.Rule, Is.EqualTo(nameof(MediaRuleCreator.Show)));
        Assert.That(rule.ApplyTo, Is.EquivalentTo(new[] {
            nameof(MultiTypes.Id), nameof(MultiTypes.Date), nameof(MultiTypes.NDate), nameof(MultiTypes.String), 
        }));
    }

    [Test]
    public void Does_find_correct_min_media_size()
    {
        var mediaRules = new[] {
            MediaRules.ExtraSmall.Show<UserAuth>(x => new { x.Id, x.Email, x.DisplayName }),
            MediaRules.Small.Show<UserAuth>(x => new { x.Company, x.CreatedDate }),
        };
        
        Assert.That(mediaRules.MinVisibleSize(nameof(UserAuth.Id)), Is.EqualTo(MediaSizes.ExtraSmall));
        Assert.That(mediaRules.MinVisibleSize(nameof(UserAuth.DisplayName)), Is.EqualTo(MediaSizes.ExtraSmall));
        Assert.That(mediaRules.MinVisibleSize(nameof(UserAuth.CreatedDate)), Is.EqualTo(MediaSizes.Small));
        Assert.That(mediaRules.MinVisibleSize(nameof(UserAuth.Nickname)), Is.EqualTo(MediaSizes.Medium));
        
        Assert.That(mediaRules.Reverse().MinVisibleSize(nameof(UserAuth.Id)), Is.EqualTo(MediaSizes.ExtraSmall));
    }
    
}