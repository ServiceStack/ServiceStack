#if NETCORE
using System;
using System.Collections.Specialized;
using Microsoft.AspNetCore.WebUtilities;

namespace ServiceStack.WebHost.Endpoints.Tests;

public class HttpUtility
{
    public static NameValueCollection ParseQueryString(string query)
    {
        NameValueCollection result = [];

        var queryDict = QueryHelpers.ParseQuery(query);

        foreach(var key in queryDict.Keys)
        {
            result.Add(key, String.Join("; ", queryDict[key]));
        }

        return result;
    }
}
#endif