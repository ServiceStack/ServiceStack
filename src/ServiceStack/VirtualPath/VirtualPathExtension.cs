using System;
using System.Collections.Generic;
using System.Linq;
using ServiceStack.IO;

namespace ServiceStack.VirtualPath
{
    public static class VirtualPathExtension
    {
        public static Stack<string> TokenizeVirtualPath(this string str, IVirtualPathProvider pathProvider)
        {
            if (pathProvider == null)
                throw new ArgumentNullException("pathProvider");

            return TokenizeVirtualPath(str, pathProvider.VirtualPathSeparator);
        }

        public static Stack<string> TokenizeVirtualPath(this string str, string virtualPathSeparator)
        {
            if (string.IsNullOrEmpty(str))
                return new Stack<string>();

            var tokens = str.Split(new[] { virtualPathSeparator }, StringSplitOptions.RemoveEmptyEntries);
            return new Stack<string>(tokens.Reverse());
        }


        public static Stack<string> TokenizeResourcePath(this string str, char pathSeparator = '.')
        {
            if (string.IsNullOrEmpty(str))
                return new Stack<string>();

            var n = str.Count(c => c == pathSeparator);
            var tokens = str.Split(new [] { pathSeparator }, n);

            return new Stack<string>(tokens.Reverse());
        }

        public static IEnumerable<IGrouping<string, string[]>> GroupByFirstToken(this IEnumerable<string> resourceNames, char pathSeparator = '.')
        {
            return resourceNames.Select(n => n.Split(new[] { pathSeparator }, 2))
                .GroupBy(t => t[0]);
        }
    }
}
