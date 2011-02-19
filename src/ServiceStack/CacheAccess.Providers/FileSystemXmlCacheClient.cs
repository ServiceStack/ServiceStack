using System;
using System.Collections.Generic;
using System.IO;
using ServiceStack.Logging;
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
	public class FileSystemXmlCacheClient 
		: ICacheClient
	{
		private static readonly ILog Log = LogManager.GetLogger(typeof(FileSystemXmlCacheClient));

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

		public void RemoveAll(IEnumerable<string> keys)
		{
			foreach (var key in keys)
			{
				try
				{
					this.Remove(key);
				}
				catch (Exception ex)
				{
					Log.Error(string.Format("Error trying to remove {0} from the FileSystem Cache", key), ex);
				}
			}
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

		public bool Add<T>(string key, T value)
		{
			throw new NotImplementedException();
		}

		public bool Set<T>(string relativePath, T value)
		{
			var absolutePath = GetAbsolutePath(relativePath);
			
			Directory.CreateDirectory(Path.GetDirectoryName(absolutePath));

			var xml = DataContractSerializer.Instance.Parse(value);
			File.WriteAllText(absolutePath, xml);

			return true;
		}

		public bool Replace<T>(string key, T value)
		{
			throw new NotImplementedException();
		}

		public bool Add<T>(string key, T value, DateTime expiresAt)
		{
			throw new NotImplementedException();
		}

		public bool Set<T>(string key, T value, DateTime expiresAt)
		{
			throw new NotImplementedException();
		}

		public bool Replace<T>(string key, T value, DateTime expiresAt)
		{
			throw new NotImplementedException();
		}

		public bool Add<T>(string key, T value, TimeSpan expiresIn)
		{
			throw new NotImplementedException();
		}

		public bool Set<T>(string key, T value, TimeSpan expiresIn)
		{
			throw new NotImplementedException();
		}

		public bool Replace<T>(string key, T value, TimeSpan expiresIn)
		{
			throw new NotImplementedException();
		}

		public void FlushAll()
		{
			throw new NotImplementedException();
		}

		public IDictionary<string, T> GetAll<T>(IEnumerable<string> keys)
		{
			throw new NotImplementedException();
		}

		public void SetAll<T>(IDictionary<string, T> values)
		{
			foreach (var entry in values)
			{
				Set(entry.Key, entry.Value);
			}
		}
	}
}