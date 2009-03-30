using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text.RegularExpressions;
using ServiceStack.Service;

namespace ServiceStack.ServiceInterface
{
	/// <summary>
	/// Base class for resolving handlers decorated with the [Port] attribute.
	/// </summary>
	/// <remarks>
	/// Uses port attributes to find the available ports and what messages they can handle.
	/// </remarks>
	public class PortResolver : IServiceResolver
	{
		private const string RESPONSE_SUFFIX = "Response";
		private const int DEFAULT_VERSION = default(int);
		private readonly IDictionary<int, IDictionary<string, Type>> handlerCacheByVersion;
		private readonly IDictionary<Type, PortAttribute> handlerPortAttributeMap;

		/// <summary>
		/// Returns a list of ALL operation types available in this service, required for WSDL generation.
		/// </summary>
		public static readonly Regex DefaultAllOperationsMatch = new Regex(@"\.Operations\.", RegexOptions.Compiled);
		public Regex AllOperationsMatch { get; set; }
		public IList<Type> AllOperationTypes { get; protected set; }

		public PortResolver(params Assembly[] serviceInterfaceAssemblies)
		{
			this.OperationTypes = new List<Type>();
			this.AllOperationTypes = new List<Type>();
			this.handlerCacheByVersion = new Dictionary<int, IDictionary<string, Type>>();
			this.handlerPortAttributeMap = new Dictionary<Type, PortAttribute>();
			AllOperationsMatch = AllOperationsMatch ?? DefaultAllOperationsMatch;

			foreach (var assembly in serviceInterfaceAssemblies)
			{
				foreach (Type portType in assembly.GetTypes())
				{
					IDictionary<string, Type> handlerTypeCache;

					if (AllOperationsMatch.IsMatch(portType.FullName))
					{
						var dtoAttrs = portType.GetCustomAttributes(typeof(DataContractAttribute), false);
						if (dtoAttrs.Length > 0)
						{
							this.AllOperationTypes.Add(portType);
						}
					}

					var attrs = portType.GetCustomAttributes(typeof(PortAttribute), false);

					if (attrs.Length == 0)
						continue;

					var portAttr = (PortAttribute)attrs[0];

					this.OperationTypes.Add(portAttr.OperationType);
					var responseTypeName = portAttr.OperationType.FullName + RESPONSE_SUFFIX;
					var responseType = portAttr.OperationType.Assembly.GetType(responseTypeName);
					if (responseType != null)
					{
						this.OperationTypes.Add(responseType);
					}

					var operationName = portAttr.OperationType.Name;
					var versionNumber = portAttr.Version.GetValueOrDefault(DEFAULT_VERSION);

					if (!this.handlerCacheByVersion.TryGetValue(versionNumber, out handlerTypeCache))
					{
						handlerTypeCache = new Dictionary<string, Type>();
						this.handlerCacheByVersion.Add(versionNumber, handlerTypeCache);
					}

					if (handlerTypeCache.ContainsKey(operationName))
					{
						throw new AmbiguousMatchException(string.Format(
							"A port handler has already been registered for operation '{0}' version '{1}'",
							operationName, versionNumber));
					}
					handlerTypeCache[operationName] = portType;
					handlerPortAttributeMap[portType] = portAttr;
				}
			}
		}

		/// <summary>
		/// Returns a list of operation types available in this service
		/// </summary>
		/// <value>The operation types.</value>
		public IList<Type> OperationTypes
		{
			get;
			protected set;
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
			version = version.GetValueOrDefault(DEFAULT_VERSION);

			if (this.handlerCacheByVersion.TryGetValue(version.Value, out portCache))
			{
				Type type;

				return !portCache.TryGetValue(operationName, out type) ? null : Activator.CreateInstance(type);
			}

			return null;
		}

		/// <summary>
		/// Finds the service model.
		/// </summary>
		/// <param name="operationName">Name of the operation.</param>
		/// <returns></returns>
		public Type FindOperationType(string operationName)
		{
			return FindOperationType(operationName, null);
		}

		/// <summary>
		/// Finds the specified version service model.
		/// </summary>
		/// <param name="operationName">Name of the operation.</param>
		/// <param name="version">The version.</param>
		/// <returns></returns>
		public Type FindOperationType(string operationName, int? version)
		{
			var port = FindService(operationName, version);
			if (port == null) return null;

			var portAttr = handlerPortAttributeMap[port.GetType()];
			return portAttr.OperationType;
		}
	}
}