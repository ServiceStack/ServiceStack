//
// https://github.com/ServiceStack/ServiceStack.Redis
// ServiceStack.Redis: ECMA CLI Binding to the Redis key-value storage system
//
// Authors:
//   Demis Bellot (demis.bellot@gmail.com)
//
// Copyright 2013 ServiceStack, Inc. All Rights Reserved.
//
// Licensed under the same terms of ServiceStack.
//

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Sockets;
using System.Security.Authentication;
using ServiceStack.Model;
using ServiceStack.Text;

namespace ServiceStack.Redis
{
    public static class RedisExtensions
    {
        public static List<RedisEndpoint> ToRedisEndPoints(this IEnumerable<string> hosts)
        {
            return hosts == null
                ? new List<RedisEndpoint>()
                : hosts.Select(x => ToRedisEndpoint(x)).ToList();
        }

        public static RedisEndpoint ToRedisEndpoint(this string connectionString, int? defaultPort = null)
        {
            if (connectionString == null)
                throw new ArgumentNullException("connectionString");
            if (connectionString.StartsWith("redis://"))
                connectionString = connectionString.Substring("redis://".Length);

            var domainParts = connectionString.SplitOnLast('@');
            var qsParts = domainParts.Last().SplitOnFirst('?');
            var hostParts = qsParts[0].SplitOnLast(':');
            var useDefaultPort = true;
            var port = defaultPort.GetValueOrDefault(RedisConfig.DefaultPort);
            if (hostParts.Length > 1)
            {
                port = int.Parse(hostParts[1]);
                useDefaultPort = false;
            }
            var endpoint = new RedisEndpoint(hostParts[0], port);
            if (domainParts.Length > 1)
            {
                var authParts = domainParts[0].SplitOnFirst(':');
                if (authParts.Length > 1)
                    endpoint.Client = authParts[0];

                endpoint.Password = authParts.Last();
            }

            if (qsParts.Length > 1)
            {
                var qsParams = qsParts[1].Split('&');
                foreach (var param in qsParams)
                {
                    var entry = param.Split('=');
                    var value = entry.Length > 1 ? entry[1].UrlDecode() : null;
                    if (value == null) continue;

                    var name = entry[0].ToLower();
                    switch (name)
                    {
                        case "db":
                            endpoint.Db = int.Parse(value);
                            break;
                        case "ssl":
                            endpoint.Ssl = bool.Parse(value);
                            if (useDefaultPort)
                                endpoint.Port = RedisConfig.DefaultPortSsl;
                            break;
                        case "sslprotocols":
                            SslProtocols protocols;
                            value = value?.Replace("|", ",");
                            if (!Enum.TryParse(value, true, out protocols)) throw new ArgumentOutOfRangeException("Keyword '" + name + "' requires an SslProtocol value (multiple values separated by '|').");
                            endpoint.SslProtocols = protocols;
                            break;
                        case "client":
                            endpoint.Client = value;
                            break;
                        case "password":
                            endpoint.Password = value;
                            break;
                        case "namespaceprefix":
                            endpoint.NamespacePrefix = value;
                            break;
                        case "connecttimeout":
                            endpoint.ConnectTimeout = int.Parse(value);
                            break;
                        case "sendtimeout":
                            endpoint.SendTimeout = int.Parse(value);
                            break;
                        case "receivetimeout":
                            endpoint.ReceiveTimeout = int.Parse(value);
                            break;
                        case "retrytimeout":
                            endpoint.RetryTimeout = int.Parse(value);
                            break;
                        case "idletimeout":
                        case "idletimeoutsecs":
                            endpoint.IdleTimeOutSecs = int.Parse(value);
                            break;
                    }
                }
            }

            return endpoint;
        }
    }

    internal static class RedisExtensionsInternal
    {
        public static bool IsConnected(this Socket socket)
        {
            try
            {
                return !(socket.Poll(1, SelectMode.SelectRead) && socket.Available == 0);
            }
            catch (SocketException)
            {
                return false;
            }
        }


        public static string[] GetIds(this IHasStringId[] itemsWithId)
        {
            var ids = new string[itemsWithId.Length];
            for (var i = 0; i < itemsWithId.Length; i++)
            {
                ids[i] = itemsWithId[i].Id;
            }
            return ids;
        }

        public static List<string> ToStringList(this byte[][] multiDataList)
        {
            if (multiDataList == null)
                return new List<string>();

            var results = new List<string>();
            foreach (var multiData in multiDataList)
            {
                results.Add(multiData.FromUtf8Bytes());
            }
            return results;
        }

        public static string[] ToStringArray(this byte[][] multiDataList)
        {
            if (multiDataList == null)
                return TypeConstants.EmptyStringArray;

            var to = new string[multiDataList.Length];
            for (int i = 0; i < multiDataList.Length; i++)
            {
                to[i] = multiDataList[i].FromUtf8Bytes();
            }
            return to;
        }

        public static Dictionary<string, string> ToStringDictionary(this byte[][] multiDataList)
        {
            if (multiDataList == null)
                return TypeConstants.EmptyStringDictionary;

            var map = new Dictionary<string, string>();

            for (var i = 0; i < multiDataList.Length; i += 2)
            {
                var key = multiDataList[i].FromUtf8Bytes();
                map[key] = multiDataList[i + 1].FromUtf8Bytes();
            }

            return map;
        }

        private static readonly NumberFormatInfo DoubleFormatProvider = new NumberFormatInfo
        {
            PositiveInfinitySymbol = "+inf",
            NegativeInfinitySymbol = "-inf"
        };

        public static byte[] ToFastUtf8Bytes(this double value)
        {
            return FastToUtf8Bytes(value.ToString("R", DoubleFormatProvider));
        }

        private static byte[] FastToUtf8Bytes(string strVal)
        {
            var bytes = new byte[strVal.Length];
            for (var i = 0; i < strVal.Length; i++)
                bytes[i] = (byte)strVal[i];

            return bytes;
        }

        public static byte[][] ToMultiByteArray(this string[] args)
        {
            var byteArgs = new byte[args.Length][];
            for (var i = 0; i < args.Length; ++i)
                byteArgs[i] = args[i].ToUtf8Bytes();
            return byteArgs;
        }

        public static byte[][] PrependByteArray(this byte[][] args, byte[] valueToPrepend)
        {
            var newArgs = new byte[args.Length + 1][];
            newArgs[0] = valueToPrepend;
            var i = 1;
            foreach (var arg in args)
                newArgs[i++] = arg;

            return newArgs;
        }
        public static byte[][] PrependInt(this byte[][] args, int valueToPrepend)
        {
            return args.PrependByteArray(valueToPrepend.ToUtf8Bytes());
        }
    }
}