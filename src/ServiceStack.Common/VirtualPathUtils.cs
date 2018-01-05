﻿// Copyright (c) ServiceStack, Inc. All Rights Reserved.
// License: https://raw.github.com/ServiceStack/ServiceStack/master/license.txt


using System;
using System.Collections.Generic;
using System.Linq;
using ServiceStack.IO;

namespace ServiceStack
{
    public static class VirtualPathUtils
    {
        public static Stack<string> TokenizeVirtualPath(this string str, IVirtualPathProvider pathProvider)
        {
            if (pathProvider == null)
                throw new ArgumentNullException(nameof(pathProvider));

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
            var tokens = str.Split(new[] { pathSeparator }, n);

            return new Stack<string>(tokens.Reverse());
        }

        public static IEnumerable<IGrouping<string, string[]>> GroupByFirstToken(this IEnumerable<string> resourceNames, char pathSeparator = '.')
        {
            return resourceNames.Select(n => n.Split(new[] { pathSeparator }, 2))
                .GroupBy(t => t[0]);
        }

        public static byte[] ReadAllBytes(this IVirtualFile file)
        {
            using (var stream = file.OpenRead())
            {
                var bytes = stream.ReadFully();
                return bytes;
            }
        }

        public static bool Exists(this IVirtualNode node)
        {
            return node != null;
        }

        public static bool IsFile(this IVirtualNode node)
        {
            return node is IVirtualFile;
        }

        public static bool IsDirectory(this IVirtualNode node)
        {
            return node is IVirtualDirectory;
        }

        public static IVirtualNode GetVirtualNode(this IVirtualPathProvider pathProvider, string virtualPath)
        {
            return (IVirtualNode)pathProvider.GetFile(virtualPath)
                ?? pathProvider.GetDirectory(virtualPath);
        }

        public static IVirtualFile GetDefaultDocument(this IVirtualDirectory dir, List<string> defaultDocuments)
        {
            foreach (var defaultDoc in defaultDocuments)
            {
                var defaultFile = dir.GetFile(defaultDoc);
                if (defaultFile == null) continue;

                return defaultFile;
            }

            return null;
        }

        public static TimeSpan MaxRetryOnExceptionTimeout { get; } = TimeSpan.FromSeconds(10);

        internal static void SleepBackOffMultiplier(this int i)
        {
            var nextTryMs = (2 ^ i) * 50;
#if NETSTANDARD2_0
            System.Threading.Tasks.Task.Delay(nextTryMs).Wait();
#elif NET45
            System.Threading.Thread.Sleep(nextTryMs);
#endif
        }
    }
}