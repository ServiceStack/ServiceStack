//Copyright (c) Service Stack LLC. All Rights Reserved.
//License: https://raw.github.com/ServiceStack/ServiceStack/master/license.txt

using System.Text;
using ServiceStack.Utils;

namespace ServiceStack
{
    public static class PathExtensions
    {
        public static string CombineWith(this string path, params string[] thesePaths)
        {
            if (thesePaths.Length == 1 && thesePaths[0] == null) return path;
            var startPath = path.Length > 1 ? path.TrimEnd('/', '\\') : path;
            return PathUtils.CombinePaths(new StringBuilder(startPath), thesePaths);
        }

        public static string CombineWith(this string path, params object[] thesePaths)
        {
            if (thesePaths.Length == 1 && thesePaths[0] == null) return path;
            return PathUtils.CombinePaths(new StringBuilder(path.TrimEnd('/', '\\')),
                thesePaths.SafeConvertAll(x => x.ToString()).ToArray());
        }
    }
}