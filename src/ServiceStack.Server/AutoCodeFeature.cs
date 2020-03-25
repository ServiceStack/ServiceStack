using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using ServiceStack.Configuration;
using ServiceStack.Data;
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
    public class AutoCodeFeature : IPlugin
    {
        public List<string> IncludeCrudServices { get; set; } = new List<string> {
            AutoCrudOperation.Query,
            AutoCrudOperation.Create,
            AutoCrudOperation.Update,
            AutoCrudOperation.Patch,
            AutoCrudOperation.Delete,
        };
        
        public string AccessRole { get; set; } = RoleNames.Admin;
        
        internal ConcurrentDictionary<string, DbSchema> CachedDbSchemas { get; } 
            = new ConcurrentDictionary<string, DbSchema>();

        private const string NoSchema = "__noschema";

        public DbSchema GetCachedDbSchema(IDbConnectionFactory dbFactory, string schema=null, string namedConnection=null)
        {
            var key = schema ?? NoSchema;
            if (namedConnection != null)
                key += "::" + namedConnection;

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
            appHost.RegisterService(typeof(AutoCodeSchemaService));
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
            if (string.IsNullOrEmpty(request.Include))
                throw new ArgumentNullException(nameof(request.Include));
            if (request.Include != "all" && request.Include != "new")
                throw new ArgumentException(
                    "'Include' must be either 'all' to include all AutoQuery Services or 'new' to include only missing Services and Types", 
                    nameof(request.Include));
            
            var metadata = req.Resolve<INativeTypesMetadata>();
            var feature = HostContext.AssertPlugin<AutoCodeFeature>();
            RequestUtils.AssertIsAdminOrDebugMode(req, adminRole: feature.AccessRole, authSecret: request.AuthSecret);
            
            var dbFactory = req.TryResolve<IDbConnectionFactory>();
            var results = request.NoCache == true 
                ? GetTableSchemas(dbFactory, request.Schema, request.NamedConnection)
                : feature.GetCachedDbSchema(dbFactory, request.Schema, request.NamedConnection).Tables;

            request.BaseUrl ??= HostContext.GetPlugin<NativeTypesFeature>().MetadataTypesConfig.BaseUrl ?? req.GetBaseUrl();
            if (request.MakePartial == null)
                request.MakePartial = false;
            if (request.MakeVirtual == null)
                request.MakeVirtual = false;

            var appHost = HostContext.AppHost;
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
            
            var includeCrudServices = request.IncludeCrudServices ?? feature.IncludeCrudServices;
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
                    if (typeName != tableSchema.Name)
                    {
                        modelType.Attributes = new List<MetadataAttribute> { 
                            toAlias(tableSchema.Name)
                        };
                    }
                    crudMetadataTypes.Types.Add(modelType);
                }
            }
            
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
    }

    [Route("/autocode/{Include}/{Lang}")]
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
        public List<string> IncludeCrudServices { get; set; }
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
        /// 
        /// </summary>
        public bool? NoCache { get; set; }
    }

    [Route("/autocode/schema")]
    [Route("/autocode/schema/{Schema}")]
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
    
    [DefaultRequest(typeof(AutoCodeSchema))]
    public class AutoCodeTypesService : Service
    {
        [AddHeader(ContentType = MimeTypes.PlainText)]
        public object Any(AutoCodeTypes request)
        {
            try
            {
                var src = AutoCodeFeature.GenerateSource(Request, request);
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

    [DefaultRequest(typeof(AutoCodeSchema))]
    public class AutoCodeSchemaService : Service
    {
        public object Any(AutoCodeSchema request)
        {
            var feature = HostContext.AssertPlugin<AutoCodeFeature>();
            RequestUtils.AssertIsAdminOrDebugMode(Request, adminRole: feature.AccessRole, authSecret: request.AuthSecret);
            
            var dbFactory = TryResolve<IDbConnectionFactory>();
            var results = request.NoCache == true 
                ? AutoCodeFeature.GetTableSchemas(dbFactory, request.Schema, request.NamedConnection)
                : feature.GetCachedDbSchema(dbFactory, request.Schema, request.NamedConnection).Tables;

            return new AutoCodeSchemaResponse {
                Results = results,
            };
        }
    }
}