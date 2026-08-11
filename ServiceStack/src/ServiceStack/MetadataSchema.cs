#nullable enable
#if NET8_0_OR_GREATER

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using ServiceStack.DataAnnotations;
using ServiceStack.Host;
using ServiceStack.Web;

using JsonObject = System.Text.Json.Nodes.JsonObject;
using JsonArray = System.Text.Json.Nodes.JsonArray;

namespace ServiceStack;

/// <summary>
/// The {Name} these routes take is the name in the path, not the bound property: a query string
/// is where these pages put their own arguments, so an API with its own `Name` property - or an
/// AutoQuery filtering on a Name column - would otherwise bind over the route and look up the
/// wrong schema entirely.
/// </summary>
public static class SchemaRoute
{
    public static string RouteName(IRequest req, string? bound)
    {
        var path = req.PathInfo?.TrimEnd('/');
        if (string.IsNullOrEmpty(path))
            return bound!;

        var last = path.LastRightPart('/');
        // /schema/QueryBookings.json asks for the same schema as /schema/QueryBookings
        var name = last.LeftPart('.');
        return string.IsNullOrEmpty(name) ? bound! : name;
    }
}

[DefaultRequest(typeof(MetadataSchemas))]
[Restrict(VisibilityTo = RequestAttributes.None)]
public class MetadataSchemasService() : Service
{
    public async Task<string> Any(MetadataSchemas request)
    {
        var authApi = await Gateway.ApiAsync(new Authenticate());
        var auth = authApi.Response;
        // one entry per Data Model whose Query API this Session is allowed to call,
        // grouping its CRUD APIs so the card can show what's available and what it needs
        var details = MetadataSchemaGenerator.CreateApis(Request!, auth);
        var detailsJson = details.ToJsonString();
        
        if (Request!.IsHtml())
        {
            var htmlFile = VirtualFileSources.GetFile("Templates/schemas.html")
                           ?? throw HttpError.NotFound("Templates/schemas.html not found");

            var html = await htmlFile.ReadAllTextAsync();
            html = html.Replace("${Results}", detailsJson);
            html = html.Replace("${Auth}", auth.ToJson() ?? "null");
            
            Response!.ContentType = MimeTypes.Html;
            return html;
        }

        Response!.ContentType = MimeTypes.Json;
        return detailsJson;
    }
}

[DefaultRequest(typeof(MetadataSchema))]
[Restrict(VisibilityTo = RequestAttributes.None)]
public class MetadataSchemaService() : Service
{
    public async Task<string> Any(MetadataSchema request)
    {
        var name = SchemaRoute.RouteName(Request!, request?.Name);
        if (string.IsNullOrEmpty(name))
            throw HttpError.NotFound("Schema name is required");

        var requestDto = HostContext.Metadata.GetRequestType(name)
            ?? HostContext.Metadata.RequestTypes.FirstOrDefault(x => x.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
        
        if (requestDto == null)
            throw HttpError.NotFound($"Request DTO '{name}' was not found");

        var schema = MetadataSchemaGenerator.CreateSchema(requestDto);
        
        if (Request!.IsHtml())
        {
            var htmlFile = VirtualFileSources.GetFile("Templates/schema.html")
                ?? throw HttpError.NotFound("Templates/schema.html not found");

            var authApi = await Gateway.ApiAsync(new Authenticate());
            var auth = authApi.Response;

            var html = await htmlFile.ReadAllTextAsync();
            html = html.Replace("${Title}", $"{name} Schema");
            html = html.Replace("${Schema}", schema.ToJsonString());
            html = html.Replace("${Auth}", auth.ToJson() ?? "null");
            
            Response!.ContentType = MimeTypes.Html;
            return html;
        }

        Response!.ContentType = MimeTypes.Json;
        return schema.ToJsonString();
    }
}

[DefaultRequest(typeof(AutoQuerySchemas))]
[Restrict(VisibilityTo = RequestAttributes.None)]
public class AutoQuerySchemasService() : Service
{
    public async Task<string> Any(AutoQuerySchemas request)
    {
        var authApi = await Gateway.ApiAsync(new Authenticate());
        var auth = authApi.Response;
        // one entry per Data Model whose Query API this Session is allowed to call,
        // grouping its CRUD APIs so the card can show what's available and what it needs
        var details = MetadataSchemaGenerator.CreateAutoQueryModels(Request!, auth);
        var detailsJson = details.ToJsonString();
        
        if (Request!.IsHtml())
        {
            var htmlFile = VirtualFileSources.GetFile("Templates/autos.html")
                ?? throw HttpError.NotFound("Templates/autos.html not found");

            var html = await htmlFile.ReadAllTextAsync();
            html = html.Replace("${Results}", detailsJson);
            html = html.Replace("${Auth}", auth.ToJson() ?? "null");
            
            Response!.ContentType = MimeTypes.Html;
            return html;
        }

        Response!.ContentType = MimeTypes.Json;
        return detailsJson;
    }
}

[DefaultRequest(typeof(AutoQuerySchema))]
[Restrict(VisibilityTo = RequestAttributes.None)]
public class AutoQuerySchemaService() : Service
{
    public async Task<string> Any(AutoQuerySchema request)
    {
        var name = SchemaRoute.RouteName(Request!, request?.Name);
        if (string.IsNullOrEmpty(name))
            throw HttpError.NotFound("Schema name is required");

        var authApi = await Gateway.ApiAsync(new Authenticate());
        var auth = authApi.Response;

        var schema = MetadataSchemaGenerator.CreateAutoQuerySchema(name, Request!, auth);
        var jsonSchema = schema.ToJsonString();
        if (Request!.IsHtml())
        {
            var htmlFile = VirtualFileSources.GetFile("Templates/auto.html")
                ?? throw HttpError.NotFound("Templates/auto.html not found");
            
            var html = await htmlFile.ReadAllTextAsync();
            html = html.Replace("${Title}", schema.TryGetPropertyValue("title", out var title) ? title?.ToString() ?? name : name);
            html = html.Replace("${Schema}", jsonSchema);
            html = html.Replace("${Auth}", auth.ToJson() ?? "null");
            
            Response!.ContentType = MimeTypes.Html;
            return html;
        }

        Response!.ContentType = MimeTypes.Json;
        return jsonSchema;
    }
}

public class CrudApi
{
    public required Operation Operation { get; init; }
    public required Type ModelType { get; init; }
    public required string Crud { get; init; }
}

public static class MetadataSchemaGenerator
{
    /// <summary>
    /// Returns all AutoQuery/CRUD Schemas available for a Data Model, e.g:
    /// { "query":{...}, "create":{...}, "update":{...}, "delete":{...} }
    /// Only includes the APIs this Request is authorized to call, and requires
    /// access to the Query API to see the Data Model at all.
    /// </summary>
    /// <param name="name">Data Model (e.g. Booking) or AutoQuery Request DTO (e.g. QueryBookings) name</param>
    /// <param name="req">Only include APIs visible and authorized for this Request</param>
    /// <param name="auth">The Authenticated User's Info</param>
    public static JsonObject CreateAutoQuerySchema(string name, IRequest req, AuthenticateResponse? auth = null)
    {
        // resolve against every visible API so a Data Model that exists but is off-limits
        // reports 401/403 instead of masquerading as 404
        var visibleApis = GetVisibleCrudApis(req);
        var modelType = visibleApis.FirstOrDefault(x => x.ModelType.Name.EqualsIgnoreCase(name))?.ModelType
            ?? visibleApis.FirstOrDefault(x => x.Operation.Name.EqualsIgnoreCase(name))?.ModelType
            ?? throw HttpError.NotFound($"No AutoQuery APIs were found for '{name}'");

        var crudApis = GetAuthorizedCrudApis(req, auth);
        var modelApis = crudApis.Where(x => x.ModelType == modelType).ToList();
        var queryApi = FindCrud(modelApis, AutoCrudOperation.Query);

        // Viewing the Schema requires access to the Query API that reads it
        if (queryApi == null)
        {
            throw auth == null
                ? HttpError.Unauthorized($"You need to be authenticated to view the '{modelType.Name}' Schema")
                : HttpError.Forbidden($"You don't have access to the '{modelType.Name}' Schema");
        }

        // Rows returned by IQueryDb<From,Into> are the Into type, which the Create/Update/Delete
        // APIs still write through the From type - so both shapes are needed, not just one
        var viewModelType = AutoCrudOperation.GetViewModelType(
            queryApi.Operation.RequestType, queryApi.Operation.ResponseType) ?? modelType;

        var to = new JsonObject
        {
            ["name"] = modelType.Name,
            ["title"] = modelType.GetDescription() ?? modelType.Name.SplitCamelCase(),
        };

        // Which property identifies a row, so UIs can reference one (e.g. deep link to it)
        if (GetPrimaryKey(modelType) is { } primaryKey)
            to["primaryKey"] = primaryKey;

        to["model"] = CreateModelSchema(modelType);

        // Only when they differ, so the common IQueryDb<T> case stays a single Model
        if (viewModelType != modelType)
            to["viewModel"] = CreateModelSchema(viewModelType);

        foreach (var (action, api) in SelectCrudApis(modelApis))
        {
            AddSchema(to, action, api);
        }

        return to;

        static void AddSchema(JsonObject to, string name, CrudApi? api)
        {
            if (api != null)
            {
                to[name] = CreateSchema(api.Operation.RequestType);
            }
        }
    }

    /// <summary>
    /// The CRUD API to use for each action, in display order.
    /// Shared so a Data Model's card and its Schema describe the same APIs.
    /// </summary>
    public static List<KeyValuePair<string, CrudApi>> SelectCrudApis(List<CrudApi> modelApis)
    {
        var to = new List<KeyValuePair<string, CrudApi>>();

        Add("query", FindCrud(modelApis, AutoCrudOperation.Query));
        Add("create", FindCrud(modelApis, AutoCrudOperation.Create));
        // Prefer IPatchDb<> over IUpdateDb<> when both are available
        Add("update", FindCrud(modelApis, AutoCrudOperation.Patch) ?? FindCrud(modelApis, AutoCrudOperation.Update));
        // Prefer single row deletes (e.g. DeleteTodo) over bulk deletes (e.g. DeleteTodos)
        Add("delete", FindSingleRowCrud(modelApis, AutoCrudOperation.Delete));
        Add("save", FindCrud(modelApis, AutoCrudOperation.Save));

        return to;

        void Add(string action, CrudApi? api)
        {
            if (api != null)
                to.Add(new KeyValuePair<string, CrudApi>(action, api));
        }
    }

    static CrudApi? FindCrud(List<CrudApi> apis, string crud) =>
        apis.FirstOrDefault(x => x.Crud == crud);

    static CrudApi? FindSingleRowCrud(List<CrudApi> apis, string crud)
    {
        var candidates = apis.Where(x => x.Crud == crud).ToList();
        return candidates.FirstOrDefault(x => !HasCollectionProperty(x.Operation.RequestType))
            ?? candidates.FirstOrDefault();
    }

    public static JsonArray CreateApis(IRequest req, AuthenticateResponse? auth = null)
    {
        var to = new JsonArray();
        var metadata = HostContext.Metadata;

        foreach (var op in metadata.Operations.OrderBy(x => x.Name))
        {
            if (op.RequestType.ExcludesFeature(Feature.Metadata) && !op.RequestType.ForceInclude())
                continue;

            if (!metadata.IsVisible(req, op))
                continue;

            if (!metadata.IsAuthorized(op, auth))
                continue;

            var title = op.RequestType.FirstAttribute<ApiAttribute>()?.Description
                ?? op.RequestType.GetDescription()
                ?? op.Name.SplitCamelCase();

            var notes = op.RequestType.FirstAttribute<NotesAttribute>()?.Notes
                ?? op.RequestType.FirstAttribute<DescriptionAttribute>()?.Description
                ?? op.Notes;

            var api = new JsonObject
            {
                ["name"] = op.Name,
                ["request"] = op.Name,
                ["title"] = title,
            };

            if (!string.IsNullOrEmpty(notes) && !string.Equals(notes, title, StringComparison.OrdinalIgnoreCase))
                api["notes"] = notes;

            if (!string.IsNullOrEmpty(op.Method))
                api["verb"] = op.Method;

            var route = op.Routes?.FirstOrDefault()?.Path;
            if (!string.IsNullOrEmpty(route))
                api["path"] = route;

            if (op.Tags != null && op.Tags.Count > 0)
                AddNames(api, "tags", op.Tags);

            if (GetIcon(op.RequestType) is { } icon)
                api["icon"] = icon;

            var apisObj = new JsonObject();
            var action = AutoCrudOperation.GetAutoQueryDtoType(op.RequestType)?.Operation.ToLower()
                ?? (op.Method == "GET" ? "query" : "api");

            var actionObj = new JsonObject { ["request"] = op.Name };
            if (AuthSchema(op) is { } authObj)
                actionObj["auth"] = authObj;

            apisObj[action] = actionObj;
            api["apis"] = apisObj;

            to.Add(api);
        }

        return to;
    }

    /// <summary>
    /// One entry per Data Model (not per API), listing the CRUD APIs this Session can call
    /// and what each requires, e.g:
    /// { "name":"Booking", "title":"Booking Details", "apis":{ "query":{...}, "create":{...} } }
    /// </summary>
    public static JsonArray CreateAutoQueryModels(IRequest req, AuthenticateResponse? auth = null)
    {
        var to = new JsonArray();
        var byModel = GetAuthorizedCrudApis(req, auth)
            .GroupBy(x => x.ModelType)
            .OrderBy(x => x.Key.Name);

        foreach (var group in byModel)
        {
            var modelType = group.Key;
            var apis = SelectCrudApis(group.ToList());

            // Same rule as /auto/{Model}: without a readable Query API there's nothing to open
            if (apis.All(x => x.Key != "query"))
                continue;

            var model = new JsonObject
            {
                ["name"] = modelType.Name,
                ["title"] = modelType.GetDescription() ?? modelType.Name.SplitCamelCase(),
            };

            // JsonIgnoreCondition only applies to POCO properties: a JsonObject writes every
            // node it holds, so an optional value has to be left out rather than set to null
            if (modelType.GetNotes() is { } notes && !string.IsNullOrEmpty(notes))
                model["notes"] = notes;

            // [Tag] is declared per API, so a Data Model's tags are the union of its APIs'
            var tags = apis.SelectMany(x => x.Value.Operation.Tags)
                .Distinct()
                .OrderBy(x => x)
                .ToList();
            if (tags.Count > 0)
                AddNames(model, "tags", tags);

            var apisObj = new JsonObject();
            foreach (var (action, api) in apis)
            {
                var apiObj = new JsonObject { ["request"] = api.Operation.Name };
                if (AuthSchema(api.Operation) is { } authObj)
                    apiObj["auth"] = authObj;
                apisObj[action] = apiObj;
            }
            model["apis"] = apisObj;

            to.Add(model);
        }
        return to;
    }

    /// <summary>
    /// A Type's [Icon], as {svg|uri|cls} so a UI can render it without App metadata
    /// </summary>
    static JsonObject? GetIcon(Type? type)
    {
        if (type?.FirstAttribute<IconAttribute>() is not { } icon)
            return null;

        var to = new JsonObject();
        if (!string.IsNullOrEmpty(icon.Svg)) to["svg"] = icon.Svg;
        if (!string.IsNullOrEmpty(icon.Uri)) to["uri"] = icon.Uri;
        if (!string.IsNullOrEmpty(icon.Cls)) to["cls"] = icon.Cls;
        return to.Count > 0 ? to : null;
    }

    /// <summary>
    /// The Data Model's Primary Key, by [PrimaryKey]/[AutoIncrement] or Id/{Model}Id convention
    /// </summary>
    static string? GetPrimaryKey(Type modelType)
    {
        var properties = modelType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p is { CanRead: true, CanWrite: true })
            .ToList();

        var pk = properties.FirstOrDefault(p => p.HasAttributeOf<PrimaryKeyAttribute>())
            ?? properties.FirstOrDefault(p => p.HasAttributeOf<AutoIncrementAttribute>())
            ?? properties.FirstOrDefault(p => p.Name == IdUtils.IdField)
            ?? properties.FirstOrDefault(p => p.Name.EqualsIgnoreCase(modelType.Name + IdUtils.IdField));

        return pk?.Name;
    }

    /// <summary>
    /// APIs that accept a collection (e.g. DeleteTodos Ids) operate on multiple rows
    /// </summary>
    static bool HasCollectionProperty(Type type) =>
        type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Any(p => p is { CanRead: true, CanWrite: true } && GetCollectionItemType(p.PropertyType) != null);

    /// <summary>
    /// The visible CRUD APIs this Request's Auth info is authorized to call
    /// </summary>
    public static List<CrudApi> GetAuthorizedCrudApis(IRequest req, AuthenticateResponse? auth = null)
    {
        var to = new List<CrudApi>();
        foreach (var api in GetVisibleCrudApis(req))
        {
            if (HostContext.Metadata.IsAuthorized(api.Operation, auth))
            {
                to.Add(api);
            }
        }
        return to;
    }

    static List<CrudApi> GetVisibleCrudApis(IRequest req)
    {
        var metadata = HostContext.Metadata;
        var to = new List<CrudApi>();
        
        foreach (var op in metadata.Operations)
        {
            if (AutoCrudOperation.GetAutoQueryDtoType(op.RequestType) is not { ModelType: not null } crudType)
                continue;

            if (op.RequestType.ExcludesFeature(Feature.Metadata) && !op.RequestType.ForceInclude())
                continue;

            if (!metadata.IsVisible(req, op))
                continue;

            to.Add(new CrudApi {
                Operation = op,
                ModelType = crudType.ModelType,
                Crud = crudType.Operation,
            });
        }
        return to;
    }

    /// <summary>
    /// Create the Schema for an API's Request DTO
    /// </summary>
    public static JsonObject CreateSchema(Type requestDto) => CreateSchema(requestDto, isModel: false);

    /// <summary>
    /// Create the Schema for a Data Model, i.e. the shape of the rows an AutoQuery API returns
    /// </summary>
    public static JsonObject CreateModelSchema(Type modelType) => CreateSchema(modelType, isModel: true);

    static JsonObject CreateSchema(Type requestDto, bool isModel)
    {
        Operation? operation = null;
        if (!isModel)
            HostContext.Metadata.OperationsMap.TryGetValue(requestDto, out operation);

        var title = requestDto.FirstAttribute<ApiAttribute>()?.Description
            ?? requestDto.GetDescription()
            ?? requestDto.Name.SplitCamelCase();

        var description = requestDto.FirstAttribute<NotesAttribute>()?.Notes
            ?? requestDto.FirstAttribute<DescriptionAttribute>()?.Description
            ?? title;

        // $id is the pre-defined route: it always exists, never changes, and takes every
        // property in the body or query string, so it doubles as the URL to call. A custom
        // [Route] can put properties in the path, which a generic client would have to reassemble
        var apiPath = $"/api/{requestDto.Name}";

        var rootSchema = new JsonObject
        {
            ["$schema"] = "http://json-schema.org/draft-07/schema#",
            // A Data Model isn't addressable so use a draft-07 plain name fragment
            ["$id"] = isModel ? "#" + requestDto.Name : apiPath,
            ["title"] = title,
            ["description"] = description,
            ["type"] = "object",
        };

        // The API this describes, so clients can resolve its Request DTO metadata
        if (!isModel)
            rootSchema["request"] = requestDto.Name;

        // Which AutoQuery/CRUD API this is, e.g. Query, Create, Update, Patch, Delete, Save
        if (!isModel && AutoCrudOperation.GetAutoQueryDtoType(requestDto) is { } crudType)
            rootSchema["operation"] = crudType.Operation;

        // The HTTP Method to call it with. Always populated and specific: the pre-defined
        // route only accepts the verb its Request DTO declares, so this pairs with $id
        if (!string.IsNullOrEmpty(operation?.Method))
            rootSchema["method"] = operation.Method;

        var propertiesObj = new JsonObject();
        var requiredArray = new JsonArray();
        var uiObj = new JsonObject();
        if (!isModel)
            uiObj["submitLabel"] = requestDto.Name.SplitCamelCase();

        if (operation != null)
        {

            if (!string.IsNullOrEmpty(operation.Notes))
                uiObj["notes"] = operation.Notes;

            if (operation.LocodeCss != null || operation.ExplorerCss != null)
            {
                var cssObj = new JsonObject();
                var formCss = operation.LocodeCss?.Form ?? operation.ExplorerCss?.Form;
                var fieldCss = operation.LocodeCss?.Field ?? operation.ExplorerCss?.Field;
                if (!string.IsNullOrEmpty(formCss)) cssObj["formCss"] = formCss;
                if (!string.IsNullOrEmpty(fieldCss)) cssObj["fieldCss"] = fieldCss;
                uiObj["css"] = cssObj;
            }
        }

        var dataModelType = AutoCrudOperation.GetModelType(requestDto);
        var classFields = requestDto.AllAttributes<FieldAttribute>();

        var properties = requestDto.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanRead && p.CanWrite && p.GetIndexParameters().Length == 0)
            .Where(p => !HasAttributeNamed(p, "IgnoreDataMemberAttribute")
                && !HasAttributeNamed(p, "IgnoreAttribute")
                && p.FirstAttribute<ApiMemberAttribute>()?.ExcludeInSchema != true);

        var seenTypes = new HashSet<Type> { requestDto };

        foreach (var pi in properties)
        {
            var inputAttr = pi.FirstAttribute<InputAttribute>();
            var fieldAttr = classFields.FirstOrDefault(f => f.Name?.Equals(pi.Name, StringComparison.OrdinalIgnoreCase) == true);

            if (inputAttr?.Ignore == true || fieldAttr?.Ignore == true)
                continue;

            var propName = pi.Name;
            var apiMember = pi.FirstAttribute<ApiMemberAttribute>();

            var isRequired = apiMember?.IsRequired == true
                || (HasAttributeNamed(pi, "RequiredAttribute") && apiMember?.IsOptional != true)
                || pi.HasAttributeOf<ValidateNotEmptyAttribute>()
                || pi.HasAttributeOf<ValidateNotNullAttribute>()
                || inputAttr?.Required == true;

            if (isRequired)
                requiredArray.Add(propName);

            var propSchema = PropertySchema(pi, apiMember, inputAttr, fieldAttr, dataModelType, 0, seenTypes);
            propertiesObj[propName] = propSchema;
        }

        rootSchema["properties"] = propertiesObj;

        if (requiredArray.Count > 0)
            rootSchema["required"] = requiredArray;

        if (operation != null && AuthSchema(operation) is { } authObj)
            rootSchema["auth"] = authObj;

        if (uiObj.Count > 0)
            rootSchema["ui"] = uiObj;

        return rootSchema;
    }

    static JsonObject? AuthSchema(Operation operation)
    {
        var authObj = new JsonObject();
        AddAuthSchema(authObj, operation: operation);
        return authObj.Count > 0 ? authObj : null;
    }

    public static void AddAuthSchema(JsonObject obj, Operation operation)
    {
        if (operation.RequiresAuthentication)
            obj["requiresAuth"] = true;

        if (operation.RequiresApiKey)
            obj["requiresApiKey"] = true;

        AddNames(obj, "requiredRoles", operation.RequiredRoles);
        AddNames(obj, "requiresAnyRole", operation.RequiresAnyRole);
        AddNames(obj, "requiredPermissions", operation.RequiredPermissions);
        AddNames(obj, "requiresAnyPermission", operation.RequiresAnyPermission);
        AddNames(obj, "requiredScopes", operation.RequiredScopes);

        if (operation.RequiredClaims.Count > 0)
        {
            var claimsArr = new JsonArray();
            foreach (var claim in operation.RequiredClaims)
            {
                claimsArr.Add(new JsonObject
                {
                    ["type"] = claim.Type,
                    ["value"] = claim.Value,
                });
            }
            obj["requiredClaims"] = claimsArr;
        }

        if (!string.IsNullOrEmpty(operation.Authorize?.Policy))
            obj["policy"] = operation.Authorize.Policy;

        if (!string.IsNullOrEmpty(operation.Authorize?.AuthenticationSchemes))
            obj["authSchemes"] = operation.Authorize.AuthenticationSchemes;
    }

    static void AddNames(JsonObject obj, string name, List<string>? values)
    {
        if (values is not { Count: > 0 })
            return;

        var arr = new JsonArray();
        foreach (var value in values.Distinct())
            arr.Add(value);
        obj[name] = arr;
    }

    static JsonObject PropertySchema(PropertyInfo pi, ApiMemberAttribute? apiMember, InputAttribute? inputAttr, FieldAttribute? fieldAttr, Type? dataModelType, int depth, HashSet<Type> seen)
    {
        var propSchema = new JsonObject();
        var propUi = new JsonObject();

        var propType = Nullable.GetUnderlyingType(pi.PropertyType) ?? pi.PropertyType;
        var propTitle = apiMember?.Description ?? pi.GetDescription() ?? pi.Name.SplitCamelCase();
        var propHelp = inputAttr?.Help ?? fieldAttr?.Help ?? apiMember?.Description ?? pi.GetDescription();
        var propPlaceholder = inputAttr?.Placeholder ?? fieldAttr?.Placeholder;

        propSchema["title"] = propTitle;
        if (!string.IsNullOrEmpty(propHelp) && propHelp != propTitle)
            propSchema["description"] = propHelp;

        // Data Type mapping
        if (propType.IsEnum)
        {
            propSchema["type"] = "string";
            var enumArray = new JsonArray();
            foreach (var name in Enum.GetNames(propType))
                enumArray.Add(name);
            propSchema["enum"] = enumArray;

            var enumDescObj = new JsonObject();
            foreach (var field in propType.GetFields(BindingFlags.Public | BindingFlags.Static))
            {
                var desc = field.FirstAttribute<DescriptionAttribute>()?.Description;
                if (!string.IsNullOrEmpty(desc))
                    enumDescObj[field.Name] = desc;
            }
            if (enumDescObj.Count > 0)
                propUi["enumDescriptions"] = enumDescObj;
        }
        else if (propType == typeof(string))
        {
            propSchema["type"] = "string";
        }
        else if (propType == typeof(bool))
        {
            propSchema["type"] = "boolean";
        }
        else if (propType == typeof(int) || propType == typeof(long) || propType == typeof(short) || propType == typeof(byte) || propType == typeof(uint) || propType == typeof(ulong) || propType == typeof(ushort) || propType == typeof(sbyte))
        {
            propSchema["type"] = "integer";
        }
        else if (propType == typeof(double) || propType == typeof(float) || propType == typeof(decimal))
        {
            propSchema["type"] = "number";
        }
        else if (propType == typeof(DateTime) || propType == typeof(DateTimeOffset))
        {
            propSchema["type"] = "string";
            propSchema["format"] = "date-time";
        }
        else if (propType == typeof(Guid))
        {
            propSchema["type"] = "string";
            propSchema["format"] = "uuid";
        }
        else if (propType == typeof(Uri))
        {
            propSchema["type"] = "string";
            propSchema["format"] = "uri";
        }
        else if (propType == typeof(TimeSpan))
        {
            propSchema["type"] = "string";
            propSchema["format"] = "duration";
        }
        else if (GetDictionaryValueType(propType) is { } dictValueType)
        {
            propSchema["type"] = "object";
            propSchema["additionalProperties"] = TypeSchema(dictValueType, depth + 1, seen);
        }
        else if (GetCollectionItemType(propType) is { } itemType)
        {
            propSchema["type"] = "array";
            propSchema["items"] = TypeSchema(itemType, depth + 1, seen);
        }
        else if (propType.IsClass || propType.IsValueType)
        {
            propSchema["type"] = "object";
            propSchema["properties"] = ObjectProperties(propType, depth + 1, seen);
        }

        // Allowable Values ([ApiAllowableValues])
        var allowable = pi.FirstAttribute<ApiAllowableValuesAttribute>()?.Values;
        if (allowable is { Length: > 0 })
        {
            var enumArr = new JsonArray();
            foreach (var v in allowable) enumArr.Add(v);
            propSchema["enum"] = enumArr;
        }

        // Process [Validate*] attributes
        var validateAttrs = pi.AllAttributes<ValidateAttribute>();
        foreach (var vAttr in validateAttrs)
        {
            ApplyValidator(vAttr.Validator, propSchema);
        }

        // Range attribute
        var rangeAttr = pi.GetCustomAttributes(true).FirstOrDefault(a => a.GetType().Name == "RangeAttribute");
        if (rangeAttr != null)
        {
            var minProp = rangeAttr.GetType().GetProperty("Minimum")?.GetValue(rangeAttr);
            var maxProp = rangeAttr.GetType().GetProperty("Maximum")?.GetValue(rangeAttr);
            if (minProp != null && double.TryParse(minProp.ToString(), out var minVal))
                propSchema["minimum"] = minVal;
            if (maxProp != null && double.TryParse(maxProp.ToString(), out var maxVal))
                propSchema["maximum"] = maxVal;
        }

        // StringLength attribute
        var stringLengthAttr = pi.GetCustomAttributes(true).FirstOrDefault(a => a.GetType().Name == "StringLengthAttribute");
        if (stringLengthAttr != null)
        {
            var minLenProp = stringLengthAttr.GetType().GetProperty("MinimumLength")?.GetValue(stringLengthAttr);
            var maxLenProp = stringLengthAttr.GetType().GetProperty("MaximumLength")?.GetValue(stringLengthAttr);
            if (minLenProp is int minL && minL > 0)
                propSchema["minLength"] = minL;
            if (maxLenProp is int maxL && maxL > 0)
                propSchema["maxLength"] = maxL;
        }

        // Reference / Lookup Info ([Ref], [References], [ForeignKey], [Reference])
        var refInfo = GetRefInfo(pi, dataModelType);
        if (refInfo != null)
        {
            var refObj = new JsonObject();
            if (!string.IsNullOrEmpty(refInfo.Model)) refObj["model"] = refInfo.Model;
            if (!string.IsNullOrEmpty(refInfo.RefId)) refObj["refId"] = refInfo.RefId;
            if (!string.IsNullOrEmpty(refInfo.RefLabel)) refObj["refLabel"] = refInfo.RefLabel;
            if (!string.IsNullOrEmpty(refInfo.SelfId)) refObj["selfId"] = refInfo.SelfId;
            if (!string.IsNullOrEmpty(refInfo.QueryApi)) refObj["queryApi"] = refInfo.QueryApi;

            // the referenced Model's icon, so a UI can label the ref without App metadata
            if (GetIcon(refInfo.ModelType ?? HostContext.Metadata.FindDtoType(refInfo.Model!)) is { } refIcon)
                refObj["icon"] = refIcon;

            if (refObj.Count > 0)
                propUi["ref"] = refObj;
        }

        // UI Widget & Options
        var widget = inputAttr?.Type ?? fieldAttr?.Type;
        if (!string.IsNullOrEmpty(widget))
        {
            propUi["widget"] = widget;
        }
        else if (refInfo != null)
        {
            propUi["widget"] = "lookup";
        }

        if (!string.IsNullOrEmpty(propPlaceholder))
            propUi["placeholder"] = propPlaceholder;

        if (!string.IsNullOrEmpty(propHelp))
            propUi["help"] = propHelp;

        if (!string.IsNullOrEmpty(inputAttr?.Step))
        {
            if (double.TryParse(inputAttr.Step, out var stepVal))
                propUi["step"] = stepVal;
        }

        // How to render the value, e.g. currency/relative time/icon, and 'hidden' to omit it
        // from a grid. Same derivation MetadataTypes.ToProperty() uses for App metadata.
        var format = pi.FirstAttribute<Intl>().ToFormat()
            ?? pi.FirstAttribute<FormatAttribute>().ToFormat();
        if (format != null)
        {
            var formatObj = new JsonObject();
            if (!string.IsNullOrEmpty(format.Method)) formatObj["method"] = format.Method;
            if (!string.IsNullOrEmpty(format.Options)) formatObj["options"] = format.Options;
            if (!string.IsNullOrEmpty(format.Locale)) formatObj["locale"] = format.Locale;
            if (formatObj.Count > 0)
                propUi["format"] = formatObj;
        }

        // Which file types the [UploadTo] location allows, as NativeTypesMetadata does
        if (pi.FirstAttribute<UploadToAttribute>() is { } uploadTo)
        {
            var location = HostContext.GetPlugin<FilesUploadFeature>()?.Locations
                .FirstOrDefault(x => x.Name == uploadTo.Location);
            if (location is { AllowExtensions.Count: > 0 })
                propUi["accept"] = string.Join(",", location.AllowExtensions.Map(x => $".{x}"));
        }

        var fieldCssAttr = pi.FirstAttribute<FieldCssAttribute>();
        var fieldCss = fieldCssAttr?.Field ?? fieldAttr?.FieldCss;
        if (!string.IsNullOrEmpty(fieldCss))
            propUi["fieldCss"] = fieldCss;

        if (propUi.Count > 0)
            propSchema["ui"] = propUi;

        return propSchema;
    }

    static JsonObject TypeSchema(Type type, int depth, HashSet<Type> seen)
    {
        type = Nullable.GetUnderlyingType(type) ?? type;
        var schema = new JsonObject();

        if (type.IsEnum)
        {
            schema["type"] = "string";
            var enumArray = new JsonArray();
            foreach (var name in Enum.GetNames(type)) enumArray.Add(name);
            schema["enum"] = enumArray;
            return schema;
        }

        if (type == typeof(string)) schema["type"] = "string";
        else if (type == typeof(bool)) schema["type"] = "boolean";
        else if (type == typeof(int) || type == typeof(long) || type == typeof(short) || type == typeof(byte)) schema["type"] = "integer";
        else if (type == typeof(double) || type == typeof(float) || type == typeof(decimal)) schema["type"] = "number";
        else if (type == typeof(DateTime) || type == typeof(DateTimeOffset)) { schema["type"] = "string"; schema["format"] = "date-time"; }
        else if (type == typeof(Guid)) { schema["type"] = "string"; schema["format"] = "uuid"; }
        else if (GetDictionaryValueType(type) is { } dictValueType)
        {
            schema["type"] = "object";
            schema["additionalProperties"] = TypeSchema(dictValueType, depth + 1, seen);
        }
        else if (GetCollectionItemType(type) is { } itemType)
        {
            schema["type"] = "array";
            schema["items"] = TypeSchema(itemType, depth + 1, seen);
        }
        else if (type.IsClass && depth < 3 && seen.Add(type))
        {
            schema["type"] = "object";
            schema["properties"] = ObjectProperties(type, depth + 1, seen);
            seen.Remove(type);
        }
        else
        {
            schema["type"] = "object";
        }

        return schema;
    }

    static JsonObject ObjectProperties(Type type, int depth, HashSet<Type> seen)
    {
        var propsObj = new JsonObject();
        var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanRead && p.CanWrite && p.GetIndexParameters().Length == 0)
            .Where(p => !HasAttributeNamed(p, "IgnoreDataMemberAttribute") && !HasAttributeNamed(p, "IgnoreAttribute"));

        foreach (var pi in properties)
        {
            propsObj[pi.Name] = TypeSchema(pi.PropertyType, depth, seen);
        }
        return propsObj;
    }

    static void ApplyValidator(string validator, JsonObject propSchema)
    {
        if (string.IsNullOrEmpty(validator)) return;

        if (validator == "Email") propSchema["format"] = "email";
        else if (validator == "CreditCard") propSchema["format"] = "credit-card";
        else if (Regex.Match(validator, @"^MinimumLength\((\d+)\)$") is { Success: true } mMin)
            propSchema["minLength"] = int.Parse(mMin.Groups[1].Value);
        else if (Regex.Match(validator, @"^MaximumLength\((\d+)\)$") is { Success: true } mMax)
            propSchema["maxLength"] = int.Parse(mMax.Groups[1].Value);
        else if (Regex.Match(validator, @"^ExactLength\((\d+)\)$") is { Success: true } mExact)
        {
            propSchema["minLength"] = int.Parse(mExact.Groups[1].Value);
            propSchema["maxLength"] = int.Parse(mExact.Groups[1].Value);
        }
        else if (Regex.Match(validator, @"^Length\((\d+),(\d+)\)$") is { Success: true } mLen)
        {
            propSchema["minLength"] = int.Parse(mLen.Groups[1].Value);
            propSchema["maxLength"] = int.Parse(mLen.Groups[2].Value);
        }
        else if (Regex.Match(validator, @"^GreaterThan\((\d+(?:\.\d+)?)\)$") is { Success: true } mGt)
        {
            if (double.TryParse(mGt.Groups[1].Value, out var val)) propSchema["exclusiveMinimum"] = val;
        }
        else if (Regex.Match(validator, @"^GreaterThanOrEqual\((\d+(?:\.\d+)?)\)$") is { Success: true } mGte)
        {
            if (double.TryParse(mGte.Groups[1].Value, out var val)) propSchema["minimum"] = val;
        }
        else if (Regex.Match(validator, @"^LessThan\((\d+(?:\.\d+)?)\)$") is { Success: true } mLt)
        {
            if (double.TryParse(mLt.Groups[1].Value, out var val)) propSchema["exclusiveMaximum"] = val;
        }
        else if (Regex.Match(validator, @"^LessThanOrEqual\((\d+(?:\.\d+)?)\)$") is { Success: true } mLte)
        {
            if (double.TryParse(mLte.Groups[1].Value, out var val)) propSchema["maximum"] = val;
        }
        else if (Regex.Match(validator, @"^InclusiveBetween\((\d+(?:\.\d+)?),(\d+(?:\.\d+)?)\)$") is { Success: true } mInc)
        {
            if (double.TryParse(mInc.Groups[1].Value, out var f) && double.TryParse(mInc.Groups[2].Value, out var t))
            {
                propSchema["minimum"] = f;
                propSchema["maximum"] = t;
            }
        }
        else if (Regex.Match(validator, @"^ExclusiveBetween\((\d+(?:\.\d+)?),(\d+(?:\.\d+)?)\)$") is { Success: true } mExc)
        {
            if (double.TryParse(mExc.Groups[1].Value, out var f) && double.TryParse(mExc.Groups[2].Value, out var t))
            {
                propSchema["exclusiveMinimum"] = f;
                propSchema["exclusiveMaximum"] = t;
            }
        }
        else if (Regex.Match(validator, @"^RegularExpression\(.?'([^']+)'\.?\)$") is { Success: true } mRegex)
        {
            propSchema["pattern"] = mRegex.Groups[1].Value;
        }
    }

    static bool HasAttributeNamed(MemberInfo mi, string name) =>
        mi.GetCustomAttributes(true).Any(a => a.GetType().Name == name);

    static RefInfo? GetRefInfo(PropertyInfo pi, Type? dataModelType)
    {
        var refInfo = ResolveRefInfo(pi);
        if (refInfo != null)
            return refInfo;

        if (dataModelType != null && dataModelType != pi.DeclaringType)
        {
            var modelProp = dataModelType.GetProperty(pi.Name);
            if (modelProp != null)
            {
                refInfo = ResolveRefInfo(modelProp);
                if (refInfo != null)
                    return refInfo;
            }
        }

        return null;
    }

    static RefInfo? ResolveRefInfo(PropertyInfo pi)
    {
        var refAttr = pi.FirstAttribute<RefAttribute>();
        if (refAttr != null && !refAttr.None)
        {
            var model = refAttr.Model ?? refAttr.ModelType?.Name;
            if (!string.IsNullOrEmpty(model))
            {
                return new RefInfo
                {
                    ModelType = refAttr.ModelType,
                    QueryType = refAttr.QueryType,
                    QueryApi = refAttr.QueryType?.Name,
                    Model = model,
                    SelfId = refAttr.SelfId,
                    RefId = refAttr.RefId,
                    RefLabel = refAttr.RefLabel,
                };
            }
        }

        var refsAttr = pi.FirstAttribute<ReferencesAttribute>();
        if (refsAttr?.Type != null)
        {
            var modelRef = refsAttr.Type.CreateRefModel();
            if (modelRef != null)
                modelRef.SelfId = pi.Name;
            return modelRef;
        }

        var fkAttr = pi.FirstAttribute<ForeignKeyAttribute>();
        if (fkAttr?.Type != null)
        {
            var modelRef = fkAttr.Type.CreateRefModel();
            if (modelRef != null)
                modelRef.SelfId = pi.Name;
            return modelRef;
        }

        var referenceAttr = pi.FirstAttribute<ReferenceAttribute>();
        if (referenceAttr != null)
        {
            var pt = pi.PropertyType;
            if (referenceAttr.SelfId != null && referenceAttr.RefLabel != null)
            {
                return new RefInfo
                {
                    Model = pt.Name,
                    SelfId = referenceAttr.SelfId,
                    RefId = referenceAttr.RefId,
                    RefLabel = referenceAttr.RefLabel,
                };
            }
            var selfId = referenceAttr.SelfId
                ?? (pi.DeclaringType?.GetProperty(pi.Name + "Id") != null ? pi.Name + "Id" : null);
            var modelRef = pt.CreateRefModel();
            if (modelRef != null && selfId != null)
                modelRef.SelfId = selfId;
            return modelRef;
        }

        return null;
    }

    static Type? GetDictionaryValueType(Type type)
    {
        if (type == typeof(string) || type == typeof(byte[])) return null;
        var dictionary = type.GetInterfaces().Concat([type])
            .FirstOrDefault(x => x.IsGenericType && (x.GetGenericTypeDefinition() == typeof(IDictionary<,>) || x.GetGenericTypeDefinition() == typeof(IReadOnlyDictionary<,>)));
        if (dictionary != null)
            return dictionary.GetGenericArguments()[1];

        if (typeof(System.Collections.IDictionary).IsAssignableFrom(type))
            return typeof(object);

        return null;
    }

    static Type? GetCollectionItemType(Type type)
    {
        if (type == typeof(string) || type == typeof(byte[])) return null;
        if (type.IsArray) return type.GetElementType();
        var enumerable = type.GetInterfaces().Concat([type])
            .FirstOrDefault(x => x.IsGenericType && x.GetGenericTypeDefinition() == typeof(IEnumerable<>));
        return enumerable?.GetGenericArguments()[0];
    }
}

#endif
