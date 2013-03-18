using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using ServiceStack.Common;
using ServiceStack.Text;
using ServiceStack.WebHost.Endpoints;
using ServiceStack.WebHost.Endpoints.Extensions;

namespace ServiceStack.ServiceHost
{
    public class ServiceMetadata
    {
        public ServiceMetadata()
        {
            this.RequestTypes = new HashSet<Type>();
            this.ServiceTypes = new HashSet<Type>();
            this.ResponseTypes = new HashSet<Type>();
            this.OperationsMap = new Dictionary<Type, Operation>();
            this.OperationsResponseMap = new Dictionary<Type, Operation>();
            this.OperationNamesMap = new Dictionary<string, Operation>();
            this.Routes = new ServiceRoutes();
        }

        public Dictionary<Type, Operation> OperationsMap { get; protected set; }
        public Dictionary<Type, Operation> OperationsResponseMap { get; protected set; }
        public Dictionary<string, Operation> OperationNamesMap { get; protected set; }
        public HashSet<Type> RequestTypes { get; protected set; }
        public HashSet<Type> ServiceTypes { get; protected set; }
        public HashSet<Type> ResponseTypes { get; protected set; }
        public ServiceRoutes Routes { get; set; }

        public IEnumerable<Operation> Operations
        {
            get { return OperationsMap.Values; }
        }

        public void Add(Type serviceType, Type requestType, Type responseType)
        {
            this.ServiceTypes.Add(serviceType);
            this.RequestTypes.Add(requestType);

            var restrictTo = requestType.GetCustomAttributes(true)
                    .OfType<RestrictAttribute>().FirstOrDefault()
                ?? serviceType.GetCustomAttributes(true)
                    .OfType<RestrictAttribute>().FirstOrDefault();

            var operation = new Operation {
                ServiceType = serviceType,
                RequestType = requestType,
                ResponseType = responseType,
                RestrictTo = restrictTo,
                Actions = GetImplementedActions(serviceType, requestType),
                Routes = new List<RestPath>(),
            };

            this.OperationsMap[requestType] = operation;
            this.OperationNamesMap[requestType.Name.ToLower()] = operation;

            if (responseType != null)
            {
                this.ResponseTypes.Add(responseType);
                this.OperationsResponseMap[responseType] = operation;
            }
        }

        public void AfterInit()
        {
            foreach (var restPath in Routes.RestPaths)
            {
                Operation operation;
                if (!OperationsMap.TryGetValue(restPath.RequestType, out operation))
                    continue;

                operation.Routes.Add(restPath);
            }
        }

        public List<OperationDto> GetOperationDtos()
        {
            return OperationsMap.Values
                .SafeConvertAll(x => x.ToOperationDto())
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
            if (typeof(IService).IsAssignableFrom(serviceType))
            {
                return serviceType.GetActions()
                    .Where(x => x.GetParameters()[0].ParameterType == requestType)
                    .Select(x => x.Name.ToUpper())
                    .ToList();
            }

            var oldApiActions = serviceType
                .GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                .Select(x => ToNewApiAction(x.Name))
                .Where(x => x != null)
                .ToList();
            return oldApiActions;
        }

        public static string ToNewApiAction(string oldApiAction)
        {
            switch (oldApiAction)
            {
                case "Get":
                case "OnGet":
                    return "GET";
                case "Put":
                case "OnPut":
                    return "PUT";
                case "Post":
                case "OnPost":
                    return "POST";
                case "Delete":
                case "OnDelete":
                    return "DELETE";
                case "Patch":
                case "OnPatch":
                    return "PATCH";
                case "Execute":
                case "Run":
                    return "ANY";
            }
            return null;
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
            return Operations.Select(x => x.RequestType.Name).ToList();
        }

        public List<string> GetOperationNamesForMetadata(IHttpRequest httpReq)
        {
            return Operations.Select(x => x.RequestType.Name).ToList();
        }

        public List<string> GetOperationNamesForMetadata(IHttpRequest httpReq, Format format)
        {
            return Operations.Select(x => x.RequestType.Name).ToList();
        }

        public bool IsVisible(IHttpRequest httpReq, Operation operation)
        {
            if (EndpointHost.Config != null && !EndpointHost.Config.EnableAccessRestrictions)
                return true;

            if (operation.RestrictTo == null) return true;

            //Less fine-grained on /metadata pages. Only check Network and Format
            var reqAttrs = httpReq.GetAttributes();
            var showToNetwork = CanShowToNetwork(operation, reqAttrs);
            return showToNetwork;
        }

        public bool IsVisible(IHttpRequest httpReq, Format format, string operationName)
        {
            if (EndpointHost.Config != null && !EndpointHost.Config.EnableAccessRestrictions)
                return true;

            Operation operation;
            OperationNamesMap.TryGetValue(operationName.ToLower(), out operation);
            if (operation == null) return false;

            var canCall = HasImplementation(operation, format);
            if (!canCall) return false;

            var isVisible = IsVisible(httpReq, operation);
            if (!isVisible) return false;

            if (operation.RestrictTo == null) return true;
            var allowsFormat = operation.RestrictTo.CanShowTo((EndpointAttributes)(long)format);
            return allowsFormat;
        }

        public bool CanAccess(IHttpRequest httpReq, Format format, string operationName)
        {
            var reqAttrs = httpReq.GetAttributes();
            return CanAccess(reqAttrs, format, operationName);
        }

        public bool CanAccess(EndpointAttributes reqAttrs, Format format, string operationName)
        {
            if (EndpointHost.Config != null && !EndpointHost.Config.EnableAccessRestrictions)
                return true;

            Operation operation;
            OperationNamesMap.TryGetValue(operationName.ToLower(), out operation);
            if (operation == null) return false;

            var canCall = HasImplementation(operation, format);
            if (!canCall) return false;

            if (operation.RestrictTo == null) return true;

            var allow = operation.RestrictTo.HasAccessTo(reqAttrs);
            if (!allow) return false;

            var allowsFormat = operation.RestrictTo.HasAccessTo((EndpointAttributes)(long)format);
            return allowsFormat;
        }

        public bool CanAccess(Format format, string operationName)
        {
            if (EndpointHost.Config != null && !EndpointHost.Config.EnableAccessRestrictions)
                return true;

            Operation operation;
            OperationNamesMap.TryGetValue(operationName.ToLower(), out operation);
            if (operation == null) return false;

            var canCall = HasImplementation(operation, format);
            if (!canCall) return false;

            if (operation.RestrictTo == null) return true;

            var allowsFormat = operation.RestrictTo.HasAccessTo((EndpointAttributes)(long)format);
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

        private static bool CanShowToNetwork(Operation operation, EndpointAttributes reqAttrs)
        {
            if (reqAttrs.IsLocalhost())
                return operation.RestrictTo.CanShowTo(EndpointAttributes.Localhost)
                       || operation.RestrictTo.CanShowTo(EndpointAttributes.LocalSubnet);

            return operation.RestrictTo.CanShowTo(
                reqAttrs.IsLocalSubnet()
                    ? EndpointAttributes.LocalSubnet
                    : EndpointAttributes.External);
        }

    }

    public class Operation
    {
        public string Name { get { return RequestType.Name; } }
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
            return Metadata.OperationsMap.Values
                .Where(x => EndpointHost.Config != null
                    && EndpointHost.Config.MetadataPagesConfig.CanAccess(format, x.Name))
                .Where(x => !x.IsOneWay)
                .Select(x => x.RequestType.Name)
                .ToList();
        }

        public List<string> GetOneWayOperationNames(Format format)
        {
            return Metadata.OperationsMap.Values
                .Where(x => EndpointHost.Config != null
                    && EndpointHost.Config.MetadataPagesConfig.CanAccess(format, x.Name))
                .Where(x => x.IsOneWay)
                .Select(x => x.RequestType.Name)
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
                if (baseType.Name == type.Name)
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
                ResponseName = operation.IsOneWay ? null : operation.ResponseType.Name,
                ServiceName = operation.ServiceType.Name,
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

        public static string GetDescription(this Type operationType)
        {
            var apiAttr = operationType.GetCustomAttributes(typeof(ApiAttribute), true).OfType<ApiAttribute>().FirstOrDefault();
            return apiAttr != null ? apiAttr.Description : "";
        }

        public static List<ApiMemberAttribute> GetApiMembers(this Type operationType)
        {
            var attrs = operationType
                .GetMembers(BindingFlags.Instance | BindingFlags.Public)
                .SelectMany(x =>
                    x.GetCustomAttributes(typeof(ApiMemberAttribute), true).OfType<ApiMemberAttribute>()
                )
                .ToList();

            return attrs;
        }
    }


}