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
using ServiceStack.Text;

namespace ServiceStack.Redis
{
    public partial class RedisClient
        : IRedisClient
    {
        public IEnumerable<SlowlogItem> GetSlowlog(int? numberOfRecords = null)
        {
            var data = Slowlog(numberOfRecords);
            return ParseSlowlog(data);
        }

        private static SlowlogItem[] ParseSlowlog(object[] data)
        {
            var list = new SlowlogItem[data.Length];
            for (int i = 0; i < data.Length; i++)
            {
                var log = (object[])data[i];

                var arguments = ((object[])log[3]).OfType<byte[]>()
                    .Select(t => t.FromUtf8Bytes())
                    .ToArray();


                list[i] = new SlowlogItem(
                    Int32.Parse((string)log[0], CultureInfo.InvariantCulture),
                    DateTimeExtensions.FromUnixTime(Int32.Parse((string)log[1], CultureInfo.InvariantCulture)),
                    Int32.Parse((string)log[2], CultureInfo.InvariantCulture),
                    arguments
                    );
            }

            return list;
        }


    }
}
