#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace ServiceStack.Html;

public static class Media
{
}
public class MediaRuleCreator
{
    public MediaRuleCreator(string size)
    {
        Size = size;
    }
    public string Size { get; }

    public MediaRule Show<T>(Expression<Func<T,object?>> expr)
    {
        var fieldNames = expr.GetFieldNames();
        return new() { Rule = nameof(Show), Size = Size, ApplyTo = fieldNames };
    }
}

public static class MediaRules
{
    public static MediaRuleCreator ExtraSmall = new(MediaSizes.ExtraSmall);
    public static MediaRuleCreator Small = new(MediaSizes.Small);
    public static MediaRuleCreator Medium = new(MediaSizes.Medium);
    public static MediaRuleCreator Large = new(MediaSizes.Large);
    public static MediaRuleCreator ExtraLarge = new(MediaSizes.ExtraLarge);
    public static MediaRuleCreator ExtraLarge2x = new(MediaSizes.ExtraLarge2x);

    /// <summary>
    /// Returns the minimum visible size when the target should be visible.
    /// Returns next Size if no rule was defined for the target.
    /// Returns ExtraSmall (xs) if there were no 'Show' Media Rules.
    /// </summary>
    /// <param name="mediaRules"></param>
    /// <param name="target">Property Name</param>
    /// <returns></returns>
    public static string MinVisibleSize(this IEnumerable<MediaRule> mediaRules, string target)
    {
        var sortedRules = mediaRules
            .Where(x => x.Rule == nameof(MediaRuleCreator.Show))
            .OrderBy(x => Array.IndexOf(MediaSizes.All, x.Size)).ToList();

        if (sortedRules.Count == 0)
            return MediaSizes.ExtraSmall;

        // Return lowest size property is visible
        foreach (var rule in sortedRules)
        {
            if (rule.ApplyTo.Contains(target))
                return rule.Size;
        }
        
        // Otherwise return the next size that wasn't defined
        var maxSizeIndex = sortedRules.Select(x => Array.IndexOf(MediaSizes.All, x.Size)).Max();
        return maxSizeIndex + 1 < MediaSizes.All.Length ? MediaSizes.All[maxSizeIndex + 1] : MediaSizes.ExtraLarge2x;
    }
}

/// <summary>
/// Size | Bootstrap | Tailwind
/// xs   | -576px    |  
/// sm   | 576px     | -640px
/// md   | 768px     | 768px
/// lg   | 992px     | 1024px
/// xl   | 1200px    | 1280px
/// xxl  | 1400px    | 1536px
/// </summary>
public static class MediaSizes
{
    public const string ExtraSmall = "xs";
    public const string Small = "sm";
    public const string Medium = "md";
    public const string Large = "lg";
    public const string ExtraLarge = "xl";
    public const string ExtraLarge2x = "2xl";

    public static string[] All = { ExtraSmall, Small, Medium, Large, ExtraLarge, ExtraLarge2x };

    public static string ForBootstrap(string size) => size switch
    {
        ExtraLarge2x => "xxl",
        _ => size,
    };

    public static string ForTailwind(string size) => size switch
    {
        ExtraSmall => Small,
        _ => size,
    };
}
