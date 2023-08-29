using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ServiceStack.Text;

namespace ServiceStack;

public static class SiteUtils
{
    private static char[] UrlCharChecks = {':', '/', '('};
        
    /// <summary>
    /// Allow slugs to capture URLs, Examples:
    /// techstacks.io                  => https://techstacks.io
    /// http:techstacks.io             => http://techstacks.io
    /// techstacks.io:1000             => https://techstacks.io:1000
    /// techstacks.io:1000:site1:site2 => https://techstacks.io:1000/site1/site2
    /// techstacks.io:site1%7Csite2    => https://techstacks.io/site1|site2
    /// techstacks.io:1000:site1:site2(a:1,b:"c,d",e:f) => https://techstacks.io:1000/site1/site2(a:1,b:"c,d",e:f)
    /// </summary>
    public static string UrlFromSlug(string slug)
    {
        var url = slug;
        var isUrl = url.StartsWith("https://") || url.StartsWith("http://");
        var scheme = !isUrl && (url.StartsWith("http:") || url.StartsWith("https:"))
            ? url.LeftPart(':')
            : null;
        if (scheme != null)
            url = url.RightPart(':');
            
        var firstPos = url.IndexOf(':');
        if (!isUrl && firstPos >= 0)
        {
            var isColonPos = url.IndexOfAny(UrlCharChecks);
            if (isColonPos >= 0 && url[isColonPos] == ':')
            {
                var atPort = url.RightPart(':');
                if (atPort.Length > 0)
                {
                    var delim1Pos = atPort.IndexOf(':');
                    var delim2Pos = atPort.IndexOf('/');
                    var endPos = delim1Pos >= 0 && delim2Pos >= 0
                        ? Math.Min(delim1Pos, delim2Pos)
                        : Math.Max(delim1Pos, delim2Pos);
                    var testPort = endPos >= 0
                        ? atPort.Substring(0,endPos)
                        : atPort.Substring(0,atPort.Length - 1);
                    url = int.TryParse(testPort, out _)
                        ? url.LeftPart(':') + ':' + UnSlash(atPort)
                        : url.LeftPart(':') + '/' + UnSlash(atPort);
                }
                else
                {
                    url = url.LeftPart(':') + '/' + UnSlash(atPort);
                }
            }
        }
        url = url.UrlDecode();
        if (!isUrl)
        {
            url = scheme != null
                ? scheme + "://" + url
                : "https://" + url;
        }
        return url;
    }

    private static string UnSlash(string urlComponent)
    {
        // don't replace ':' after '('...)
        if (urlComponent.IndexOf('(') >= 0)
        {
            var target = urlComponent.LeftPart('(');
            var suffix = urlComponent.RightPart('(');
            return target.Replace(':', '/') + '(' + suffix;
        }
        return urlComponent.Replace(':', '/');
    }

    /// <summary>
    /// Convert URL to URL-friendly slugs, Examples:
    /// https://techstacks.io                  => techstacks.io 
    /// http://techstacks.io                   => http:techstacks.io 
    /// https://techstacks.io:1000             => techstacks.io:1000 
    /// https://techstacks.io:1000/site1/site2 => techstacks.io:1000:site1:site2 
    /// https://techstacks.io/site1|site2      => techstacks.io:site|site2
    /// https://techstacks.io:1000/site1/site2(a:1,b:"c,d",e:f) => techstacks.io:1000:site1:site2(a:1,b:"c,d",e:f)
    /// </summary>
    public static string UrlToSlug(string url)
    {
        var slug = url;
        if (slug.StartsWith("https://"))
            slug = slug.Substring("https://".Length);
        else if (slug.StartsWith("http://"))
            slug = "http:" + slug.Substring("http://".Length);
        slug = slug.Replace('/', ':');
        return slug;
    }
        
    public static string ToUrlEncoded(List<string> args)
    {
        if (!args.IsEmpty())
        {
            if (args.Count % 2 != 0)
                throw new ArgumentException(@"Invalid odd number of arguments, expected [key1,value1,key2,value2,...]", nameof(args));

            var sb = StringBuilderCache.Allocate();
            for (var i = 0; i < args.Count; i += 2)
            {
                if (sb.Length > 0)
                    sb.Append('&');
                    
                var key = args[i];
                var val = args[i + 1];
                val = val?.Replace((char)31, ','); // 31 1F US (unit separator) 
                sb.Append(key).Append('=').Append(val.UrlEncode());
            }
            return StringBuilderCache.ReturnAndFree(sb);
        }
        return string.Empty;
    }
}