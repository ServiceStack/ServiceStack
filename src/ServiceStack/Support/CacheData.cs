using System;

#nullable enable

namespace ServiceStack.Support;

public class DataCache
{
    private static readonly DataCache instance = new();
    public static DataCache Instance => instance;
    readonly byte[] Utf8JsonpPrefix = { (byte) '(' };
    readonly byte[] Utf8JsonpSuffix = { (byte) ')' };

    public static byte[] JsonpPrefix => instance.Utf8JsonpPrefix;
    public static byte[] JsonpSuffix => instance.Utf8JsonpSuffix;

    public static byte[] CreateJsonpPrefix(string callback)
    {
        try
        {
            var to = new byte[callback.Length + 1];
            for (var i = 0; i < callback.Length; i++)
            {
                to[i] = (byte)callback[i];
            }
            to[to.Length - 1] = (byte)'(';
            return to;
        }
        catch
        {
            return (callback + "(").ToUtf8Bytes();
        }
    }
}