using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using ServiceStack.Configuration;
using ServiceStack.Data;
using ServiceStack.DataAnnotations;
using ServiceStack.NativeTypes;
using ServiceStack.NativeTypes.CSharp;
using ServiceStack.NativeTypes.Dart;
using ServiceStack.NativeTypes.FSharp;
using ServiceStack.NativeTypes.Java;
using ServiceStack.NativeTypes.Kotlin;
using ServiceStack.NativeTypes.Swift;
using ServiceStack.NativeTypes.TypeScript;
using ServiceStack.NativeTypes.VbNet;
using ServiceStack.OrmLite;
using ServiceStack.Web;

namespace ServiceStack
{
    //TODO: persist AutoCrud
    public class AutoCrudFeature : IPlugin
    {
        public List<string> IncludeCrudOperations { get; set; } = new List<string> {
            AutoCrudOperation.Query,
            AutoCrudOperation.Create,
            AutoCrudOperation.Update,
            AutoCrudOperation.Patch,
            AutoCrudOperation.Delete,
        };
        
        /// <summary>
        /// Generate services 
        /// </summary>
        public List<AutoCodeServices> GenerateMissingServices { get; set; } = new List<AutoCodeServices>();
        
        public Action<MetadataTypes, MetadataTypesConfig, IRequest> MetadataTypesFilter { get; set; }
        public Action<MetadataType, IRequest> TypeFilter { get; set; }
        public Action<MetadataOperationType, IRequest> ServiceFilter { get; set; }
        
        public Func<MetadataType, bool> IncludeType { get; set; }
        public Func<MetadataOperationType, bool> IncludeService { get; set; }
        
        public string AccessRole { get; set; } = RoleNames.Admin;
        
        internal ConcurrentDictionary<Tuple<string,string>, DbSchema> CachedDbSchemas { get; } 
            = new ConcurrentDictionary<Tuple<string,string>, DbSchema>();
        
        string Localize(string s) => HostContext.AppHost?.ResolveLocalizedString(s, null) ?? s;

        
        public Dictionary<Type, string[]> ServiceRoutes { get; set; }
        
        private const string NoSchema = "__noschema";

        public AutoCrudFeature()
        {
            ServiceRoutes = new Dictionary<Type, string[]> {
                { typeof(AutoCrudTypesService), new[]
                {
                    "/" + Localize("autocrud") + "/{Include}/{Lang}",
                } },
                { typeof(AutoCrudSchemaService), new[] {
                    "/" + Localize("autocrud") + "/" + Localize("schema"),
                    "/" + Localize("autocrud") + "/" + Localize("schema") + "/{Schema}",
                } },
            };
        }

        public DbSchema GetCachedDbSchema(IDbConnectionFactory dbFactory, string schema=null, string namedConnection=null)
        {
            var key = new Tuple<string, string>(schema ?? NoSchema, namedConnection);
            return CachedDbSchemas.GetOrAdd(key, k => {

                var tables = GetTableSchemas(dbFactory, schema, namedConnection);
                return new DbSchema {
                    Schema = schema,
                    NamedConnection = namedConnection,
                    Tables = tables,
                };
            });
        }

        public void Register(IAppHost appHost)
        {
            foreach (var registerService in ServiceRoutes)
            {
                appHost.RegisterService(registerService.Key, registerService.Value);
            }
        }

        public static List<TableSchema> GetTableSchemas(IDbConnectionFactory dbFactory, string schema=null, string namedConnection=null)
        {
            using var db = namedConnection != null
                ? dbFactory.OpenDbConnection(namedConnection)
                : dbFactory.OpenDbConnection();

            var tables = db.GetTableNames(schema);
            var results = new List<TableSchema>();

            var dialect = db.GetDialectProvider();
            foreach (var table in tables)
            {
                var to = new TableSchema {
                    Name = table,
                };

                try
                {
                    var quotedTable = dialect.GetQuotedTableName(table, schema);
                    to.Columns = db.GetTableColumns($"SELECT * FROM {quotedTable}");
                }
                catch (Exception e)
                {
                    to.ErrorType = e.GetType().Name;
                    to.ErrorMessage = e.Message;
                }

                results.Add(to);
            }

            return results;
        }


        public static string GenerateSourceCode(IRequest req, AutoCodeTypes request, 
            MetadataTypesConfig typesConfig, MetadataTypes crudMetadataTypes)
        {
            var metadata = req.Resolve<INativeTypesMetadata>();
            var src = request.Lang switch {
                "csharp" => new CSharpGenerator(typesConfig).GetCode(crudMetadataTypes, req),
                "fsharp" => new FSharpGenerator(typesConfig).GetCode(crudMetadataTypes, req),
                "vbnet" => new VbNetGenerator(typesConfig).GetCode(crudMetadataTypes, req),
                "typescript" => new TypeScriptGenerator(typesConfig).GetCode(crudMetadataTypes, req, metadata),
                "dart" => new DartGenerator(typesConfig).GetCode(crudMetadataTypes, req, metadata),
                "swift" => new SwiftGenerator(typesConfig).GetCode(crudMetadataTypes, req),
                "java" => new JavaGenerator(typesConfig).GetCode(crudMetadataTypes, req, metadata),
                "kotlin" => new KotlinGenerator(typesConfig).GetCode(crudMetadataTypes, req, metadata),
                "typescript.d" => new FSharpGenerator(typesConfig).GetCode(crudMetadataTypes, req),
            };
            return src;
        }

        public static string GenerateSource(IRequest req, AutoCodeTypes request)
        {
            var ret = ResolveMetadataTypes(req, request);
            var crudMetadataTypes = ret.Item1;
            var typesConfig = ret.Item2;
            
            if (request.Lang == "typescript")
            {
                typesConfig.MakePropertiesOptional = false;
                typesConfig.ExportAsTypes = true;
            }
            else if (request.Lang == "typescript.d")
            {
                typesConfig.MakePropertiesOptional = true;
            }

            var src = GenerateSourceCode(req, request, typesConfig, crudMetadataTypes);
            return src;
        }
        
        public static Tuple<MetadataTypes,MetadataTypesConfig> ResolveMetadataTypes(IRequest req, AutoCodeTypes request)
        {
            if (string.IsNullOrEmpty(request.Include))
                throw new ArgumentNullException(nameof(request.Include));
            if (request.Include != "all" && request.Include != "new")
                throw new ArgumentException(
                    "'Include' must be either 'all' to include all AutoQuery Services or 'new' to include only missing Services and Types", 
                    nameof(request.Include));
            
            var metadata = req.Resolve<INativeTypesMetadata>();
            var feature = HostContext.AssertPlugin<AutoCrudFeature>();
            
            var dbFactory = req.TryResolve<IDbConnectionFactory>();
            var results = request.NoCache == true 
                ? GetTableSchemas(dbFactory, request.Schema, request.NamedConnection)
                : feature.GetCachedDbSchema(dbFactory, request.Schema, request.NamedConnection).Tables;

            var appHost = HostContext.AppHost;
            request.BaseUrl ??= HostContext.GetPlugin<NativeTypesFeature>().MetadataTypesConfig.BaseUrl ?? appHost.GetBaseUrl(req);
            if (request.MakePartial == null)
                request.MakePartial = false;
            if (request.MakeVirtual == null)
                request.MakeVirtual = false;

            var typesConfig = metadata.GetConfig(request);
            typesConfig.UsePath = req.PathInfo;
            var metadataTypes = metadata.GetMetadataTypes(req, typesConfig);
            var serviceModelNs = appHost.GetType().Namespace + ".ServiceModel";
            var typesNs = serviceModelNs + ".Types";
            metadataTypes.Namespaces.Add(serviceModelNs);
            metadataTypes.Namespaces.Add(typesNs);
            metadataTypes.Namespaces = metadataTypes.Namespaces.Distinct().ToList();
            
            var crudVerbs = new Dictionary<string,string> {
                { AutoCrudOperation.Query, HttpMethods.Get },
                { AutoCrudOperation.Create, HttpMethods.Post },
                { AutoCrudOperation.Update, HttpMethods.Put },
                { AutoCrudOperation.Patch, HttpMethods.Patch },
                { AutoCrudOperation.Delete, HttpMethods.Delete },
                { AutoCrudOperation.Save, HttpMethods.Post },
            };
            var allVerbs = crudVerbs.Values.Distinct().ToArray();

            var existingRoutes = new HashSet<Tuple<string,string>>();
            foreach (var op in appHost.Metadata.Operations)
            {
                foreach (var route in op.Routes.Safe())
                {
                    var routeVerbs = route.Verbs.IsEmpty() ? allVerbs : route.Verbs;
                    foreach (var verb in routeVerbs)
                    {
                        existingRoutes.Add(new Tuple<string, string>(route.Path, verb));
                    }
                }
            }

            var typesToGenerateMap = new Dictionary<string, TableSchema>();
            foreach (var result in results)
            {
                var keysCount = result.Columns.Count(x => x.IsKey);
                if (keysCount != 1) // Only support tables with 1 PK
                    continue;
                
                typesToGenerateMap[result.Name] = result;
            }
            
            var includeCrudServices = request.IncludeCrudOperations ?? feature.IncludeCrudOperations;
            var includeCrudInterfaces = AutoCrudOperation.CrudInterfaceMetadataNames(includeCrudServices);
            
            //remove unnecessary
            var existingTypes = new HashSet<string>();
            var operations = new List<MetadataOperationType>();
            var types = new List<MetadataType>();
            
            foreach (var op in metadataTypes.Operations)
            {
                if (op.Request.Implements?.Any(x => includeCrudInterfaces.Contains(x.Name)) == true)
                {
                    operations.Add(op);
                }
                existingTypes.Add(op.Request.Name);
                if (op.Response != null)
                    existingTypes.Add(op.Response.Name);
            }
            foreach (var metaType in metadataTypes.Types)
            {
                if (typesToGenerateMap.ContainsKey(metaType.Name))
                {
                    types.Add(metaType);
                }
                existingTypes.Add(metaType.Name);
            }

            var crudMetadataTypes = new MetadataTypes {
                Config = metadataTypes.Config,
                Namespaces = metadataTypes.Namespaces,
                Operations = operations,
                Types = types,
            };
            if (request.Include == "new")
            {
                crudMetadataTypes.Operations = new List<MetadataOperationType>();
                crudMetadataTypes.Types = new List<MetadataType>();
            }
            
            MetadataAttribute toAlias(string alias) => new MetadataAttribute {
                Name = "Alias",
                ConstructorArgs = new List<MetadataPropertyType> {
                    new MetadataPropertyType { Name = "Name", Value = alias, Type = "string" },
                }
            };

            List<MetadataPropertyType> toMetaProps(IEnumerable<ColumnSchema> columns, bool isModel=false)
            {
                var to = new List<MetadataPropertyType>();
                foreach (var column in columns)
                {
                    var dataType = column.DataType;
                    if (dataType == null)
                        continue;
                    if (dataType == typeof(string) && column.ColumnSize == 1)
                        dataType = typeof(char);

                    var isKey = column.IsKey || column.IsAutoIncrement;

                    var prop = new MetadataPropertyType {
                        Name = CSharpGenerator.SafeSymbolName(column.ColumnName),
                        Type = dataType.GetMetadataPropertyType(),
                        IsValueType = dataType.IsValueType ? true : (bool?) null,
                        IsSystemType = dataType.IsSystemType() ? true : (bool?) null,
                        IsEnum = dataType.IsEnum ? true : (bool?) null,
                        TypeNamespace = dataType.Namespace,
                    };
                    
                    if (dataType.IsValueType && column.AllowDBNull && !isKey)
                    {
                        prop.Type += "?";
                    }
                    
                    var attrs = new List<MetadataAttribute>();
                    if (isModel)
                    {
                        prop.TypeNamespace = typesNs;
                        if (column.IsKey && column.ColumnName != IdUtils.IdField && !column.IsAutoIncrement)
                            attrs.Add(new MetadataAttribute { Name = "PrimaryKey" });
                        if (column.IsAutoIncrement)
                            attrs.Add(new MetadataAttribute { Name = "AutoIncrement" });
                        if (prop.Name != column.ColumnName)
                            attrs.Add(toAlias(column.ColumnName));
                        if (!dataType.IsValueType && !column.AllowDBNull && !isKey)
                            attrs.Add(new MetadataAttribute { Name = "Required" });
                    }

                    if (attrs.Count > 0)
                        prop.Attributes = attrs;
                    
                    to.Add(prop);
                }
                return to;
            }

            foreach (var entry in typesToGenerateMap)
            {
                var typeName = CSharpGenerator.SafeSymbolName(entry.Key);
                var tableSchema = entry.Value;
                if (includeCrudServices != null)
                {
                    var pkField = tableSchema.Columns.First(x => x.IsKey);
                    var id = CSharpGenerator.SafeSymbolName(pkField.ColumnName);
                    foreach (var operation in includeCrudServices)
                    {
                        if (!AutoCrudOperation.IsOperation(operation))
                            continue;

                        var requestType = operation + typeName;
                        if (existingTypes.Contains(requestType))
                            continue;

                        var verb = crudVerbs[operation];
                        var plural = Words.Pluralize(typeName).ToLower();
                        var route = verb == "GET" || verb == "POST"
                            ? "/" + plural
                            : "/" + plural + "/{" + id + "}";
                            
                        var op = new MetadataOperationType {
                            Actions = new List<string> { verb },
                            Request = new MetadataType {
                                Routes = new List<MetadataRoute> {
                                },
                                Name = requestType,
                                Namespace = serviceModelNs,
                                Implements = new [] { 
                                    new MetadataTypeName {
                                        Name = "I" + verb[0] + verb.Substring(1).ToLower(), //marker interface 
                                    },
                                },
                            },
                        };
                        
                        if (!existingRoutes.Contains(new Tuple<string, string>(route, verb)))
                            op.Request.Routes.Add(new MetadataRoute { Path = route, Verbs = verb });

                        if (verb == HttpMethods.Get)
                        {
                            op.Request.Inherits = new MetadataTypeName {
                                Namespace = "ServiceStack",
                                Name = "QueryDb`1",
                                GenericArgs = new[] {typeName},
                            };
                            
                            var uniqueRoute = "/" + plural + "/{" + id + "}";
                            if (!existingRoutes.Contains(new Tuple<string, string>(uniqueRoute, verb)))
                            {
                                op.Request.Routes.Add(new MetadataRoute {
                                    Path = uniqueRoute, 
                                    Verbs = verb
                                });
                            }
                        }
                        else
                        {
                            op.Request.Implements = new List<MetadataTypeName>(op.Request.Implements) {
                                new MetadataTypeName {
                                    Name = $"I{operation}Db`1",
                                    GenericArgs = new[] {
                                        typeName,
                                    }
                                },
                            }.ToArray();
                            op.Response = new MetadataType {
                                Name = "IdResponse",
                                Namespace = "ServiceStack",
                            };
                        }

                        var allProps = toMetaProps(tableSchema.Columns);
                        switch (operation)
                        {
                            case AutoCrudOperation.Query:
                                // Only Id Property (use implicit conventions)
                                op.Request.Properties = new List<MetadataPropertyType> {
                                    new MetadataPropertyType {
                                        Name = id,
                                        Type = pkField.DataType.Name + (pkField.DataType.IsValueType ? "?" : ""),
                                        TypeNamespace = pkField.DataType.Namespace, 
                                    }
                                };
                                break;
                            case AutoCrudOperation.Create:
                                // all props - AutoIncrement/AutoId PK
                                var autoId = tableSchema.Columns.FirstOrDefault(x => x.IsAutoIncrement);
                                if (autoId != null)
                                {
                                    op.Request.Properties = toMetaProps(tableSchema.Columns)
                                        .Where(x => !x.Name.EqualsIgnoreCase(autoId.ColumnName)).ToList();
                                }
                                else
                                {
                                    op.Request.Properties = allProps;
                                }
                                break;
                            case AutoCrudOperation.Update:
                                // all props
                                op.Request.Properties = allProps;
                                break;
                            case AutoCrudOperation.Patch:
                                // all props
                                op.Request.Properties = allProps;
                                break;
                            case AutoCrudOperation.Delete:
                                // PK prop
                                var pks = tableSchema.Columns.Where(x => x.IsKey).ToList();
                                op.Request.Properties = toMetaProps(pks);
                                break;
                            case AutoCrudOperation.Save:
                                // all props
                                op.Request.Properties = allProps;
                                break;
                        }
                        
                        crudMetadataTypes.Operations.Add(op);
                    }
                }

                if (!existingTypes.Contains(typeName))
                {
                    var modelType = new MetadataType {
                        Name = typeName,
                        Namespace = typesNs,
                        Properties = toMetaProps(tableSchema.Columns, isModel:true),
                    };
                    if (request.NamedConnection != null)
                        modelType.AddAttribute(new NamedConnectionAttribute(request.NamedConnection));
                    if (request.Schema != null)
                        modelType.AddAttribute(new SchemaAttribute(request.Schema));
                    if (typeName != tableSchema.Name)
                        modelType.AddAttribute(new AliasAttribute(tableSchema.Name));
                    crudMetadataTypes.Types.Add(modelType);
                }
            }

            if (feature.IncludeService != null)
            {
                crudMetadataTypes.Operations = crudMetadataTypes.Operations.Where(feature.IncludeService).ToList();
            }
            if (feature.IncludeType != null)
            {
                crudMetadataTypes.Types = crudMetadataTypes.Types.Where(feature.IncludeType).ToList();
            }

            if (feature.ServiceFilter != null)
            {
                foreach (var op in crudMetadataTypes.Operations)
                {
                    feature.ServiceFilter(op, req);
                }
            }

            if (feature.TypeFilter != null)
            {
                foreach (var type in crudMetadataTypes.Types)
                {
                    feature.TypeFilter(type, req);
                }
            }
            
            feature.MetadataTypesFilter?.Invoke(crudMetadataTypes, typesConfig, req);
            
            return new Tuple<MetadataTypes, MetadataTypesConfig>(crudMetadataTypes, typesConfig);
        }
        
    }

    /// <summary>
    /// Instruction for which AutoCrud Services to generate
    /// </summary>
    public class AutoCodeServices
    {
        /// <summary>
        /// Which AutoCrud Operations to include:
        /// - Query
        /// - Create
        /// - Update
        /// - Patch
        /// - Delete
        /// </summary>
        public List<string> IncludeCrudOperations { get; set; }

        /// <summary>
        /// The RDBMS Schema you want AutoQuery Services generated for
        /// </summary>
        public string Schema { get; set; }
        
        /// <summary>
        /// The NamedConnection you want AutoQuery Services generated for
        /// </summary>
        public string NamedConnection { get; set; }
        
        /// <summary>
        /// Include additional C# namespaces
        /// </summary>
        public List<string> AddNamespaces { get; set; }
        
        /// <summary>
        /// Is used as a Whitelist to specify only the types you would like to have code-generated, see:
        /// https://docs.servicestack.net/csharp-add-servicestack-reference#includetypes
        /// </summary>
        public List<string> IncludeTypes { get; set; }
        
        /// <summary>
        /// Is used as a Blacklist to specify which types you would like excluded from being generated. see:
        /// https://docs.servicestack.net/csharp-add-servicestack-reference#excludetypes
        /// </summary>
        public List<string> ExcludeTypes { get; set; }
    }

    public class AutoCodeTypes : NativeTypesBase, IReturn<string>
    {
        /// <summary>
        /// Either 'all' to include all AutoQuery Services or 'new' to include only missing Services and Types
        /// </summary>
        public string Include { get; set; }
        /// <summary>
        /// The language you want
        ///  csharp
        ///  typescript
        ///  java
        ///  kotlin
        ///  swift
        ///  dart
        ///  vbnet
        ///  fsharp
        ///  typescript.d
        /// </summary>
        public string Lang { get; set; }
        /// <summary>
        /// Which AutoCrud Operations to include:
        /// - Query
        /// - Create
        /// - Update
        /// - Patch
        /// - Delete
        /// </summary>
        public List<string> IncludeCrudOperations { get; set; }
        /// <summary>
        /// The RDBMS Schema you want AutoQuery Services generated for
        /// </summary>
        public string Schema { get; set; }
        /// <summary>
        /// The NamedConnection you want AutoQuery Services generated for
        /// </summary>
        public string NamedConnection { get; set; }
        /// <summary>
        /// The Admin AuthSecret to access Service in Release mode
        /// </summary>
        public string AuthSecret { get; set; }
        /// <summary>
        /// Do not use cached DB Table Schemas, re-fetch latest 
        /// </summary>
        public bool? NoCache { get; set; }
    }

    public class AutoCodeSchema : IReturn<AutoCodeSchemaResponse>
    {
        public string Schema { get; set; }
        public string NamedConnection { get; set; }
        public string AuthSecret { get; set; }
        public bool? NoCache { get; set; }
    }

    public class AutoCodeSchemaResponse
    {
        public List<TableSchema> Results { get; set; }
        
        public ResponseStatus ResponseStatus { get; set; }
    }

    public class DbSchema
    {
        public string Schema { get; set; }
        public string NamedConnection { get; set; }
        
        public List<TableSchema> Tables { get; set; } = new List<TableSchema>();
    }

    public class TableSchema
    {
        public string Name { get; set; }
        
        public ColumnSchema[] Columns { get; set; }
        
        public string ErrorType { get; set; }
        public string ErrorMessage { get; set; }
    }
    
    [Restrict(VisibilityTo = RequestAttributes.None)]
    [DefaultRequest(typeof(AutoCodeTypes))]
    public class AutoCrudTypesService : Service
    {
        [AddHeader(ContentType = MimeTypes.PlainText)]
        public object Any(AutoCodeTypes request)
        {
            try
            {
                var feature = HostContext.AssertPlugin<AutoCrudFeature>();
                RequestUtils.AssertIsAdminOrDebugMode(base.Request, adminRole: feature.AccessRole, authSecret: request.AuthSecret);
                
                var src = AutoCrudFeature.GenerateSource(Request, request);
                return src;
            }
            catch (Exception e)
            {
                base.Response.StatusCode = e.ToStatusCode();
                base.Response.StatusDescription = e.GetType().Name;
                return e.ToString();
            }
        }
    }

    [Restrict(VisibilityTo = RequestAttributes.None)]
    [DefaultRequest(typeof(AutoCodeSchema))]
    public class AutoCrudSchemaService : Service
    {
        public object Any(AutoCodeSchema request)
        {
            var feature = HostContext.AssertPlugin<AutoCrudFeature>();
            RequestUtils.AssertIsAdminOrDebugMode(Request, adminRole: feature.AccessRole, authSecret: request.AuthSecret);
            
            var dbFactory = TryResolve<IDbConnectionFactory>();
            var results = request.NoCache == true 
                ? AutoCrudFeature.GetTableSchemas(dbFactory, request.Schema, request.NamedConnection)
                : feature.GetCachedDbSchema(dbFactory, request.Schema, request.NamedConnection).Tables;

            return new AutoCodeSchemaResponse {
                Results = results,
            };
        }
    }

}