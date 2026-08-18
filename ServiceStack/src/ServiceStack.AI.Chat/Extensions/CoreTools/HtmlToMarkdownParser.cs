using System.Net;
using System.Text;
using System.Text.RegularExpressions;

namespace ServiceStack.AI;

/// <summary>
/// Lightweight, zero-dependency HTML to Markdown parser.
/// Converts standard HTML elements (headings, paragraphs, lists, links, emphasis, code blocks, tables)
/// into clean, token-efficient Markdown while stripping scripts, styles, and layout boilerplate.
/// </summary>
public class HtmlToMarkdownParser
{
    private readonly string baseUrl;
    private static readonly HashSet<string> SkipTags = new(StringComparer.OrdinalIgnoreCase)
    {
        "script", "style", "head", "svg", "noscript", "iframe", "canvas", "template", "nav", "footer", "aside"
    };

    public HtmlToMarkdownParser(string baseUrl = "")
    {
        this.baseUrl = baseUrl;
    }

    public string Parse(string html)
    {
        if (string.IsNullOrWhiteSpace(html))
            return "";

        var sb = new StringBuilder();
        int skipDepth = 0;
        bool inPre = false;
        string? currentHref = null;
        var linkText = new StringBuilder();

        var len = html.Length;
        int i = 0;

        while (i < len)
        {
            if (html[i] == '<')
            {
                // HTML Comments <!-- ... -->
                if (i + 3 < len && html[i + 1] == '!' && html[i + 2] == '-' && html[i + 3] == '-')
                {
                    int commentEnd = html.IndexOf("-->", i + 4, StringComparison.Ordinal);
                    i = commentEnd >= 0 ? commentEnd + 3 : len;
                    continue;
                }

                // CDATA <![CDATA[ ... ]]>
                if (i + 8 < len && html.Substring(i, 9).Equals("<![CDATA[", StringComparison.OrdinalIgnoreCase))
                {
                    int cdataEnd = html.IndexOf("]]>", i + 9, StringComparison.Ordinal);
                    if (cdataEnd >= 0)
                    {
                        var cdataText = html.Substring(i + 9, cdataEnd - (i + 9));
                        if (skipDepth == 0)
                        {
                            if (currentHref != null)
                                linkText.Append(cdataText);
                            else
                                sb.Append(cdataText);
                        }
                        i = cdataEnd + 3;
                        continue;
                    }
                }

                // Find end of tag
                int tagEnd = html.IndexOf('>', i);
                if (tagEnd < 0)
                {
                    i++;
                    continue;
                }

                var tagContent = html.Substring(i + 1, tagEnd - (i + 1)).Trim();
                i = tagEnd + 1;

                if (tagContent.Length == 0)
                    continue;

                bool isClosing = tagContent.StartsWith('/');
                if (isClosing)
                    tagContent = tagContent.Substring(1).TrimStart();

                bool isSelfClosing = tagContent.EndsWith('/');
                if (isSelfClosing)
                    tagContent = tagContent.Substring(0, tagContent.Length - 1).TrimEnd();

                // Extract tag name and attribute string
                string tagName;
                string attributesString = "";
                int firstSpace = tagContent.IndexOfAny([' ', '\t', '\r', '\n']);
                if (firstSpace >= 0)
                {
                    tagName = tagContent.Substring(0, firstSpace);
                    attributesString = tagContent.Substring(firstSpace + 1);
                }
                else
                {
                    tagName = tagContent;
                }

                if (SkipTags.Contains(tagName))
                {
                    if (isClosing)
                        skipDepth = Math.Max(0, skipDepth - 1);
                    else if (!isSelfClosing)
                        skipDepth++;
                    continue;
                }

                if (skipDepth > 0)
                    continue;

                var lowerTagName = tagName.ToLowerInvariant();
                var attrs = ParseAttributes(attributesString);

                if (isClosing)
                {
                    switch (lowerTagName)
                    {
                        case "pre":
                            inPre = false;
                            sb.Append("\n```\n");
                            break;
                        case "code" when !inPre:
                            sb.Append('`');
                            break;
                        case "b" or "strong":
                            sb.Append("**");
                            break;
                        case "i" or "em":
                            sb.Append('*');
                            break;
                        case "a":
                            var text = linkText.ToString().Trim();
                            if (text.Length > 0 && currentHref != null)
                            {
                                var fullHref = ResolveUrl(baseUrl, currentHref);
                                sb.Append($"[{text}]({fullHref})");
                            }
                            else if (text.Length > 0)
                            {
                                sb.Append(text);
                            }
                            currentHref = null;
                            linkText.Clear();
                            break;
                        case "td" or "th":
                            sb.Append(" | ");
                            break;
                        case "tr":
                            sb.Append('\n');
                            break;
                    }
                }
                else
                {
                    switch (lowerTagName)
                    {
                        case "h1": sb.Append("\n\n# "); break;
                        case "h2": sb.Append("\n\n## "); break;
                        case "h3": sb.Append("\n\n### "); break;
                        case "h4": sb.Append("\n\n#### "); break;
                        case "h5": sb.Append("\n\n##### "); break;
                        case "h6": sb.Append("\n\n###### "); break;
                        case "p" or "div" or "section" or "article":
                            sb.Append("\n\n");
                            break;
                        case "blockquote":
                            sb.Append("\n\n> ");
                            break;
                        case "br":
                            sb.Append('\n');
                            break;
                        case "hr":
                            sb.Append("\n\n---\n\n");
                            break;
                        case "li":
                            sb.Append("\n- ");
                            break;
                        case "pre":
                            inPre = true;
                            sb.Append("\n```\n");
                            break;
                        case "code" when !inPre:
                            sb.Append('`');
                            break;
                        case "b" or "strong":
                            sb.Append("**");
                            break;
                        case "i" or "em":
                            sb.Append('*');
                            break;
                        case "a":
                            if (attrs.TryGetValue("href", out var href))
                                currentHref = href;
                            linkText.Clear();
                            break;
                        case "img":
                            var alt = attrs.GetValueOrDefault("alt", "");
                            if (attrs.TryGetValue("src", out var src) && !string.IsNullOrEmpty(src))
                            {
                                var fullSrc = ResolveUrl(baseUrl, src);
                                sb.Append($"![{alt}]({fullSrc})");
                            }
                            break;
                        case "tr":
                            sb.Append("\n| ");
                            break;
                    }

                    if (isSelfClosing && lowerTagName == "a")
                    {
                        currentHref = null;
                        linkText.Clear();
                    }
                }
            }
            else
            {
                // Text node
                int nextTag = html.IndexOf('<', i);
                string textSegment;
                if (nextTag >= 0)
                {
                    textSegment = html.Substring(i, nextTag - i);
                    i = nextTag;
                }
                else
                {
                    textSegment = html.Substring(i);
                    i = len;
                }

                if (skipDepth > 0)
                    continue;

                var decoded = WebUtility.HtmlDecode(textSegment);
                if (currentHref != null)
                {
                    linkText.Append(decoded);
                }
                else
                {
                    if (inPre)
                    {
                        sb.Append(decoded);
                    }
                    else
                    {
                        var normalized = Regex.Replace(decoded, @"[ \t]+", " ");
                        sb.Append(normalized);
                    }
                }
            }
        }

        var resultText = sb.ToString();
        resultText = Regex.Replace(resultText, @"\n{3,}", "\n\n");
        return resultText.Trim();
    }

    private static Dictionary<string, string> ParseAttributes(string attrString)
    {
        var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (string.IsNullOrWhiteSpace(attrString))
            return dict;

        var matches = Regex.Matches(attrString, @"(?<name>[a-zA-Z0-9_\-:]+)(?:\s*=\s*(?:""(?<val>[^""]*)""|'(?<val>[^']*)'|(?<val>[^\s>]+)))?");
        foreach (Match m in matches)
        {
            var name = m.Groups["name"].Value;
            var val = m.Groups["val"].Success ? m.Groups["val"].Value : "";
            dict[name] = val;
        }
        return dict;
    }

    private static string ResolveUrl(string baseUrl, string relUrl)
    {
        if (string.IsNullOrEmpty(baseUrl) || string.IsNullOrEmpty(relUrl))
            return relUrl;
        if (relUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
            relUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            return relUrl;
        if (relUrl.StartsWith("//"))
            return "https:" + relUrl;
        if (Uri.TryCreate(baseUrl, UriKind.Absolute, out var baseUri) && Uri.TryCreate(baseUri, relUrl, out var fullUri))
            return fullUri.ToString();
        return relUrl;
    }
}
