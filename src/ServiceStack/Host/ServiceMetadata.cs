using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using ServiceStack.DataAnnotations;
using ServiceStack.NativeTypes;
using ServiceStack.NativeTypes.CSharp;
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
            var opName = operationTypeName.ToLower();
            if (!OperationNamesMap.TryGetValue(opName, out operation))
            {
                var arrayPos = opName.LastIndexOf('[');
                if (arrayPos >= 0)
                {
                    opName = opName.Substring(0, arrayPos);
                    OperationNamesMap.TryGetValue(opName, out operation);
                    return operation != null
                        ? operation.RequestType.MakeArrayType()
                        : null;
                }
            }
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

        public List<Type> GetAllOperationTypes()
        {
            var allTypes = new List<Type>(RequestTypes);
            foreach (var responseType in ResponseTypes)
            {
                allTypes.AddIfNotExists(responseType);
            }
            return allTypes;
        }

        public List<Type> GetAllSoapOperationTypes()
        {
            var operationTypes = GetAllOperationTypes();
            var soapTypes = HostContext.AppHost.ExportSoapOperationTypes(operationTypes);
            return soapTypes;
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

        public List<MetadataType> GetMetadataTypesForOperation(IRequest httpReq, Operation op)
        {
            var typeMetadata = HostContext.TryResolve<INativeTypesMetadata>();

            var typesConfig = HostContext.AppHost.GetTypesConfigForMetadata(httpReq);

            var metadataTypes = typeMetadata != null
                ? typeMetadata.GetMetadataTypes(httpReq, typesConfig)
                : new MetadataTypesGenerator(this, typesConfig)
                    .GetMetadataTypes(httpReq);

            var types = new List<MetadataType>();

            var reqType = FindMetadataType(metadataTypes, op.RequestType);
            if (reqType != null)
            {
                types.Add(reqType);

                AddReferencedTypes(reqType, metadataTypes, types);
            }

            var resType = FindMetadataType(metadataTypes, op.ResponseType);
            if (resType != null)
            {
                types.Add(resType);

                AddReferencedTypes(resType, metadataTypes, types);
            }

            var generator = new CSharpGenerator(typesConfig);
            types.Each(x =>
            {
                x.DisplayType = x.DisplayType ?? generator.Type(x.Name, x.GenericArgs);
                x.Properties.Each(p =>
                    p.DisplayType = p.DisplayType ?? generator.Type(p.Type, p.GenericArgs));
            });

            return types;
        }

        private static void AddReferencedTypes(MetadataType metadataType, MetadataTypes metadataTypes, List<MetadataType> types)
        {
            if (metadataType.Inherits != null)
            {
                var type = FindMetadataType(metadataTypes, metadataType.Inherits.Name, metadataType.Inherits.Namespace);
                if (type != null && !types.Contains(type))
                {
                    types.Add(type);
                    AddReferencedTypes(type, metadataTypes, types);
                }

                if (!metadataType.Inherits.GenericArgs.IsEmpty())
                {
                    foreach (var arg in metadataType.Inherits.GenericArgs)
                    {
                        type = FindMetadataType(metadataTypes, arg);
                        if (type != null && !types.Contains(type))
                        {
                            types.Add(type);
                            AddReferencedTypes(type, metadataTypes, types);
                        }
                    }
                }
            }

            if (metadataType.Properties != null)
            {
                foreach (var p in metadataType.Properties)
                {
                    var type = FindMetadataType(metadataTypes, p.Type, p.TypeNamespace);
                    if (type != null && !types.Contains(type))
                    {
                        types.Add(type);
                        AddReferencedTypes(type, metadataTypes, types);
                    }

                    if (!p.GenericArgs.IsEmpty())
                    {
                        foreach (var arg in p.GenericArgs)
                        {
                            type = FindMetadataType(metadataTypes, arg);
                            if (type != null && !types.Contains(type))
                            {
                                types.Add(type);
                                AddReferencedTypes(type, metadataTypes, types);
                            }
                        }
                    }
                    else if (p.IsArray())
                    {
                        var elType = p.Type.SplitOnFirst('[')[0];
                        type = FindMetadataType(metadataTypes, elType);
                        if (type != null && !types.Contains(type))
                        {
                            types.Add(type);
                            AddReferencedTypes(type, metadataTypes, types);
                        }
                    }
                }
            }
        }

        static MetadataType FindMetadataType(MetadataTypes metadataTypes, Type type)
        {
            return type == null ? null : FindMetadataType(metadataTypes, type.Name, type.Namespace);
        }

        static MetadataType FindMetadataType(MetadataTypes metadataTypes, string name, string @namespace = null)
        {
            if (@namespace != null && @namespace.StartsWith("System"))
                return null;

            var reqType = metadataTypes.Operations.FirstOrDefault(x => x.Request.Name == name);
            if (reqType != null)
                return reqType.Request;

            var resType = metadataTypes.Operations
                .FirstOrDefault(x => x.Response != null && x.Response.Name == name);

            if (resType != null)
                return resType.Response;

            var type = metadataTypes.Types.FirstOrDefault(x => x.Name == name
                && (@namespace == null || x.Namespace == @namespace));

            return type;
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
        public List<string> Routes { get; set; }
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

        public List<string> GetReplyOperationNames(Format format, HashSet<Type> soapTypes)
        {
            return Metadata.OperationsMap.Values
                .Where(x => HostContext.Config != null
                    && HostContext.MetadataPagesConfig.CanAccess(format, x.Name))
                .Where(x => !x.IsOneWay)
                .Where(x => soapTypes.Contains(x.RequestType))
                .Select(x => x.RequestType.GetOperationName())
                .ToList();
        }

        public List<string> GetOneWayOperationNames(Format format, HashSet<Type> soapTypes)
        {
            return Metadata.OperationsMap.Values
                .Where(x => HostContext.Config != null
                    && HostContext.MetadataPagesConfig.CanAccess(format, x.Name))
                .Where(x => x.IsOneWay)
                .Where(x => soapTypes.Contains(x.RequestType))
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
                Routes = operation.Routes.Map(x => x.Path),
            };
            
            if (operation.RestrictTo != null)
            {
                to.RestrictTo = operation.RestrictTo.AccessibleToAny.ToList().ConvertAll(x => x.ToString());
                to.VisibleTo = operation.RestrictTo.VisibleToAny.ToList().ConvertAll(x => x.ToString());
            }

            return to;
        }

        public static List<ApiMemberAttribute> GetApiMembers(this Type operationType)
        {
            var members = operationType.GetMembers(BindingFlags.Instance | BindingFlags.Public);
            var attrs = new List<ApiMemberAttribute>();
            foreach (var member in members)
            {
                var memattr = member.AllAttributes<ApiMemberAttribute>()
                    .Select(x => { x.Name = x.Name ?? member.Name; return x; });

                attrs.AddRange(memattr);
            }

            return attrs;
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

    public static class MetadataTypeExtensions
    {
        public static string GetParamType(this MetadataPropertyType prop, MetadataType type, Operation op)
        {
            if (prop.ParamType != null)
                return prop.ParamType;

            var isRequest = type.Name == op.RequestType.Name;

            return !isRequest ? "form" : GetRequestParamType(op, prop.Name);
        }

        public static string GetParamType(this ApiMemberAttribute attr, Type type, string verb)
        {
            if (attr.ParameterType != null)
                return attr.ParameterType;

            var op = HostContext.Metadata.GetOperation(type);
            var isRequestType = op != null;

            var defaultType = verb == HttpMethods.Post || verb == HttpMethods.Put
                ? "form"
                : "query";

            return !isRequestType ? defaultType : GetRequestParamType(op, attr.Name, defaultType);
        }

        private static string GetRequestParamType(Operation op, string name, string defaultType="body")
        {
            if (op.Routes.Any(x => x.IsVariable(name)))
                return "path";

            return !op.Routes.Any(x => x.Verbs.Contains(HttpMethods.Post) || x.Verbs.Contains(HttpMethods.Put))
                       ? "query"
                       : defaultType;
        }

        public static HashSet<string> CollectionTypes = new HashSet<string> {
            "List`1",
            "HashSet`1",
            "Dictionary`2",
            "Queue`1",
            "Stack`1",
        };

        public static bool IsCollection(this MetadataPropertyType prop)
        {
            return CollectionTypes.Contains(prop.Type)
                || IsArray(prop);
        }

        public static bool IsArray(this MetadataPropertyType prop)
        {
            return prop.Type.IndexOf('[') >= 0;
        }

        public static bool IsInterface(this MetadataType type)
        {
            return type != null && type.IsInterface.GetValueOrDefault();
        }

        public static bool IsAbstract(this MetadataType type)
        {
            return type.IsAbstract.GetValueOrDefault()
                || type.Name == typeof(AuthUserSession).Name; //not abstract but treat it as so
        }
    }
}
