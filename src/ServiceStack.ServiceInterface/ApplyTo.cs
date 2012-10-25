﻿using System;
using System.Collections.Generic;
using ServiceStack.Common.Web;
using ServiceStack.ServiceHost;

namespace ServiceStack.ServiceInterface
{
    [Flags]
    public enum ApplyTo
    {
        None = 0,
        All = int.MaxValue,
        Get = 1 << 0,
        Post = 1 << 1,
        Put = 1 << 2,
        Delete = 1 << 3,
        Patch = 1 << 4,
        Options = 1 << 5,
        Head = 1 << 6,
        Connect = 1 << 7,
        Trace = 1 << 8,
        PropFind = 1 << 9,
        PropPatch = 1 << 10,
        MkCol = 1 << 11,
        Copy = 1 << 12,
        Move = 1 << 13,
        Lock = 1 << 14,
        UnLock = 1 << 15,
        Report = 1 << 16,
        CheckOut = 1 << 17,
        CheckIn = 1 << 18,
        UnCheckOut = 1 << 19,
        MkWorkSpace = 1 << 20,
        Update = 1 << 21,
        Label = 1 << 22,
        Merge = 1 << 23,
        MkActivity = 1 << 24,
        OrderPatch = 1 << 25,
        Acl = 1 << 26,
        Search = 1 << 27,
        VersionControl = 1 << 28,
        BaseLineControl = 1 << 29,
    }

    public static class ApplyToUtils
    {
        static ApplyToUtils()
        {
            var map = new Dictionary<string, ApplyTo>();
            foreach (var entry in ApplyToVerbs)
            {
                map[entry.Value] = entry.Key;
            }
            VerbsApplyTo = map;
        }

        public static Dictionary<string, ApplyTo> VerbsApplyTo;

        public static readonly Dictionary<ApplyTo,string> ApplyToVerbs = new Dictionary<ApplyTo, string> {
            {ApplyTo.Get, HttpMethods.Get},
            {ApplyTo.Post, HttpMethods.Post},
            {ApplyTo.Put, HttpMethods.Put},
            {ApplyTo.Delete, HttpMethods.Delete},
            {ApplyTo.Patch, HttpMethods.Patch},
            {ApplyTo.Options, HttpMethods.Options},
            {ApplyTo.Head, HttpMethods.Head},
            {ApplyTo.Connect, "CONNECT"},
            {ApplyTo.Trace, "TRACE"},
            {ApplyTo.PropFind, "PROPFIND"},
            {ApplyTo.PropPatch, "PROPPATCH"},
            {ApplyTo.MkCol, "MKCOL"},
            {ApplyTo.Copy, "COPY"},
            {ApplyTo.Move, "MOVE"},
            {ApplyTo.Lock, "LOCK"},
            {ApplyTo.UnLock, "UNLOCK"},
            {ApplyTo.Report, "REPORT"},
            {ApplyTo.CheckOut, "CHECKOUT"},
            {ApplyTo.CheckIn, "CHECKIN"},
            {ApplyTo.UnCheckOut, "UNCHECKOUT"},
            {ApplyTo.MkWorkSpace, "MKWORKSPACE"},
            {ApplyTo.Update, "UPDATE"},
            {ApplyTo.Label, "LABEL"},
            {ApplyTo.Merge, "MERGE"},
            {ApplyTo.MkActivity, "MKACTIVITY"},
            {ApplyTo.OrderPatch, "ORDERPATCH"},
            {ApplyTo.Acl, "ACL"},
            {ApplyTo.Search, "SEARCH"},
            {ApplyTo.VersionControl, "VERSION-CONTROL"},
            {ApplyTo.BaseLineControl, "BASELINE-CONTROL"},
        };

        public static ApplyTo HttpMethodAsApplyTo(this IHttpRequest req)
        {
            ApplyTo applyTo;
            return VerbsApplyTo.TryGetValue(req.HttpMethod, out applyTo)
                ? applyTo
                : ApplyTo.None;
        }
    }
}
