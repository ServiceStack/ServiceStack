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
using System.Linq;
using System.Text;
using ServiceStack.Caching;
using ServiceStack.Text;

namespace ServiceStack.Redis
{
    public partial class RedisClient
        : ICacheClient
    {
        public T Exec<T>(Func<RedisClient, T> action)
        {
            using (JsConfig.With(new Text.Config { ExcludeTypeInfo = false }))
            {
                return action(this);
            }
        }

        public void Exec(Action<RedisClient> action)
        {
            using (JsConfig.With(new Text.Config { ExcludeTypeInfo = false }))
            {
                action(this);
            }
        }

        public void RemoveAll(IEnumerable<string> keys)
        {
            Exec(r => r.RemoveEntry(keys.ToArray()));
        }

        public T Get<T>(string key)
        {
            return Exec(r =>
                typeof(T) == typeof(byte[])
                    ? (T)(object)r.Get(key)
                    : JsonSerializer.DeserializeFromString<T>(r.GetValue(key))
            );
        }

        //Looking up Dictionary<Type,bool> for type is faster than HashSet<Type>.
        private static readonly Dictionary<Type, bool> numericTypes = new Dictionary<Type, bool> {
            { typeof(byte), true},
            { typeof(sbyte), true},
            { typeof(short), true},
            { typeof(ushort), true},
            { typeof(int), true},
            { typeof(uint), true},
            { typeof(long), true},
            { typeof(ulong), true},
	        { typeof(double), true},
	        { typeof(float), true},
	        { typeof(decimal), true}
        };

        private static byte[] ToBytes<T>(T value)
        {
            var bytesValue = value as byte[];
            if (bytesValue == null && (numericTypes.ContainsKey(typeof(T)) || !Equals(value, default(T))))
                bytesValue = value.ToJson().ToUtf8Bytes();
            return bytesValue;
        }

        public long Increment(string key, uint amount)
        {
            return Exec(r => r.IncrementValueBy(key, (int)amount));
        }

        public long Decrement(string key, uint amount)
        {
            return Exec(r => DecrementValueBy(key, (int)amount));
        }

        public bool Add<T>(string key, T value)
        {
            return Exec(r => r.Set(key, ToBytes(value), exists: false));
        }

        public bool Set<T>(string key, T value)
        {
            Exec(r => ((RedisNativeClient)r).Set(key, ToBytes(value)));
            return true;
        }

        public bool Replace<T>(string key, T value)
        {
            return Exec(r => r.Set(key, ToBytes(value), exists: true));
        }

        public bool Add<T>(string key, T value, DateTime expiresAt)
        {
            AssertNotInTransaction();

            return Exec(r =>
            {
                if (r.Add(key, value))
                {
                    r.ExpireEntryAt(key, ConvertToServerDate(expiresAt));
                    return true;
                }
                return false;
            });
        }

        public bool Add<T>(string key, T value, TimeSpan expiresIn)
        {
            return Exec(r => r.Set(key, ToBytes(value), exists: false, expiryMs: (long)expiresIn.TotalMilliseconds));
        }

        public bool Set<T>(string key, T value, TimeSpan expiresIn)
        {
            if (AssertServerVersionNumber() >= 2600)
            {
                Exec(r => r.Set(key, ToBytes(value), 0, expiryMs: (long)expiresIn.TotalMilliseconds));
            }
            else
            {
                Exec(r => r.SetEx(key, (int)expiresIn.TotalSeconds, ToBytes(value)));
            }
            return true;
        }

        public bool Set<T>(string key, T value, DateTime expiresAt)
        {
            AssertNotInTransaction();

            Exec(r =>
            {
                Set(key, value);
                ExpireEntryAt(key, ConvertToServerDate(expiresAt));
            });
            return true;
        }

        public bool Replace<T>(string key, T value, DateTime expiresAt)
        {
            AssertNotInTransaction();

            return Exec(r =>
            {
                if (r.Replace(key, value))
                {
                    r.ExpireEntryAt(key, ConvertToServerDate(expiresAt));
                    return true;
                }
                return false;
            });
        }

        public bool Replace<T>(string key, T value, TimeSpan expiresIn)
        {
            return Exec(r => r.Set(key, ToBytes(value), exists: true, expiryMs: (long)expiresIn.TotalMilliseconds));
        }

        public IDictionary<string, T> GetAll<T>(IEnumerable<string> keys)
        {
            return Exec(r =>
            {
                var keysArray = keys.ToArray();
                var keyValues = r.MGet(keysArray);

                return ProcessGetAllResult<T>(keysArray, keyValues);
            });
        }

        private static IDictionary<string, T> ProcessGetAllResult<T>(string[] keysArray, byte[][] keyValues)
        {
            var results = new Dictionary<string, T>();
            var isBytes = typeof(T) == typeof(byte[]);

            var i = 0;
            foreach (var keyValue in keyValues)
            {
                var key = keysArray[i++];

                if (keyValue == null)
                {
                    results[key] = default(T);
                    continue;
                }

                if (isBytes)
                {
                    results[key] = (T)(object)keyValue;
                }
                else
                {
                    var keyValueString = Encoding.UTF8.GetString(keyValue);
                    results[key] = JsonSerializer.DeserializeFromString<T>(keyValueString);
                }
            }
            return results;
        }

        public void SetAll<T>(IDictionary<string, T> values)
        {
            if (values.Count != 0)
            {
                Exec(r =>
                {
                    // need to do this inside Exec for the JSON config bits
                    GetSetAllBytesTyped<T>(values, out var keys, out var valBytes);
                    r.MSet(keys, valBytes);
                });
            }
        }

        private static void GetSetAllBytesTyped<T>(IDictionary<string, T> values, out string[] keys, out byte[][] valBytes)
        {
            keys = values.Keys.ToArray();
            valBytes = new byte[values.Count][];
            var isBytes = typeof(T) == typeof(byte[]);

            var i = 0;
            foreach (var value in values.Values)
            {
                if (!isBytes)
                {
                    var t = JsonSerializer.SerializeToString(value);
                    if (t != null)
                        valBytes[i] = t.ToUtf8Bytes();
                    else
                        valBytes[i] = new byte[] { };
                }
                else
                    valBytes[i] = (byte[])(object)value ?? new byte[] { };
                i++;
            }
        }
    }


}