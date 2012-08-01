using System;
using System.Collections.Generic;
using System.Linq;

namespace ServiceStack.Razor.VirtualPath
{
    public static class VirtualPathExtension
    {
        public static Stack<String> TokenizeVirtualPath(this String str, IVirtualPathProvider pathProvider)
        {
            if (pathProvider == null)
                throw new ArgumentNullException("pathProvider");

            return TokenizeVirtualPath(str, pathProvider.VirtualPathSeparator);
        }

        public static Stack<String> TokenizeVirtualPath(this String str, String virtualPathSeparator)
        {
            if (String.IsNullOrEmpty(str))
                return new Stack<string>();

            var tokens = str.Split(new[] { virtualPathSeparator }, StringSplitOptions.RemoveEmptyEntries);
            return new Stack<string>(tokens.Reverse());
        }


        public static Stack<String> TokenizeResourcePath(this String str, char pathSeparator = '.')
        {
            if (String.IsNullOrEmpty(str))
                return new Stack<string>();

            var n = str.Count(c => c == pathSeparator);
            var tokens = str.Split(new [] { pathSeparator }, n);

            return new Stack<string>(tokens.Reverse());
        }

        public static IEnumerable<IGrouping<String, String[]>> GroupByFirstToken(this IEnumerable<string> resourceNames, char pathSeparator = '.')
        {
            return resourceNames.Select(n => n.Split(new[] { pathSeparator }, 2))
                                .GroupBy(t => t[0]);
        }
    }
}
