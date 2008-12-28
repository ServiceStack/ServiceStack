using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using ServiceStack.Common.Services.Cache;

namespace ServiceStack.CacheProvider
{
	public class CacheClient
	{
		private readonly BinaryFormatter formatter = new BinaryFormatter();
		private readonly string[] urls;
		private readonly Dictionary<string, Queue<ICacheProvider>> pool = new Dictionary<string, Queue<ICacheProvider>>();

		public CacheClient(params string[] urls)
		{
			this.urls = urls;
			foreach (var url in urls)
			{
				this.pool[url] = new Queue<ICacheProvider>();
			}
		}

		public bool Add(string key, object value)
		{
			return ClientFunc(key, client => {
			                       	SerializationOptions options;
			                       	var bytes = Serialize(value, out options);
			                       	var result = client.Add(key, (long)options, null, bytes);
			                       	return result == CacheOperationResult.Stored;
			                       });
		}

		private byte[] Serialize(object value, out SerializationOptions options)
		{
			if (value is string)
			{
				options = SerializationOptions.String;
				return Encoding.UTF8.GetBytes((string)value);
			}
			if (value is byte[])
			{
				options = SerializationOptions.None;
				return (byte[])value;
			}
			if (value is int)
			{
				options = SerializationOptions.Integer;
				return BitConverter.GetBytes((int)value);
			}
			if (value is long)
			{
				options = SerializationOptions.Long;
				return BitConverter.GetBytes((long)value);
			}
			var stream = new MemoryStream();
			this.formatter.Serialize(stream, value);
			options = SerializationOptions.Serialized;
			return stream.ToArray();
		}

		private string GetUrl(string key)
		{
			int index = key.GetHashCode() % this.urls.Length;
			return this.urls[index];
		}

		public bool Set(string key, object value)
		{
			return ClientFunc(key, client => {
			                       	SerializationOptions options;
			                       	var bytes = Serialize(value, out options);
			                       	var result = client.Set(key, (long)options, null, bytes);
			                       	return result == CacheOperationResult.Stored;
			                       });
		}

		public bool Append(string key, object value)
		{
			return ClientFunc(key, client => {
			                       	SerializationOptions options;
			                       	var result = client.Append(key, Serialize(value, out options));
			                       	return result == CacheOperationResult.Stored;
			                       });
		}

		public object Get(string key)
		{
			var objects = Get(new[] { key });
			return objects.Length > 0 ? objects[0].Value : null;
		}

		public T Get<T>(string key)
		{
			return (T)Get(key);
		}


		public KeyValuePair<string, object>[] Get(params string[] keys)
		{
			var keysByClient = from key in keys
			                   let url = GetUrl(key)
			                   group key by url
			                   into g
			                   		select new {
			                   		           		Client = g.Key,
			                   		           		Keys = g.ToArray()
			                   		           };

			var values = new List<KeyValuePair<string, object>>();
			foreach (var g in keysByClient)
			{
				var copy = g;
				ClientAction(copy.Client, client => {
				                          	var cachedValues = client.Get(copy.Keys);
				                          	foreach (var cachedValue in cachedValues)
				                          	{
				                          		values.Add(GetDeserializedItem(cachedValue));
				                          	}
				                          });
			}
			return values.ToArray();
		}

		private KeyValuePair<string, object> GetDeserializedItem(CachedValue cachedValue)
		{
			var deserialize = Deserialize(
					(SerializationOptions)cachedValue.Flags,
					cachedValue.Value);
			return new KeyValuePair<string, object>(
					cachedValue.Key,
					deserialize);
		}

		private object Deserialize(SerializationOptions options, byte[] value)
		{
			switch (options)
			{
				case SerializationOptions.None:
					return value;
				case SerializationOptions.String:
					return Encoding.UTF8.GetString(value);
				case SerializationOptions.Serialized:
					using (var stream = new MemoryStream(value))
						return this.formatter.Deserialize(stream);
				case SerializationOptions.Integer:
					return BitConverter.ToInt32(value, 0);
				case SerializationOptions.Long:
					return BitConverter.ToInt64(value, 0);
				default:
					throw new ArgumentOutOfRangeException("options cannot be: " + options);
			}
		}

		public bool Prepend(string key, object value)
		{
			return ClientFunc(key, client => {
			                       	SerializationOptions options;
			                       	var result = client.Prepend(key, Serialize(value, out options));
			                       	return result == CacheOperationResult.Stored;
			                       });
		}

		public object Gets(string key, out long timestamp)
		{
			long tmp = 0;
			var result = ClientFunc(key, client => {
			                             	var values = client.Get(key);
			                             	if (values.Length == 0)
			                             	{
			                             		tmp = 0;
			                             		return null;
			                             	}
			                             	tmp = values[0].Timestamp;
			                             	return GetDeserializedItem(values[0]).Value;
			                             });
			timestamp = tmp;
			return result;
		}

		public CacheOperationResult CheckAndSet(string key, object value, long timestamp)
		{
			return ClientFunc(key, client => {
			                       	SerializationOptions options;
			                       	var serialize = Serialize(value, out options);
			                       	return client.CompareAndSwap(key, (long)options, null, timestamp, serialize);
			                       });
		}

		public void Delete(string key, TimeSpan timeSpan)
		{
			ClientAction(GetUrl(key), client => client.Delete(key, SystemTime.Now().Add(timeSpan)));
		}

		public void FlushAll()
		{
			foreach (var url in this.urls)
			{
				try
				{
					ClientAction(url, client => client.FlushAll(null));
				}
				catch
				{
					//nothing to do
				}
			}
		}

		public void FlushAll(TimeSpan span)
		{
			foreach (var url in this.urls)
			{
				try
				{
					ClientAction(url, client => client.FlushAll(SystemTime.Now().Add(span)));
				}
				catch
				{
					//nothing to do
				}
			}
		}

		public ulong? Increment(string key, ulong value)
		{
			return ClientFunc<ulong?>(key, client => {
			                               	var result = client.Incr(key, value);
			                               	if (result.Result == CacheOperationResult.Stored)
			                               		return result.Value;
			                               	return null;
			                               });
		}

		public ulong? Decrement(string key, ulong value)
		{
			return ClientFunc<ulong?>(key, client => {
			                               	var result = client.Decr(key, value);
			                               	if (result.Result == CacheOperationResult.Stored)
			                               		return result.Value;
			                               	return null;
			                               });
		}

		public bool Replace(string key, object value)
		{
			return ClientFunc(key, client => {
			                       	SerializationOptions options;
			                       	var serialize = Serialize(value, out options);
			                       	var result = client.Replace(key, (long)options, null, serialize);
			                       	return result == CacheOperationResult.Stored;
			                       });
		}

		public bool Set(string key, object value, TimeSpan span)
		{
			return ClientFunc(key, client => {
			                       	SerializationOptions options;
			                       	var serialize = Serialize(value, out options);
			                       	var result = client.Set(key, (long)options, SystemTime.Now().Add(span), serialize);
			                       	return result == CacheOperationResult.Stored;
			                       });
		}

		public bool Set(string key, object value, DateTime time)
		{
			return ClientFunc(key, client => {
			                       	SerializationOptions options;
			                       	var serialize = Serialize(value, out options);
			                       	var result = client.Set(key, (long)options, time, serialize);
			                       	return result == CacheOperationResult.Stored;
			                       });
		}

		private T ClientFunc<T>(string key, Func<ICacheProvider, T> func)
		{
			var url = GetUrl(key);
			var client = CreateClient(url);
			try
			{
				return func(client);
			}
			finally
			{
				DisposeObject(url, client);
			}
		}

		//private ICacheProvider CreateClient(string url)
		//{
		//    var queue = pool[url];
		//    lock (queue)
		//    {
		//        if (queue.Count == 0)
		//        {
		//            var channel = ChannelFactory<ICacheProvider>.CreateChannel(
		//                binding,
		//                new EndpointAddress(url));
		//            ((ICommunicationObject)channel).Open();
		//            return channel;
		//        }
		//        return queue.Dequeue();
		//    }
		//}

		//private void DisposeObject(string url, ICacheProvider client)
		//{
		//    var co = (ICommunicationObject)client;
		//    if (co.State == CommunicationState.Faulted)
		//    {
		//        co.Close();
		//        return;
		//    }
		//    var queue = pool[url];
		//    lock (queue)
		//        queue.Enqueue(client);
		//}

		private void ClientAction(string clientUrl, Action<ICacheProvider> func)
		{
			var client = CreateClient(clientUrl);
			try
			{
				func(client);
			}
			finally
			{
				DisposeObject(clientUrl, client);
			}
		}

		private enum SerializationOptions : long
		{
			None = 0,
			String = 1,
			Serialized = 3,
			Integer = 4,
			Long = 5
		}
	}
}