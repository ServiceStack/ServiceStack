using System;
using System.Text.RegularExpressions;

namespace ServiceStack.Html;

[Flags]
public enum Minify
{
    JavaScript   = 1 << 0,
    Css          = 1 << 1,
    Html         = 1 << 2,
    HtmlAdvanced = 1 << 3,
}
    
public static class Minifiers
{
    public static ICompressor JavaScript = new JSMinifierFactory();
    public static ICompressor Css = new CssMinifier();

    public static ICompressor Html = new HtmlCompressor();

    public static ICompressor HtmlAdvanced = new HtmlCompressor
    {
        JavaScriptCompressor = JavaScript,
        CompressJavaScript = true,
        CssCompressor = Css,
        CompressCss = true,
    };
}

class JSMinifierFactory : ICompressor
{
    public string Compress(string source) => new JSMinifier().Compress(source);
}

public class BasicHtmlMinifier : ICompressor
{
    static Regex BetweenScriptTagsRegEx = new Regex(@"<script[^>]*>[\w|\t|\r|\W]*?</script>", RegexOptions.Compiled);
    static Regex BetweenTagsRegex = new Regex(@"(?<=[^])\t{2,}|(?<=[>])\s{2,}(?=[<])|(?<=[>])\s{2,11}(?=[<])|(?=[\n])\s{2,}|(?=[\r])\s{2,}", RegexOptions.Compiled);
    static Regex MatchBodyRegEx = new Regex(@"</body>", RegexOptions.Compiled);

    public static string MinifyHtml(string html)
    {
        if (html == null)
            return html;

        var matches = BetweenScriptTagsRegEx.Matches(html);
        html = BetweenScriptTagsRegEx.Replace(html, string.Empty);
        html = BetweenTagsRegex.Replace(html, string.Empty);

        var str = string.Empty;
        foreach (Match match in matches)
        {
            str += match.ToString();
        }

        html = MatchBodyRegEx.Replace(html, str + "</body>");
        return html;
    }

    public string Compress(string html)
    {
        return MinifyHtml(html);
    }
}