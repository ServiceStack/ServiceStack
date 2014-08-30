using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using ServiceStack.DataAnnotations;
using ServiceStack.Text;
using ServiceStack.Web;

namespace ServiceStack.Host
{
    public class ServiceMetadata
    {
        public ServiceMetadata(List<RestPath> restPaths)
        {
            this.restPaths = restPaths;
            this.RequestTypes = new HashSet<Type>();
            this.ServiceTypes = new HashSet<Type>();
            this.ResponseTypes = new HashSet<Type>();
            this.OperationsMap = new Dictionary<Type, Operation>();
            this.OperationsResponseMap = new Dictionary<Type, Operation>();
            this.OperationNamesMap = new Dictionary<string, Operation>();
        }

        public Dictionary<Type, Operation> OperationsMap { get; protected set; }
        public Dictionary<Type, Operation> OperationsResponseMap { get; protected set; }
        public Dictionary<string, Operation> OperationNamesMap { get; protected set; }
        public HashSet<Type> RequestTypes { get; protected set; }
        public HashSet<Type> ServiceTypes { get; protected set; }
        public HashSet<Type> ResponseTypes { get; protected set; }
        private List<RestPath> restPaths;

        public IEnumerable<Operation> Operations
        {
            get { return OperationsMap.Values; }
        }

        public void Add(Type serviceType, Type requestType, Type responseType)
        {
            this.ServiceTypes.Add(serviceType);
            this.RequestTypes.Add(requestType);

            var restrictTo = requestType.FirstAttribute<RestrictAttribute>()
                          ?? serviceType.FirstAttribute<RestrictAttribute>();

            var operation = new Operation {
                ServiceType = serviceType,
                RequestType = requestType,
                ResponseType = responseType,
                RestrictTo = restrictTo,
                Actions = GetImplementedActions(serviceType, requestType),
                Routes = new List<RestPath>(),
            };

			this.OperationsMap[requestType] = operation;
			this.OperationNamesMap[operation.Name.ToLower()] = operation;
			//this.OperationNamesMap[requestType.Name.ToLower()] = operation;
			if (responseType != null)
			{
				this.ResponseTypes.Add(responseType);
				this.OperationsResponseMap[responseType] = operation;
			}

            //Only count non-core ServiceStack Services, i.e. defined outside of ServiceStack.dll or Swagger
            var nonCoreServicesCount = OperationsMap.Values
                .Count(x => x.ServiceType.Assembly != typeof(Service).Assembly
                && x.ServiceType.FullName != "ServiceStack.Api.Swagger.SwaggerApiService"
                && x.ServiceType.FullName != "ServiceStack.Api.Swagger.SwaggerResourcesService"
                && x.ServiceType.Name != "__AutoQueryServices");

            LicenseUtils.AssertValidUsage(LicenseFeature.ServiceStack, QuotaType.Operations, nonCoreServicesCount);
        }

        public void AfterInit()
        {
            foreach (var restPath in restPaths)
            {
                Operation operation;
                if (!OperationsMap.TryGetValue(restPath.RequestType, out operation))
                    continue;

                operation.Routes.Add(restPath);
            }
        }

        readonly HashSet<Assembly> excludeAssemblies = new HashSet<Assembly>
        {
            typeof(string).Assembly,            //mscorelib
            typeof(Uri).Assembly,               //System
            typeof(ServiceStackHost).Assembly,  //ServiceStack
            typeof(UrnId).Assembly,             //ServiceStack.Common
            typeof(ErrorResponse).Assembly,     //ServiceStack.Interfaces
        };

        public List<Assembly> GetOperationAssemblies()
        {
            var assemblies = Operations
                .SelectMany(x => x.GetAssemblies())
                .Where(x => !excludeAssemblies.Contains(x));

            return assemblies.ToList();
        }

        public List<OperationDto> GetOperationDtos()
        {
            return OperationsMap.Values
                .Map(x => x.ToOperationDto())
                .OrderBy(x => x.Name)
                .ToList();
        }

        public Operation GetOperation(Type operationType)
        {
            Operation op;
            OperationsMap.TryGetValue(operationType, out op);
            return op;
        }

        public List<string> GetImplementedActions(Type serviceType, Type requestType)
        {
            if (!typeof(IService).IsAssignableFrom(serviceType))
                throw new NotSupportedException("All Services must implement IService");

            return serviceType.GetActions()
                .Where(x => x.GetParameters()[0].ParameterType == requestType)
                .Select(x => x.Name.ToUpper())
                .ToList();
        }

        public Type GetOperationType(string operationTypeName)
        {
            Operation operation;
            OperationNamesMap.TryGetValue(operationTypeName.ToLower(), out operation);
            return operation != null ? operation.RequestType : null;
        }

        public Type GetServiceTypeByRequest(Type requestType)
        {
            Operation operation;
            OperationsMap.TryGetValue(requestType, out operation);
            return operation != null ? operation.ServiceType : null;
        }

        public Type GetServiceTypeByResponse(Type responseType)
        {
            Operation operation;
            OperationsResponseMap.TryGetValue(responseType, out operation);
            return operation != null ? operation.ServiceType : null;
        }

        public Type GetResponseTypeByRequest(Type requestType)
        {
            Operation operation;
            OperationsMap.TryGetValue(requestType, out operation);
            return operation != null ? operation.ResponseType : null;
        }

        public List<Type> GetAllTypes()
        {
            var allTypes = new List<Type>(RequestTypes);
            foreach (var responseType in ResponseTypes)
            {
                allTypes.AddIfNotExists(responseType);
            }
            return allTypes;
        }

        public List<string> GetAllOperationNames()
        {
            return Operations.Select(x => x.RequestType.GetOperationName()).OrderBy(operation => operation).ToList();
        }

        public List<string> GetOperationNamesForMetadata(IRequest httpReq)
        {
            return GetAllOperationNames();
        }

        public List<string> GetOperationNamesForMetadata(IRequest httpReq, Format format)
        {
            return GetAllOperationNames();
        }

        public bool IsVisible(IRequest httpReq, Operation operation)
        {
            if (HostContext.Config != null && !HostContext.Config.EnableAccessRestrictions)
                return true;

            if (operation.RestrictTo == null) return true;

            //Less fine-grained on /metadata pages. Only check Network and Format
            var reqAttrs = httpReq.GetAttributes();
            var showToNetwork = CanShowToNetwork(operation.RestrictTo, reqAttrs);
            return showToNetwork;
        }

        public bool IsVisible(IRequest httpReq, Type requestType)
        {
            if (HostContext.Config != null && !HostContext.Config.EnableAccessRestrictions)
                return true;

            var operation = HostContext.Metadata.GetOperation(requestType);
            return operation == null || IsVisible(httpReq, operation);
        }

        public bool IsVisible(IRequest httpReq, Format format, string operationName)
        {
            if (HostContext.Config != null && !HostContext.Config.EnableAccessRestrictions)
                return true;

            Operation operation;
            OperationNamesMap.TryGetValue(operationName.ToLowerInvariant(), out operation);
            if (operation == null) return false;

            var canCall = HasImplementation(operation, format);
            if (!canCall) return false;

            var isVisible = IsVisible(httpReq, operation);
            if (!isVisible) return false;

            if (operation.RestrictTo == null) return true;
            var allowsFormat = operation.RestrictTo.CanShowTo((RequestAttributes)(long)format);
            return allowsFormat;
        }

        public bool CanAccess(IRequest httpReq, Format format, string operationName)
        {
            var reqAttrs = httpReq.GetAttributes();
            return CanAccess(reqAttrs, format, operationName);
        }

        public bool CanAccess(RequestAttributes reqAttrs, Format format, string operationName)
        {
            if (HostContext.Config != null && !HostContext.Config.EnableAccessRestrictions)
                return true;

            Operation operation;
            OperationNamesMap.TryGetValue(operationName.ToLower(), out operation);
            if (operation == null) return false;

            var canCall = HasImplementation(operation, format);
            if (!canCall) return false;

            if (operation.RestrictTo == null) return true;

            var allow = operation.RestrictTo.HasAccessTo(reqAttrs);
            if (!allow) return false;

            var allowsFormat = operation.RestrictTo.HasAccessTo((RequestAttributes)(long)format);
            return allowsFormat;
        }

        public bool CanAccess(Format format, string operationName)
        {
            if (HostContext.Config != null && !HostContext.Config.EnableAccessRestrictions)
                return true;

            Operation operation;
            OperationNamesMap.TryGetValue(operationName.ToLower(), out operation);
            if (operation == null) return false;

            var canCall = HasImplementation(operation, format);
            if (!canCall) return false;

            if (operation.RestrictTo == null) return true;

            var allowsFormat = operation.RestrictTo.HasAccessTo((RequestAttributes)(long)format);
            return allowsFormat;
        }

        public bool HasImplementation(Operation operation, Format format)
        {
            if (format == Format.Soap11 || format == Format.Soap12)
            {
                if (operation.Actions == null) return false;

                return operation.Actions.Contains("POST")
                    || operation.Actions.Contains(ActionContext.AnyAction);
            }
            return true;
        }

        private static bool CanShowToNetwork(RestrictAttribute restrictTo, RequestAttributes reqAttrs)
        {
            if (reqAttrs.IsLocalhost())
                return restrictTo.CanShowTo(RequestAttributes.Localhost)
                       || restrictTo.CanShowTo(RequestAttributes.LocalSubnet);

            return restrictTo.CanShowTo(
                reqAttrs.IsLocalSubnet()
                    ? RequestAttributes.LocalSubnet
                    : RequestAttributes.External);
        }

    }

    public class Operation
    {
    	public string Name
    	{
    		get 
			{
				return RequestType.GetOperationName(); 
			}
    	}

        public Type RequestType { get; set; }
        public Type ServiceType { get; set; }
        public Type ResponseType { get; set; }
        public RestrictAttribute RestrictTo { get; set; }
        public List<string> Actions { get; set; }
        public List<RestPath> Routes { get; set; }
        public bool IsOneWay { get { return ResponseType == null; } }
    }

    public class OperationDto
    {
        public string Name { get; set; }
        public string ResponseName { get; set; }
        public string ServiceName { get; set; }
        public List<string> RestrictTo { get; set; }
        public List<string> VisibleTo { get; set; }
        public List<string> Actions { get; set; }
        public Dictionary<string, string> Routes { get; set; }
    }

    public class XsdMetadata
    {
        public ServiceMetadata Metadata { get; set; }
        public bool Flash { get; set; }

        public XsdMetadata(ServiceMetadata metadata, bool flash = false)
        {
            Metadata = metadata;
            Flash = flash;
        }

        public List<Type> GetAllTypes()
        {
            var allTypes = new List<Type>(Metadata.RequestTypes);
            allTypes.AddRange(Metadata.ResponseTypes);
            return allTypes;
        }

        public List<string> GetReplyOperationNames(Format format)
        {
            var feature = format.ToFeature();
            return Metadata.OperationsMap.Values
                .Where(x => HostContext.Config != null
                    && HostContext.MetadataPagesConfig.CanAccess(format, x.Name))
                .Where(x => !x.IsOneWay)
                .Where(x => !x.RequestType.AllAttributes<ExcludeAttribute>()
                    .Any(attr => attr.Feature.HasFlag(feature)))
                .Select(x => x.RequestType.GetOperationName())
                .ToList();
        }

        public List<string> GetOneWayOperationNames(Format format)
        {
            var feature = format.ToFeature();
            return Metadata.OperationsMap.Values
                .Where(x => HostContext.Config != null
                    && HostContext.MetadataPagesConfig.CanAccess(format, x.Name))
                .Where(x => x.IsOneWay)
                .Where(x => !x.RequestType.AllAttributes<ExcludeAttribute>()
                    .Any(attr => attr.Feature.HasFlag(feature)))
                .Select(x => x.RequestType.GetOperationName())
                .ToList();
        }

        /// <summary>
        /// Gets the name of the base most type in the heirachy tree with the same.
        /// 
        /// We get an exception when trying to create a schema with multiple types of the same name
        /// like when inheriting from a DataContract with the same name.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns></returns>
        public static Type GetBaseTypeWithTheSameName(Type type)
        {
            var typesWithSameName = new Stack<Type>();
            var baseType = type;
            do
            {
                if (baseType.GetOperationName() == type.GetOperationName())
                    typesWithSameName.Push(baseType);
            }
            while ((baseType = baseType.BaseType) != null);

            return typesWithSameName.Pop();
        }
    }

    public static class ServiceMetadataExtensions
    {
        public static OperationDto ToOperationDto(this Operation operation)
        {
            var to = new OperationDto {
                Name = operation.Name,
                ResponseName = operation.IsOneWay ? null : operation.ResponseType.GetOperationName(),
                ServiceName = operation.ServiceType.GetOperationName(),
                Actions = operation.Actions,
                Routes = operation.Routes.ToDictionary(x => x.Path.PairWith(x.AllowedVerbs)),
            };
            
            if (operation.RestrictTo != null)
            {
                to.RestrictTo = operation.RestrictTo.AccessibleToAny.ToList().ConvertAll(x => x.ToString());
                to.VisibleTo = operation.RestrictTo.VisibleToAny.ToList().ConvertAll(x => x.ToString());
            }

            return to;
        }

		public static List< ModelInfo > GetApiMembers( this Type operationType )
		{
			var hasDataContract = operationType.HasAttribute< DataContractAttribute >();
			var properties = operationType.GetProperties();
			var attrsModel = new List< ModelInfo >();
			foreach( var property in properties )
			{
				var memattr = property.FirstAttribute< ApiMemberAttribute >();

				var valattr = property.FirstAttribute< ApiAllowableValuesAttribute >();

				if( memattr == null )
					continue;

				var propertyName = memattr.Name ?? property.Name;
				if( hasDataContract )
				{
					var dataMemberAttr = property.FirstAttribute< DataMemberAttribute >();
					if( dataMemberAttr != null && dataMemberAttr.Name != null )
						propertyName = dataMemberAttr.Name;
				}

				attrsModel.Add( new ModelInfo( propertyName, memattr, valattr ) );
			}

			return attrsModel;
		}

        public static List<Assembly> GetAssemblies(this Operation operation)
        {
            var ret = new List<Assembly> { operation.RequestType.Assembly };
            if (operation.ResponseType != null
                && operation.ResponseType.Assembly != operation.RequestType.Assembly)
            {
                ret.Add(operation.ResponseType.Assembly);
            }
            return ret;
        }
    }

		public class ModelInfo
	{
		public ModelInfo( string name, ApiMemberAttribute apiMemberAttribute )
		{
			this.Verb = apiMemberAttribute.Verb;
			this.ParameterType = apiMemberAttribute.ParameterType;
			this.Name = name ?? apiMemberAttribute.Name;
			this.Description = apiMemberAttribute.Description;
			this.DataType = apiMemberAttribute.DataType;
			this.IsRequired = apiMemberAttribute.IsRequired;
			this.AllowMultiple = apiMemberAttribute.AllowMultiple;
		}

		public ModelInfo( string name, ApiMemberAttribute apiMemberAttribute, ApiAllowableValuesAttribute allowedValues ): this( name, apiMemberAttribute )
		{
			if( allowedValues != null )
			{
				this.AllowedValues = allowedValues.Values;
				this.Min = allowedValues.Min;
				this.Max = allowedValues.Max;
			}
		}

		public string Verb{ get; private set; }
		public string ParameterType{ get; private set; }
		public string Name{ get; private set; }
		public string Description{ get; private set; }
		public string DataType{ get; private set; }
		public bool IsRequired{ get; private set; }
		public bool AllowMultiple{ get; private set; }
		public string[] AllowedValues{ get; private set; }
		public int? Min{ get; set; }
		public int? Max{ get; set; }
	}
}
