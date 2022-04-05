#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using ServiceStack.Auth;
using ServiceStack.Configuration;
using ServiceStack.DataAnnotations;
using ServiceStack.FluentValidation;
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
            this.ForceInclude = new HashSet<Type>();
        }

        public Dictionary<Type, Operation> OperationsMap { get; protected set; }
        public Dictionary<Type, Operation> OperationsResponseMap { get; protected set; }
        public Dictionary<string, Operation> OperationNamesMap { get; protected set; }
        public HashSet<Type> RequestTypes { get; protected set; }
        public HashSet<Type> ServiceTypes { get; protected set; }
        public HashSet<Type> ResponseTypes { get; protected set; }
        private readonly List<RestPath> restPaths;
        
        public List<Action<Operation>> ConfigureOperations { get; protected set; } = new();
        public List<Action<MetadataType>> ConfigureMetadataTypes { get; protected set; } = new();

        public IEnumerable<Operation> Operations => OperationsMap.Values;
        
        public HashSet<Type> ForceInclude { get; set; }

        public void Add(Type serviceType, Type requestType, Type responseType)
        {
            if (requestType.IsArray) //Custom AutoBatched requests
            {
                this.ServiceTypes.Add(serviceType);
                return;
            }
            
            this.ServiceTypes.Add(serviceType);
            this.RequestTypes.Add(requestType);

            var restrictTo = requestType.FirstAttribute<RestrictAttribute>()
                          ?? serviceType.FirstAttribute<RestrictAttribute>();

            var reqFilterAttrs = new[] { requestType, serviceType }
                .SelectMany(x => x.AllAttributes().OfType<IRequestFilterBase>()).ToList();
            var resFilterAttrs = (responseType != null ? new[] { responseType, serviceType } : new[] { serviceType })
                .SelectMany(x => x.AllAttributes().OfType<IResponseFilterBase>()).ToList();

            var authAttrs = reqFilterAttrs.OfType<AuthenticateAttribute>().ToList();
            var actions = serviceType.GetRequestActions(requestType);
            var actionUpperNames = actions.Select(x => x.NameUpper).Distinct().ToList();
            authAttrs.AddRange(actions.SelectMany(x => x.AllAttributes<AuthenticateAttribute>()));
            var tagAttrs = requestType.AllAttributes<TagAttribute>().ToList();

            var operation = new Operation
            {
                ServiceType = serviceType,
                RequestType = requestType,
                ResponseType = responseType,
                RestrictTo = restrictTo,
                Actions = actionUpperNames,
                Method = ServiceClientUtils.GetHttpMethod(requestType) 
                         ?? actionUpperNames.FirstOrDefault(x => x != "ANY")
                         ?? HttpMethods.Post,
                Routes = new List<RestPath>(),
                RequestFilterAttributes = reqFilterAttrs,
                ResponseFilterAttributes = resFilterAttrs,
                RequiresAuthentication = authAttrs.Count > 0,
                RequiredRoles = authAttrs.OfType<RequiredRoleAttribute>().SelectMany(x => x.RequiredRoles).ToList(),
                RequiresAnyRole = authAttrs.OfType<RequiresAnyRoleAttribute>().SelectMany(x => x.RequiredRoles).ToList(),
                RequiredPermissions = authAttrs.OfType<RequiredPermissionAttribute>().SelectMany(x => x.RequiredPermissions).ToList(),
                RequiresAnyPermission = authAttrs.OfType<RequiresAnyPermissionAttribute>().SelectMany(x => x.RequiredPermissions).ToList(),
                RequestPropertyAttributes = requestType.GetPublicProperties().SelectMany(x => x.AllAttributes()).Map(x => x.GetType()).ToSet(),
                Tags = tagAttrs,
                LocodeCss = X.Map(requestType.FirstAttribute<LocodeCssAttribute>(), x => new ApiCss { Form = x.Form, Fieldset = x.Fieldset, Field = x.Field }),
                ExplorerCss = X.Map(requestType.FirstAttribute<ExplorerCssAttribute>(), x => new ApiCss { Form = x.Form, Fieldset = x.Fieldset, Field = x.Field }),
            };

            this.OperationsMap[requestType] = operation;
            this.OperationNamesMap[operation.Name.ToLowerInvariant()] = operation;
            if (responseType != null)
            {
                this.ResponseTypes.Add(responseType);
                this.OperationsResponseMap[responseType] = operation;
            }

            //Only count non-core ServiceStack Services, i.e. defined outside of ServiceStack.dll or Swagger
            var nonCoreServicesCount = OperationsMap.Values
                .Count(x => x.ServiceType.Assembly != typeof(Service).Assembly
                && x.ServiceType.FullName != "ServiceStack.Api.OpenApi.OpenApiService"
                && x.ServiceType.Name != "__AutoQueryServices"
                && x.ServiceType.Name != "__AutoQueryDataServices");

            LicenseUtils.AssertValidUsage(LicenseFeature.ServiceStack, QuotaType.Operations, nonCoreServicesCount);
        }

        public void AfterInit()
        {
            foreach (var restPath in restPaths)
            {
                if (!OperationsMap.TryGetValue(restPath.RequestType, out var operation))
                    continue;

                operation.Routes.Add(restPath);
            }

            foreach (var entry in OperationsMap)
            {
                foreach (var configure in ConfigureOperations)
                {
                    configure(entry.Value);
                }
            }
        }

        readonly HashSet<Assembly> excludeAssemblies = new()
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

        public List<Operation> GetOperationsByTag(string tag) => 
            Operations.Where(x => x.Tags.Any(t => t.Name == tag)).ToList();

        public List<Operation> GetOperationsByTags(string[] tags) => 
            Operations.Where(x => x.Tags.Any(t => Array.IndexOf(tags, t.Name) >= 0)).ToList();

        public Operation? GetOperation(Type? requestType)
        {
            if (requestType == null)
                return null;

            OperationsMap.TryGetValue(requestType, out var op);
            return op;
        }

        public List<ActionMethod> GetImplementedActions(Type serviceType, Type requestType)
        {
            if (!typeof(IService).IsAssignableFrom(serviceType))
                throw new NotSupportedException("All Services must implement IService");

            return serviceType.GetActions()
                .Where(x => x.GetParameters()[0].ParameterType == requestType)
                .ToList();
        }

        public Type? GetOperationType(string operationTypeName)
        {
            var opName = operationTypeName.ToLowerInvariant();
            if (!OperationNamesMap.TryGetValue(opName, out var operation))
            {
                var arrayPos = opName.LastIndexOf('[');
                if (arrayPos >= 0)
                {
                    opName = opName.Substring(0, arrayPos);
                    OperationNamesMap.TryGetValue(opName, out operation);
                    return operation?.RequestType.MakeArrayType();
                }
            }
            return operation?.RequestType;
        }

        public Type? GetServiceTypeByRequest(Type requestType)
        {
            OperationsMap.TryGetValue(requestType, out var operation);
            return operation?.ServiceType;
        }

        public Type? GetServiceTypeByResponse(Type responseType)
        {
            OperationsResponseMap.TryGetValue(responseType, out var operation);
            return operation?.ServiceType;
        }

        public Type? GetResponseTypeByRequest(Type requestType)
        {
            OperationsMap.TryGetValue(requestType, out var operation);
            return operation?.ResponseType;
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

        public List<string> GetAllOperationNames()
        {
            return Operations.Select(x => x.RequestType.GetOperationName()).OrderBy(operation => operation).ToList();
        }

        public List<string> GetOperationNamesForMetadata(IRequest httpReq)
        {
            return Operations
                .Where(x => !x.RequestType.ExcludesFeature(Feature.Metadata) || x.RequestType.ForceInclude())
                .Select(x => x.RequestType.GetOperationName()).OrderBy(operation => operation).ToList();
        }

        public List<string> GetOperationNamesForMetadata(IRequest httpReq, Format format)
        {
            var formatRequestAttr = format.ToRequestAttribute();
            return Operations
                .Where(x => !x.RequestType.ExcludesFeature(Feature.Metadata) && x.RestrictTo.CanShowTo(formatRequestAttr) || x.RequestType.ForceInclude())
                .Select(x => x.RequestType.GetOperationName()).OrderBy(operation => operation).ToList();
        }

        public bool IsAuthorized(Operation operation, IRequest req, IAuthSession session)
        {
            if (HostContext.HasValidAuthSecret(req))
                return true;

            if (operation.RequiresAuthentication && !session.IsAuthenticated)
                return false;

            var authRepo = HostContext.AppHost.GetAuthRepository(req);
            using (authRepo as IDisposable)
            {
                var allRoles = session.GetRoles(authRepo);
                if (!operation.RequiredRoles.IsEmpty() && !operation.RequiredRoles.All(allRoles.Contains))
                    return false;

                var allPerms = session.GetPermissions(authRepo);
                if (!operation.RequiredPermissions.IsEmpty() && !operation.RequiredPermissions.All(allPerms.Contains))
                    return false;

                if (!operation.RequiresAnyRole.IsEmpty() && !operation.RequiresAnyRole.Any(allRoles.Contains))
                    return false;

                if (!operation.RequiresAnyPermission.IsEmpty() && !operation.RequiresAnyPermission.Any(allPerms.Contains))
                    return false;

                return true;
            }
        }

        public async Task<bool> IsAuthorizedAsync(Operation operation, IRequest req, IAuthSession session)
        {
            if (HostContext.HasValidAuthSecret(req))
                return true;

            if (operation.RequiresAuthentication && !session.IsAuthenticated)
                return false;

            var authRepo = HostContext.AppHost.GetAuthRepositoryAsync(req);
            await using (authRepo as IAsyncDisposable)
            {
                var allRoles = await session.GetRolesAsync(authRepo).ConfigAwait();
                if (!operation.RequiredRoles.IsEmpty() && !operation.RequiredRoles.All(allRoles.Contains))
                    return false;

                var allPerms = await session.GetPermissionsAsync(authRepo).ConfigAwait();
                if (!operation.RequiredPermissions.IsEmpty() && !operation.RequiredPermissions.All(allPerms.Contains))
                    return false;

                if (!operation.RequiresAnyRole.IsEmpty() && !operation.RequiresAnyRole.Any(allRoles.Contains))
                    return false;

                if (!operation.RequiresAnyPermission.IsEmpty() && !operation.RequiresAnyPermission.Any(allPerms.Contains))
                    return false;

                return true;
            }
        }

        public bool IsVisible(IRequest httpReq, Operation operation)
        {
            if (HostContext.Config is { EnableAccessRestrictions: false } || operation.RequestType.ForceInclude())
                return true;

            if (operation.RequestType.ExcludesFeature(Feature.Metadata))
                return false;

            if (operation.RestrictTo == null) return true;

            //Less fine-grained on /metadata pages. Only check Network and Format
            var reqAttrs = httpReq.GetAttributes();
            var showToNetwork = CanShowToNetwork(operation.RestrictTo, reqAttrs);
            return showToNetwork;
        }

        public bool IsVisible(IRequest httpReq, Type requestType)
        {
            if (HostContext.Config is { EnableAccessRestrictions: false })
                return true;

            var operation = HostContext.Metadata.GetOperation(requestType);
            return operation == null || IsVisible(httpReq, operation);
        }

        public bool IsVisible(IRequest httpReq, Format format, string operationName)
        {
            if (HostContext.Config is { EnableAccessRestrictions: false })
                return true;

            OperationNamesMap.TryGetValue(operationName.ToLowerInvariant(), out var operation);
            if (operation == null) return false;

            if (operation.RequestType.ForceInclude())
                return true;

            if (operation.RequestType.ExcludesFeature(Feature.Metadata)) 
                return false;

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
            if (HostContext.Config is { EnableAccessRestrictions: false })
                return true;

            OperationNamesMap.TryGetValue(operationName.ToLowerInvariant(), out var operation);
            if (operation == null) 
                return false;

            var canCall = HasImplementation(operation, format);
            if (!canCall) 
                return false;

            if (operation.RestrictTo == null || operation.RequestType.ForceInclude()) 
                return true;

            var allow = operation.RestrictTo.HasAccessTo(reqAttrs);
            if (!allow) 
                return false;

            var allowsFormat = operation.RestrictTo.HasAccessTo((RequestAttributes)(long)format);
            return allowsFormat;
        }

        public bool CanAccess(Format format, string operationName)
        {
            if (HostContext.Config is { EnableAccessRestrictions: false })
                return true;

            OperationNamesMap.TryGetValue(operationName.ToLowerInvariant(), out var operation);
            if (operation == null) 
                return false;

            var canCall = HasImplementation(operation, format);
            if (!canCall) 
                return false;

            if (operation.RestrictTo == null || operation.RequestType.ForceInclude()) 
                return true;

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

        private HashSet<Type>? allDtos;
        public HashSet<Type> GetAllDtos()
        {
            if (allDtos != null)
                return allDtos;
            
            var to = new HashSet<Type>();
            var ops = OperationsMap.Values;
            foreach (var op in ops)
            {
                AddReferencedTypes(to, op.RequestType);
                AddReferencedTypes(to, op.ResponseType);
            }
            return allDtos = to;
        }

        private Dictionary<string, Type>? dtoTypesMap;
        private HashSet<string> duplicateTypeNames;
        public Type? FindDtoType(string typeName)
        {
            var opType = GetOperationType(typeName ?? throw new ArgumentNullException(nameof(typeName)));
            if (opType != null)
                return opType;

            if (dtoTypesMap == null)
            {
                var typesMap = new Dictionary<string, Type>();
                duplicateTypeNames = new HashSet<string>();

                foreach (var dto in GetAllDtos())
                {
                    if (typesMap.ContainsKey(dto.Name))
                    {
                        duplicateTypeNames.Add(dto.Name);
                        continue;
                    }
                    typesMap[dto.Name] = dto;
                }
                dtoTypesMap = typesMap;
            }

            if (duplicateTypeNames.Contains(typeName))
                throw new Exception($"There are multiple DTO Types named '{typeName}'");
                
            dtoTypesMap.TryGetValue(typeName, out var dtoType);
            return dtoType;
        }

        public RestPath? FindRoute(string pathInfo, string method = HttpMethods.Get)
        {
            var route = RestHandler.FindMatchingRestPath(method, pathInfo, out _);
            return route as RestPath;
        }

        public object CreateRequestFromUrl(string relativeOrAbsoluteUrl, string method = HttpMethods.Get)
        {
            var relativeUrl = relativeOrAbsoluteUrl.StartsWith("http:") || relativeOrAbsoluteUrl.StartsWith("https:")
                ? relativeOrAbsoluteUrl.RightPart("://").RightPart("/")
                : relativeOrAbsoluteUrl;

            if (!relativeUrl.StartsWith("/"))
                relativeUrl = "/" + relativeUrl;
            
            var parts = relativeUrl.SplitOnFirst("?");
            var pathInfo = parts[0];

            var route = FindRoute(pathInfo, method);
            if (route == null)
                throw new ArgumentException($"No matching route found for path {method} '{pathInfo}'");

            Dictionary<string, string> query = null;
            if (parts.Length == 2)
            {
                query = new Dictionary<string, string>();
                var qs = parts[1];
                var qsParts = qs.Split('&');
                foreach (var qsPart in qsParts)
                {
                    var kvp = qsPart.SplitOnFirst("=");
                    if (kvp.Length == 1) continue;
                    query[kvp[0]] = kvp[1].UrlDecode();
                }
            }

            var requestDto = route.CreateRequest(pathInfo, query, route.RequestType.CreateInstance());
            return requestDto;
        }

        public static void AddReferencedTypes(HashSet<Type> to, Type? type)
        {
            if (type == null || to.Contains(type) || !IsDtoType(type))
                return;

            to.Add(type);

            var baseType = type.BaseType;
            if (baseType != null && IsDtoType(baseType) && !to.Contains(baseType))
            {
                AddReferencedTypes(to, baseType);

                var genericArgs = type.IsGenericType
                    ? type.GetGenericArguments()
                    : Type.EmptyTypes;

                foreach (var arg in genericArgs)
                {
                    AddReferencedTypes(to, arg);
                }
            }

            foreach (var iface in type.GetInterfaces())
            {
                if (iface.IsGenericType && !iface.IsGenericTypeDefinition)
                {
                    foreach (var arg in iface.GetGenericArguments())
                    {
                        AddReferencedTypes(to, arg);
                    }
                }
            }

            foreach (var pi in type.GetSerializableProperties())
            {
                if (to.Contains(pi.PropertyType))
                    continue;
                
                if (IsDtoType(pi.PropertyType))
                    to.Add(pi.PropertyType);

                var genericArgs = pi.PropertyType.IsGenericType
                    ? pi.PropertyType.GetGenericArguments()
                    : Type.EmptyTypes;

                if (genericArgs.Length > 0)
                {
                    foreach (var arg in genericArgs)
                    {
                        AddReferencedTypes(to, arg);
                    }
                }
                else if (pi.PropertyType.IsArray)
                {
                    var elType = pi.PropertyType.HasElementType ? pi.PropertyType.GetElementType() : null;
                    AddReferencedTypes(to, elType);
                }
            }
        }

        public static bool IsDtoType(Type? type) => type != null &&
            type.Namespace?.StartsWith("System") == false &&
            type.IsClass && type != typeof(string) &&
            !type.IsGenericType &&
            !type.IsArray &&
            !type.HasInterface(typeof(IService));

        public List<MetadataType> GetMetadataTypesForOperation(IRequest httpReq, Operation op)
        {
            var typeMetadata = HostContext.TryResolve<INativeTypesMetadata>();

            var typesConfig = HostContext.AppHost.GetTypesConfigForMetadata(httpReq);

            if (HostContext.GetPlugin<MetadataFeature>().ShowResponseStatusInMetadataPages)
            {
                typesConfig.IgnoreTypes.Remove(typeof(ResponseStatus));
                typesConfig.IgnoreTypes.Remove(typeof(ResponseError));
            }

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
                x.DisplayType ??= generator.Type(x.Name, x.GenericArgs);
                x.Properties.Each(p =>
                    p.DisplayType ??= generator.Type(p.Type, p.GenericArgs));
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
                    var type = FindMetadataType(metadataTypes, p.Type, p.Namespace);
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
                        var elType = p.Type.LeftPart('[');
                        type = FindMetadataType(metadataTypes, elType, p.Namespace);
                        if (type != null && !types.Contains(type))
                        {
                            types.Add(type);
                            AddReferencedTypes(type, metadataTypes, types);
                        }
                    }
                }
            }
        }

        static MetadataType? FindMetadataType(MetadataTypes metadataTypes, Type? type)
        {
            return type == null ? null : FindMetadataType(metadataTypes, type.Name, type.Namespace);
        }

        static MetadataType? FindMetadataType(MetadataTypes metadataTypes, string name, string? @namespace = null)
        {
            if (@namespace != null 
                && @namespace.StartsWith("System") 
                && metadataTypes.Config.ExportTypes.All(x => x.Name != name))
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
        
#if !NETCORE
        public List<Type> GetAllSoapOperationTypes()
        {
            var operationTypes = GetAllOperationTypes();
            var soapTypes = HostContext.AppHost.ExportSoapOperationTypes(operationTypes);
            return soapTypes;
        }
#endif

        public List<string> GetAllRoles()
        {
            var to = new List<string> {
                RoleNames.Admin
            };

            foreach (var op in OperationsMap.Values)
            {
                op.RequiredRoles.Each(x => to.AddIfNotExists(x));
                op.RequiresAnyRole.Each(x => to.AddIfNotExists(x));
            }

            return to;
        }

        public List<string> GetAllPermissions()
        {
            var to = new List<string> {
            };

            foreach (var op in OperationsMap.Values)
            {
                op.RequiredPermissions.Each(x => to.AddIfNotExists(x));
                op.RequiresAnyPermission.Each(x => to.AddIfNotExists(x));
            }

            return to;
        }
        
        public object CreateRequestDto(Type requestType, object? dto)
        {
            if (dto == null)
                return requestType.CreateInstance();
            
            var requestDto = dto.GetType() == requestType
                ? dto
                : dto is Dictionary<string, object> objDictionary
                    ? objDictionary.FromObjectDictionary(requestType)
                    : dto.ConvertTo(requestType);
            return requestDto;
        }
        
    }

    public class Operation : ICloneable
    {
        public string Name => RequestType.GetOperationName();

        public Type RequestType { get; set; }
        public Type ServiceType { get; set; }
        public Type? ResponseType { get; set; }
        public Type DataModelType => AutoCrudOperation.GetModelType(RequestType);
        public Type ViewModelType => AutoCrudOperation.GetViewModelType(RequestType, ResponseType);
        public RestrictAttribute? RestrictTo { get; set; }
        public List<string>? Actions { get; set; }
        public bool ReturnsVoid => ResponseType == null;
        public bool IsOneWay => ResponseType == null;
        public string Method { get; set; }
        public List<RestPath>? Routes { get; set; }
        public List<IRequestFilterBase>? RequestFilterAttributes { get; set; }
        public List<IResponseFilterBase>? ResponseFilterAttributes { get; set; }
        public bool RequiresAuthentication { get; set; }
        public List<string>? RequiredRoles { get; set; }
        public List<string>? RequiresAnyRole { get; set; }
        public List<string>? RequiredPermissions { get; set; }
        public List<string>? RequiresAnyPermission { get; set; }
        public List<TagAttribute>? Tags { get; set; }
        public ApiCss? LocodeCss { get; set; } 
        public ApiCss? ExplorerCss { get; set; } 
        public List<InputInfo>? FormLayout { get; set; }
        public HashSet<Type>? RequestPropertyAttributes { get; set; }

        public List<ITypeValidator>? RequestTypeValidationRules { get; private set; }
        public List<IValidationRule>? RequestPropertyValidationRules { get; private set; }

        object ICloneable.Clone() => Clone();
        public Operation Clone() => new() {
            RequestType = RequestType,
            ServiceType = ServiceType,
            ResponseType = ResponseType,
            RestrictTo = RestrictTo,
            Actions = Actions?.ToList(),
            Method = Method,
            Routes = Routes?.ToList(),
            RequestFilterAttributes = RequestFilterAttributes,
            RequiresAuthentication = RequiresAuthentication,
            RequiredRoles = RequiredRoles?.ToList(),
            RequiresAnyRole = RequiresAnyRole?.ToList(),
            RequiredPermissions = RequiredPermissions?.ToList(),
            RequiresAnyPermission = RequiresAnyPermission?.ToList(),
            RequestTypeValidationRules = RequestTypeValidationRules?.ToList(),
            RequestPropertyValidationRules = RequestPropertyValidationRules?.ToList(),
            RequestPropertyAttributes = RequestPropertyAttributes,
            Tags = Tags?.ToList(),
            LocodeCss = LocodeCss,
            ExplorerCss = ExplorerCss,
            FormLayout = FormLayout,
        };

        public void AddRequestTypeValidationRules(List<ITypeValidator> typeValidators)
        {
            if (typeValidators != null)
            {
                RequestTypeValidationRules ??= new List<ITypeValidator>();
                RequestTypeValidationRules.AddRange(typeValidators);

                var authValidators = typeValidators.OfType<IAuthTypeValidator>().ToList();
                if (authValidators.Count > 0)
                {
                    RequiresAuthentication = true;

                    var rolesValidators = authValidators.OfType<HasRolesValidator>();
                    foreach (var validator in rolesValidators)
                    {
                        RequiredRoles ??= new List<string>();
                        validator.Roles.Each(x => RequiredRoles.AddIfNotExists(x));
                    }

                    var permsValidators = authValidators.OfType<HasPermissionsValidator>();
                    foreach (var validator in permsValidators)
                    {
                        RequiredPermissions ??= new List<string>();
                        validator.Permissions.Each(x => RequiredPermissions.AddIfNotExists(x));
                    }
                }
            }
        }

        public void AddRequestPropertyValidationRules(List<IValidationRule>? propertyValidators)
        {
            if (propertyValidators != null)
            {
                RequestPropertyValidationRules ??= new List<IValidationRule>();
                RequestPropertyValidationRules.AddRange(propertyValidators);
            }
        }

        public Operation AddRole(string role)
        {
            RequiredRoles.AddIfNotExists(role);
            RequiresAuthentication = true;
            return this;
        }

        public Operation AddPermission(string permission)
        {
            RequiredPermissions.AddIfNotExists(permission);
            RequiresAuthentication = true;
            return this;
        }
    }

    public class OperationDto
    {
        public string Name { get; set; }
        public string? ResponseName { get; set; }
        public string ServiceName { get; set; }
        public List<string>? RestrictTo { get; set; }
        public List<string>? VisibleTo { get; set; }
        public List<string>? Actions { get; set; }
        public List<string>? Routes { get; set; }
        public List<string>? Tags { get; set; }
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
                .Where(x => !x.ReturnsVoid)
                .Where(x => soapTypes.Contains(x.RequestType))
                .Select(x => x.RequestType.GetOperationName())
                .ToList();
        }

        public List<string> GetOneWayOperationNames(Format format, HashSet<Type> soapTypes)
        {
            return Metadata.OperationsMap.Values
                .Where(x => HostContext.Config != null
                    && HostContext.MetadataPagesConfig.CanAccess(format, x.Name))
                .Where(x => x.ReturnsVoid)
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
            var to = new OperationDto
            {
                Name = operation.Name,
                ResponseName = operation.ReturnsVoid ? null : operation.ResponseType.GetOperationName(),
                ServiceName = operation.ServiceType.GetOperationName(),
                Actions = operation.Actions,
                Routes = operation.Routes.Map(x => x.Path),
                Tags = operation.Tags.Map(x => x.Name),
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
                var attr = member.AllAttributes<ApiMemberAttribute>()
                    .Select(x => { x.Name ??= member.Name; return x; });

                attrs.AddRange(attr);
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

        private static string GetRequestParamType(Operation op, string name, string defaultType = "body")
        {
            if (op.Routes == null || op.Routes.Count == 0)
                return "query";
            
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

        public static bool IsCollection(this MetadataPropertyType prop) => 
            CollectionTypes.Contains(prop.Type) || IsArray(prop);

        public static bool IsArray(this MetadataPropertyType prop) => 
            prop.Type.IndexOf('[') >= 0;

        public static bool IsInterface(this MetadataType? type) => 
            type != null && type.IsInterface.GetValueOrDefault();

        public static bool IsAbstract(this MetadataType type) => 
            type.IsAbstract.GetValueOrDefault() || type.Name == nameof(AuthUserSession);

        public static bool ExcludesFeature(this Type type, Feature feature) => 
            type.FirstAttribute<ExcludeAttribute>()?.Feature.Has(feature) == true && !HostContext.Metadata.ForceInclude.Contains(type);

        public static bool Has(this Feature feature, Feature flag) => 
            (flag & feature) != 0;

        public static bool? NullIfFalse(this bool value) => value ? true : (bool?)null;
        public static List<T>? NullIfEmpty<T>(this List<T> value) => value?.Count > 0 ? value : null;
        public static int? NullIfMinValue(this int value) => value != int.MinValue ? value : (int?)null;

        public static Dictionary<string, string[]> ToMetadataServiceRoutes(this Dictionary<Type, string[]> serviceRoutes,
            Action<Dictionary<string,string[]>>? filter=null)
        {
            var to = new Dictionary<string,string[]>();
            foreach (var entry in serviceRoutes.Safe())
            {
                to[entry.Key.Name] = entry.Value;
            }
            filter?.Invoke(to);
            return to;
        }

        public static bool ForceInclude(this MetadataTypesConfig config, Type type) =>
            HostContext.Metadata.ForceInclude.Contains(type);

        public static bool ForceInclude(this MetadataTypesConfig config, MetadataType type) =>
            HostContext.Metadata.ForceInclude.Any(x =>
                type.Type != null
                    ? x == type.Type
                    : type.Name == x.Name && type.Namespace == x.Namespace);

        internal static bool ForceInclude(this Type type) => HostContext.Metadata.ForceInclude.Contains(type);
    }
}
