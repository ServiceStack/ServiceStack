using System;
using System.Collections.Generic;
using System.IO;
using ServiceStack.ServiceModel.Serialization;

namespace ServiceStack.CacheAccess.Providers
{
	/// <summary>
	/// Implements a very limited subset of ICacheClient, i.e.
	/// 
	///		- T Get[T]()
	///		- Set(path, value)
	///		- Remove(path)
	/// 
	/// </summary>
	public class FileSystemXmlCacheClient : ICacheClient
	{
		private readonly string baseFilePath;

		public FileSystemXmlCacheClient(string baseFilePath)
		{
			this.baseFilePath = baseFilePath;
		}

		private string GetAbsolutePath(string relativePath)
		{
			return Path.Combine(this.baseFilePath, relativePath);
		}

		public void Dispose()
		{
		}

		public bool Remove(string relativePath)
		{
			try
			{
				File.Delete(GetAbsolutePath(relativePath));
				return true;
			}
			catch
			{
				return false;
			}
		}

		/// <summary>
		/// As no type is specified we can only retrieve the file contents, 
		/// it is responsible for the callee to handle the deserialization
		/// </summary>
		/// <param name="relativePath">The relative path.</param>
		/// <returns></returns>
		public object Get(string relativePath)
		{
			return !File.Exists(relativePath) 
				? null : File.ReadAllText(GetAbsolutePath(relativePath));
		}

		public T Get<T>(string relativePath)
		{
			var absolutePath = GetAbsolutePath(relativePath);

			if (!File.Exists(absolutePath)) return default(T);

			var xml = File.ReadAllText(absolutePath);
			return (T)DataContractDeserializer.Instance.Parse(xml, typeof(T));
		}

		public long Increment(string key, uint amount)
		{
			throw new NotImplementedException();
		}

		public long Decrement(string key, uint amount)
		{
			throw new NotImplementedException();
		}

		public bool Add(string key, object value)
		{
			throw new NotImplementedException();
		}

		public bool Set(string relativePath, object value)
		{
			var absolutePath = GetAbsolutePath(relativePath);
			
			Directory.CreateDirectory(Path.GetDirectoryName(absolutePath));

			var xml = DataContractSerializer.Instance.Parse(value);
			File.WriteAllText(absolutePath, xml);

			return true;
		}

		public bool Replace(string key, object value)
		{
			throw new NotImplementedException();
		}

		public bool Add(string key, object value, DateTime expiresAt)
		{
			throw new NotImplementedException();
		}

		public bool Set(string key, object value, DateTime expiresAt)
		{
			throw new NotImplementedException();
		}

		public bool Replace(string key, object value, DateTime expiresAt)
		{
			throw new NotImplementedException();
		}

		public bool CheckAndSet(string key, object value, ulong lastModifiedValue)
		{
			throw new NotImplementedException();
		}

		public bool CheckAndSet(string key, object value, ulong lastModifiedValue, DateTime expiresAt)
		{
			throw new NotImplementedException();
		}

		public void FlushAll()
		{
			throw new NotImplementedException();
		}

		public IDictionary<string, object> Get(IEnumerable<string> keys)
		{
			throw new NotImplementedException();
		}

		public IDictionary<string, object> Get(IEnumerable<string> keys, out IDictionary<string, ulong> lastModifiedValues)
		{
			throw new NotImplementedException();
		}
	}
}