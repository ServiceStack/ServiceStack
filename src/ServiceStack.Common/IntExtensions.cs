// Copyright (c) ServiceStack, Inc. All Rights Reserved.
// License: https://raw.github.com/ServiceStack/ServiceStack/master/license.txt


using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ServiceStack
{
    public static class IntExtensions
    {
        public static IEnumerable<int> Times(this int times)
        {
            for (var i = 0; i < times; i++)
            {
                yield return i;
            }
        }

        public static void Times(this int times, Action<int> actionFn)
        {
            for (var i = 0; i < times; i++)
            {
                actionFn(i);
            }
        }

        public static void Times(this int times, Action actionFn)
        {
            for (var i = 0; i < times; i++)
            {
                actionFn();
            }
        }

        public static List<T> Times<T>(this int times, Func<T> actionFn)
        {
            var list = new List<T>();
            for (var i = 0; i < times; i++)
            {
                list.Add(actionFn());
            }
            return list;
        }

        public static List<T> Times<T>(this int times, Func<int, T> actionFn)
        {
            var list = new List<T>();
            for (var i = 0; i < times; i++)
            {
                list.Add(actionFn(i));
            }
            return list;
        }

        public static async Task TimesAsync(this int times, Func<int,Task> actionFn, CancellationToken token=default)
        {
            for (var i = 0; i < times; i++)
            {
                token.ThrowIfCancellationRequested();
                await actionFn(i);
            }
        }

        public static async Task<List<T>> TimesAsync<T>(this int times, Func<int,Task<T>> actionFn, CancellationToken token=default)
        {
            var list = new List<T>();
            for (var i = 0; i < times; i++)
            {
                token.ThrowIfCancellationRequested();
                list.Add(await actionFn(i));
            }
            return list;
        }

        public static List<IAsyncResult> TimesAsync(this int times, Action<int> actionFn)
        {
            var asyncResults = new List<IAsyncResult>(times);
            for (var i = 0; i < times; i++)
            {
#if NETCORE
                asyncResults.Add(Task.Run(() => actionFn(i)));
#else                
                asyncResults.Add(actionFn.BeginInvoke(i, null, null));
#endif
            }
            return asyncResults;
        }

        public static List<IAsyncResult> TimesAsync(this int times, Action actionFn)
        {
            var asyncResults = new List<IAsyncResult>(times);
            for (var i = 0; i < times; i++)
            {
#if NETCORE
                asyncResults.Add(Task.Run(actionFn));
#else                
                asyncResults.Add(actionFn.BeginInvoke(null, null));
#endif
            }
            return asyncResults;
        }
    }
}