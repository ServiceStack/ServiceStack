using System.Collections.Generic;
using ServiceStack.Configuration;

namespace ServiceStack.ServiceInterface
{
	public class FactoryProvider : IFactoryProvider
	{
		private static readonly object semaphore = new object();
		private const string OBJECTS_SECTION_NAME = "objects";
		private Dictionary<string, object> ObjectInstanceCache { get; set; }

		public FactoryProvider() : this(OBJECTS_SECTION_NAME)
		{
		}

		public FactoryProvider(string configSectionName)
		{
			this.Factory = FactoryUtils.CreateObjectFactoryFromConfig(configSectionName);
			this.ObjectInstanceCache = new Dictionary<string, object>();
		}

		private IObjectFactory Factory { get; set; }

		/// <summary>
		/// Gets a cached instance of a factory object
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="name">The name.</param>
		/// <returns></returns>
		public T Resolve<T>(string name)
		{
			if (!ObjectInstanceCache.ContainsKey(name))
			{
				ObjectInstanceCache[name] = CreateObject<T>(name);
			}
			return (T)ObjectInstanceCache[name];
		}

		/// <summary>
		/// Creates a new instance of a factory object
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="name">The name.</param>
		/// <returns></returns>
		public T CreateObject<T>(string name)
		{
			lock (semaphore)
			{
				return Factory.Create<T>(name);
			}
		}
	}
}