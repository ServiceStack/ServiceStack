using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace ServiceStack.ServiceInterface
{
	/// <summary>
	/// Base class for all ServiceResolvers.
	/// </summary>
	/// <remarks>
	/// Uses reflection to automatically resolve and instantiate request handlers based on a specific
	/// naming convention.  All request handler implementation classes should be defined inside a 
	/// sub namespace of the namespace of the concrete ServiceResolver.  The namespace should
	/// be named "VersionXXX" where XXX is the version numer.  The ports should be named
	/// "[operation]Handler" where operation is the name of the service operation.
	/// </remarks>
	public abstract class BaseServiceResolver
		: IServiceResolver
	{
		private readonly int minVersion;
		private readonly int maxVersion;
		private readonly IDictionary<int, IDictionary<string, Type>> handlerCacheByVersion;
		private static readonly Regex handlerTypeNameRegex = new Regex(@".*\.Version([0-9]+)\.(.*)Handler$", RegexOptions.Compiled);

		protected BaseServiceResolver()
		{
			this.handlerCacheByVersion = new Dictionary<int, IDictionary<string, Type>>();

			Type type = this.GetType();

			foreach (Type typeInAssembly in type.Assembly.GetTypes())
			{
				const int VERSION_INDEX = 1;
				const int PORT_NAME_INDEX = 2;
				IDictionary<string, Type> handlerTypeCache;

				Match match = handlerTypeNameRegex.Match(typeInAssembly.Namespace + "." + typeInAssembly.Name);

				if (!match.Success)
				{
					continue;
				}

				int versionNumber = Convert.ToInt32(match.Groups[VERSION_INDEX].Value);
				string portName = match.Groups[PORT_NAME_INDEX].Value;

				if (versionNumber < this.minVersion)
				{
					this.minVersion = versionNumber;
				}

				if (versionNumber > this.maxVersion)
				{
					if (this.minVersion == 0)
					{
						this.minVersion = versionNumber;
					}
					this.maxVersion = versionNumber;
				}

				if (!this.handlerCacheByVersion.TryGetValue(versionNumber, out handlerTypeCache))
				{
					handlerTypeCache = new Dictionary<string, Type>(StringComparer.InvariantCultureIgnoreCase);
					this.handlerCacheByVersion.Add(versionNumber, handlerTypeCache);
				}

				handlerTypeCache.Add(portName, typeInAssembly);
			}
		}


		/// <summary>
		/// Finds a service by the service name (i.e. port name).
		/// Always returns the port from the maximum version. 
		/// </summary>
		/// <returns>A new instance of the port</returns>
		public virtual object FindService(string serviceName)
		{
			return FindService(serviceName, this.maxVersion);
		}

		/// <summary>
		/// Finds a service by the service name (i.e. handler name) and version number.
		/// </summary>
		/// <returns>A new instance of the handler</returns>
		public virtual object FindService(string operationName, int version)
		{
			IDictionary<string, Type> portCache;

			if (this.handlerCacheByVersion.TryGetValue(version, out portCache))
			{
				Type type;

				if (!portCache.TryGetValue(operationName, out type))
				{
					// Can't find service
					return null;
				}

				return Activator.CreateInstance(type);
			}

			throw new NotSupportedException(string.Format("This service supports versions '{0}' to '{1}' version provided was '{2}'", 
				this.minVersion, this.maxVersion, version));
		}
	}
}