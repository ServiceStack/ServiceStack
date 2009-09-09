using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;
using ServiceStack.Logging;

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
		private static readonly ILog Log = LogManager.GetLogger(typeof(ZeroConfigServiceResolver));

		private int minVersion;
		private int maxVersion;
		private IDictionary<int, IDictionary<string, Type>> handlerCacheByVersion;
		private IDictionary<string, List<int>> handlerVersions;
		private static readonly Regex HandlerTypeNameRegex = new Regex(@".*\.Version([0-9]+)\.(.*)Handler$", RegexOptions.Compiled);
		private static readonly Regex OperationTypeRegex = new Regex(@".*Version([0-9]+)\..*", RegexOptions.Compiled);

		/// <summary>
		/// Returns a list of ALL operation types available in this service, required for WSDL generation.
		/// </summary>
		public Regex DefaultAllOperationsMatch = new Regex(@"\.Operations\.");
		public Regex AllOperationsMatch { get; set; }
		public IList<Type> AllOperationTypes { get; protected set; }

		public Func<Type, object> HandlerFactory { get; set; }

		public ZeroConfigServiceResolver(Assembly serviceInterfaceAssembly, Assembly serviceModelAssembly, string operationNamespace)
		{
			AllOperationsMatch = AllOperationsMatch ?? DefaultAllOperationsMatch;
			AllOperationTypes = new List<Type>();

			LoadOperations(serviceModelAssembly, operationNamespace);

			LoadHandlers(serviceInterfaceAssembly);

			SortOperationVersions();

			this.HandlerFactory = DefaultCreateHandlerFactory;
		}

		private static object DefaultCreateHandlerFactory(Type type)
		{
			return Activator.CreateInstance(type);
		}

		private void LoadHandlers(Assembly serviceInterfaceAssembly)
		{
			this.handlerCacheByVersion = new Dictionary<int, IDictionary<string, Type>>();
			this.handlerVersions = new Dictionary<string, List<int>>();

			foreach (Type portType in serviceInterfaceAssembly.GetTypes())
			{
				const int versionIndex = 1;
				const int portNameIndex = 2;
				IDictionary<string, Type> handlerTypeCache;

				Match match = HandlerTypeNameRegex.Match(portType.Namespace + "." + portType.Name);

				if (!match.Success)
				{
					continue;
				}

				int versionNumber = Convert.ToInt32(match.Groups[versionIndex].Value);
				string operationName = match.Groups[portNameIndex].Value;

				if (!this.handlerVersions.ContainsKey(operationName))
				{
					this.handlerVersions[operationName] = new List<int>();
				}
				this.handlerVersions[operationName].Add(versionNumber);

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

				handlerTypeCache.Add(operationName, portType);
			}
		}

		private void SortOperationVersions()
		{
			foreach (var list in this.handlerVersions.Values)
			{
				list.Sort();
			}
		}

		private IDictionary<string, Type> typesByName;
		/// <summary>
		/// Loads the operation types for all the types in the serviceModelAssembly that are in the 
		/// operationNamespace provided. 
		/// </summary>
		/// <param name="serviceModelAssembly">The service model assembly.</param>
		/// <param name="operationNamespace">The operation namespace.</param>
		protected void LoadOperations(Assembly serviceModelAssembly, string operationNamespace)
		{
			this.OperationTypes = new List<Type>();
			this.typesByName = new Dictionary<string, Type>();
			foreach (var serviceModelType in serviceModelAssembly.GetTypes())
			{
				if (serviceModelType.Namespace.StartsWith(operationNamespace))
				{
					try
					{
						if (!serviceModelType.Namespace.Contains("Version"))
						{
							Log.WarnFormat("Ignoring DTO candidate: '{0}'", serviceModelType.FullName);
							continue;
						}
						Match match = OperationTypeRegex.Match(serviceModelType.FullName);
						string versionText = match.Groups[1].Value;
						int version = Convert.ToInt32(versionText);

						if (match.Success)
						{
							this.typesByName[version + ":" + serviceModelType.Name] = serviceModelType;
						}
					}
					catch (Exception ex)
					{
						Log.ErrorFormat("Exception in ServiceModelFinderBase() parsing type: '{0}'",
							serviceModelType.FullName, ex);

						throw;
					}

					this.OperationTypes.Add(serviceModelType);

					if (AllOperationsMatch.IsMatch(serviceModelType.FullName))
					{
						this.AllOperationTypes.Add(serviceModelType);
					}
				}
			}
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
			return FindService(operationName, null);
		}

		/// <summary>
		/// Finds a service by the service name (i.e. handler name) and version number.
		/// </summary>
		/// <returns>A new instance of the handler</returns>
		public virtual object FindService(string operationName, int? version)
		{
			IDictionary<string, Type> portCache;

			version = version ?? GetLatestVersion(operationName);

			if (this.handlerCacheByVersion.TryGetValue(version.Value, out portCache))
			{
				Type type;

				if (!portCache.TryGetValue(operationName, out type))
				{
					// Can't find service
					return null;
				}

				return HandlerFactory(type);
			}

			throw new NotSupportedException(string.Format("This service supports versions '{0}' to '{1}' version provided was '{2}'", 
				this.minVersion, this.maxVersion, version));
		}

		private int? GetLatestVersion(string operationName)
		{
			List<int> versions;
			if (!this.handlerVersions.TryGetValue(operationName, out versions))
			{
					throw new NotImplementedException(string.Format("Cannot find operation '{0}'", operationName));
			}
			var latestVersion = versions[versions.Count - 1];
			int? version = latestVersion;
			return version;
		}

		public Type FindOperationType(string operationName)
		{
			return FindOperationType(operationName, null);
		}

		public Type FindOperationType(string operationName, int? version)
		{
			Type retval;
			version = version ?? GetLatestVersion(operationName);

			if (this.typesByName.TryGetValue(version + ":" + operationName, out retval))
			{
				return retval;
			}

			return null;
		}

	}
}