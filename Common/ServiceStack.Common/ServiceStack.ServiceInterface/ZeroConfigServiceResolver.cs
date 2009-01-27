using System;
using System.Collections.Generic;
using System.Reflection;
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
	public class ZeroConfigServiceResolver
		: IServiceResolver
	{
		private readonly int minVersion;
		private readonly int maxVersion;
		private readonly IDictionary<int, IDictionary<string, Type>> handlerCacheByVersion;
		private readonly IDictionary<string, List<int>> operationVersions;
		private static readonly Regex handlerTypeNameRegex = new Regex(@".*\.Version([0-9]+)\.(.*)Handler$", RegexOptions.Compiled);

		public ZeroConfigServiceResolver(Assembly serviceInterfaceAssembly, Assembly serviceModelAssembly, string operationNamespace)
		{
			this.handlerCacheByVersion = new Dictionary<int, IDictionary<string, Type>>();
			this.OperationTypes = GetOperationTypes(serviceModelAssembly, operationNamespace);
			operationVersions = new Dictionary<string, List<int>>();

			foreach (Type typeInAssembly in serviceInterfaceAssembly.GetTypes())
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
				string operationName = match.Groups[PORT_NAME_INDEX].Value;

				if (!operationVersions.ContainsKey(operationName))
				{
					operationVersions[operationName] = new List<int>();
				}
				operationVersions[operationName].Add(versionNumber);

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

				handlerTypeCache.Add(operationName, typeInAssembly);
			}

			SortOperationVersions();
		}

		private void SortOperationVersions()
		{
			foreach (var list in this.operationVersions.Values)
			{
				list.Sort();
			}
		}

		/// <summary>
		/// Loads the operation types for all the types in the serviceModelAssembly that are in the 
		/// operationNamespace provided. 
		/// </summary>
		/// <param name="serviceModelAssembly">The service model assembly.</param>
		/// <param name="operationNamespace">The operation namespace.</param>
		public List<Type> GetOperationTypes(Assembly serviceModelAssembly, string operationNamespace)
		{
			var operationTypes = new List<Type>();
			if (serviceModelAssembly != null)
			{
				foreach (var serviceModelType in serviceModelAssembly.GetTypes())
				{
					if (serviceModelType.Namespace.StartsWith(operationNamespace))
					{
						operationTypes.Add(serviceModelType);
					}
				}
			}
			return operationTypes;
		}

		/// <summary>
		/// Returns a list of operation types available in this service
		/// </summary>
		/// <value>The operation types.</value>
		public IList<Type> OperationTypes
		{
			get; protected set;
		}

		/// <summary>
		/// Finds a service by the service name (i.e. port name).
		/// Always returns the port from the maximum version. 
		/// </summary>
		/// <returns>A new instance of the port</returns>
		public virtual object FindService(string operationName)
		{
			List<int> versions;
			if (!operationVersions.TryGetValue(operationName, out versions))
			{
				throw new NotImplementedException(string.Format("Cannot find operation '{0}'", operationName));
			}
			var latestVersion = versions[versions.Count - 1];
			return FindService(operationName, latestVersion);
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