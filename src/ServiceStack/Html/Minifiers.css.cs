using System;
using System.Text.RegularExpressions;

namespace ServiceStack.Html
{
    //http://madskristensen.net/post/efficient-stylesheet-minification-in-c
    public class CssMinifier : ICompressor
    {
        public static string MinifyCss(string css)
        {
            css = Regex.Replace(css, @"[a-zA-Z]+#", "#");
            css = Regex.Replace(css, @"[\n\r]+\s*", String.Empty);
            css = Regex.Replace(css, @"\s+", " ");
            css = Regex.Replace(css, @"\s?([:,;{}])\s?", "$1");
            css = css.Replace(";}", "}");
            css = Regex.Replace(css, @"([\s:]0)(px|pt|%|em)", "$1");

            // Remove comments from CSS
            css = Regex.Replace(css, @"/\[\d\D]?\*/", String.Empty);

            return css;
        }

        public string Compress(string source)
        {
            return MinifyCss(source);
        }
    }
}