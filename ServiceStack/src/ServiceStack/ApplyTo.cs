using System;
using System.Collections.Generic;
using ServiceStack.Web;

namespace ServiceStack
{
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

        public static readonly Dictionary<ApplyTo, string> ApplyToVerbs = new Dictionary<ApplyTo, string> {
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

        public static ApplyTo HttpMethodAsApplyTo(this IRequest req)
        {
            return VerbsApplyTo.TryGetValue(req.Verb, out var applyTo)
                ? applyTo
                : ApplyTo.None;
        }
    }
}
