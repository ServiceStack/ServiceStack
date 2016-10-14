// Copyright (c) Service Stack LLC. All Rights Reserved.
// License: https://raw.github.com/ServiceStack/ServiceStack/master/license.txt


using System;
using System.Collections.Generic;
#if NETSTANDARD1_3
using System.Threading.Tasks;
#endif

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

        public static List<IAsyncResult> TimesAsync(this int times, Action<int> actionFn)
        {
            var asyncResults = new List<IAsyncResult>(times);
            for (var i = 0; i < times; i++)
            {
#if NETSTANDARD1_3
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
#if NETSTANDARD1_3
                asyncResults.Add(Task.Run(actionFn));
#else                
                asyncResults.Add(actionFn.BeginInvoke(null, null));
#endif
            }
            return asyncResults;
        }
    }
}