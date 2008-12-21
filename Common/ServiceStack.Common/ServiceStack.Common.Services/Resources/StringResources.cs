using System.Collections.Generic;
using ServiceStack.Logging;

namespace ServiceStack.Common.Services.Resources
{
	public class StringResources<TKey>
	{
		private readonly Dictionary<TKey, string> resources = new Dictionary<TKey, string>();
		private readonly ILog log;

		public StringResources(string catagory, ILogFactory logFactory)
		{
			this.Catagory = catagory;
			this.log = logFactory.GetLogger(GetType());

			// Set the initial default error value to null
			this.DefaultResource = null;
		}

		public string Catagory { get; private set; }

		public string DefaultResource { get; set; }

		public int Count
		{
			get { return this.resources.Count; }
		}

		public bool Contains(TKey key)
		{
			return this.resources.ContainsKey(key);
		}

		public string this[TKey key]
		{
			get
			{
				string errorString;
				if (this.resources.TryGetValue(key, out errorString))
				{
					return errorString;
				}

				this.log.WarnFormat("No {0} string resource found for key: {0}", this.Catagory, key);
				return this.DefaultResource;
			}

			set
			{
				this.resources[key] = value;
			}
		}
	}
}