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
using System.Text;
using ServiceStack.Text;

namespace ServiceStack.Redis
{
    public partial class RedisClient
        : IRedisClient
    {
        public void SetConfig(string configItem, string value)
        {
            base.ConfigSet(configItem, value.ToUtf8Bytes());
        }

        public RedisText GetServerRoleInfo()
        {
            return base.Role();
        }

        public string GetConfig(string configItem)
        {
            var byteArray = base.ConfigGet(configItem);
            return GetConfigParse(byteArray);
        }

        static string GetConfigParse(byte[][] byteArray)
        {
            var sb = StringBuilderCache.Allocate();
            const int startAt = 1; //skip repeating config name
            for (var i = startAt; i < byteArray.Length; i++)
            {
                var bytes = byteArray[i];
                if (sb.Length > 0)
                    sb.Append(" ");

                sb.Append(bytes.FromUtf8Bytes());
            }
            return StringBuilderCache.ReturnAndFree(sb);
        }

        public void SaveConfig()
        {
            base.ConfigRewrite();
        }

        public void ResetInfoStats()
        {
            base.ConfigResetStat();
        }

        public string GetClient()
        {
            return base.ClientGetName();
        }

        public void SetClient(string name)
        {
            base.ClientSetName(name);
        }

        public void KillClient(string address)
        {
            base.ClientKill(address);
        }

        public long KillClients(string fromAddress = null, string withId = null, RedisClientType? ofType = null, bool? skipMe = null)
        {
            var typeString = ofType != null ? ofType.ToString().ToLower() : null;
            var skipMeString = skipMe != null ? (skipMe.Value ? "yes" : "no") : null;
            return base.ClientKill(addr: fromAddress, id: withId, type: typeString, skipMe: skipMeString);
        }

        public List<Dictionary<string, string>> GetClientsInfo()
        {
            return GetClientsInfoParse(ClientList());
        }
        private static List<Dictionary<string, string>> GetClientsInfoParse(byte[] rawResult)
        {
            var clientList = rawResult.FromUtf8Bytes();
            var results = new List<Dictionary<string, string>>();

            var lines = clientList.Split('\n');
            foreach (var line in lines)
            {
                if (String.IsNullOrEmpty(line)) continue;

                var map = new Dictionary<string, string>();
                var parts = line.Split(' ');
                foreach (var part in parts)
                {
                    var keyValue = part.SplitOnFirst('=');
                    map[keyValue[0]] = keyValue[1];
                }
                results.Add(map);
            }
            return results;
        }

        public void PauseAllClients(TimeSpan duration)
        {
            base.ClientPause((int)duration.TotalMilliseconds);
        }

        public DateTime GetServerTime()
        {
            var parts = base.Time();
            return ParseTimeResult(parts);
        }

        private static DateTime ParseTimeResult(byte[][] parts)
        {
            var unixTime = long.Parse(parts[0].FromUtf8Bytes());
            var microSecs = long.Parse(parts[1].FromUtf8Bytes());
            var ticks = microSecs / 1000 * TimeSpan.TicksPerMillisecond;

            var date = unixTime.FromUnixTime();
            var timeSpan = TimeSpan.FromTicks(ticks);
            return date + timeSpan;
        }
    }
}
