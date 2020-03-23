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
    }

    [Route("/autocode/{Lang}")]
    public class AutoCodeTypes : NativeTypesBase, IReturn<string>
    {
        public string Lang { get; set; }
        public List<string> IncludeCrudServices { get; set; }
        public bool? ExcludeExistingCrudServices { get; set; }
        public string Schema { get; set; }
        public string NamedConnection { get; set; }
        public string AuthSecret { get; set; }
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
        private string GetBaseUrl(string baseUrl) => 
            baseUrl ?? HostContext.GetPlugin<NativeTypesFeature>().MetadataTypesConfig.BaseUrl ?? HostContext.AppHost.GetBaseUrl(Request);

        public INativeTypesMetadata NativeTypesMetadata { get; set; }

        [AddHeader(ContentType = MimeTypes.PlainText)]
        public object Any(AutoCodeTypes request)
        {
            var feature = HostContext.AssertPlugin<AutoCodeFeature>();
            RequestUtils.AssertIsAdminOrDebugMode(Request, adminRole: feature.AccessRole, authSecret: request.AuthSecret);
            
            var dbFactory = TryResolve<IDbConnectionFactory>();
            var results = request.NoCache == true 
                ? AutoCodeFeature.GetTableSchemas(dbFactory, request.Schema, request.NamedConnection)
                : feature.GetCachedDbSchema(dbFactory, request.Schema, request.NamedConnection).Tables;
            
            request.BaseUrl = GetBaseUrl(request.BaseUrl);
            if (request.MakePartial == null)
                request.MakePartial = false;
            if (request.MakeVirtual == null)
                request.MakeVirtual = false;

            var typesConfig = NativeTypesMetadata.GetConfig(request);
            var metadataTypes = NativeTypesMetadata.GetMetadataTypes(Request, typesConfig);

            var typesToGenerateMap = new Dictionary<string, TableSchema>();
            foreach (var result in results)
            {
                typesToGenerateMap[result.Name] = result;
            }
            
            var includeCrudServices = request.IncludeCrudServices ?? feature.IncludeCrudServices;
            var servicesToGenerate = new Dictionary<string, Tuple<string, TableSchema>>();
            
            //populate servicesToGenerate
            foreach (var tableSchema in results)
            {
                if (includeCrudServices != null)
                {
                    foreach (var operation in includeCrudServices)
                    {
                        if (!AutoCrudOperation.IsOperation(operation))
                            continue;

                        var name = operation + tableSchema.Name;
                        servicesToGenerate[name] = Tuple.Create(operation, tableSchema);
                    }
                }
            }
            
            //remove unnecessary
            var existingTypes = new HashSet<string>();
            var operations = new List<MetadataOperationType>();
            var types = new List<MetadataType>();
            
            foreach (var op in metadataTypes.Operations)
            {
                if (servicesToGenerate.ContainsKey(op.Request.Name))
                {
                    operations.Add(op);
                    existingTypes.Add(op.Request.Name);
                    if (op.Response != null)
                        existingTypes.Add(op.Response.Name);
                }
            }
            foreach (var metaType in metadataTypes.Types)
            {
                if (typesToGenerateMap.ContainsKey(metaType.Name))
                {
                    types.Add(metaType);
                    existingTypes.Add(metaType.Name);
                }
            }

            var crudMetadataTypes = new MetadataTypes {
                Config = metadataTypes.Config,
                Namespaces = metadataTypes.Namespaces,
                Operations = operations,
                Types = types,
            };
            if (request.ExcludeExistingCrudServices == true)
            {
                crudMetadataTypes.Operations = new List<MetadataOperationType>();
                crudMetadataTypes.Types = new List<MetadataType>();
            }
            
            var opActions = new Dictionary<string,List<string>> {
                { AutoCrudOperation.Query, new List<string> { HttpMethods.Get } },
                { AutoCrudOperation.Create, new List<string> { HttpMethods.Post } },
                { AutoCrudOperation.Update, new List<string> { HttpMethods.Put } },
                { AutoCrudOperation.Patch, new List<string> { HttpMethods.Patch } },
                { AutoCrudOperation.Delete, new List<string> { HttpMethods.Delete } },
                { AutoCrudOperation.Save, new List<string> { HttpMethods.Post } },
            };

            MetadataAttribute toAlias(string alias) => new MetadataAttribute {
                Name = "Alias",
                ConstructorArgs = new List<MetadataPropertyType> {
                    new MetadataPropertyType {Name = "Name", Value = alias, Type = "string"},
                }
            };

            List<MetadataPropertyType> toMetaProps(IEnumerable<ColumnSchema> columns, bool isModel=false)
            {
                var to = new List<MetadataPropertyType>();
                foreach (var column in columns)
                {
                    var pi = column.DataType;
                    if (pi == null)
                        continue;

                    var prop = new MetadataPropertyType {
                        Name = column.ColumnName.SafeVarName(),
                        Type = column.DataType.GetMetadataPropertyType(),
                        IsValueType = pi.IsValueType ? true : (bool?) null,
                        IsSystemType = pi.IsSystemType() ? true : (bool?) null,
                        IsEnum = pi.IsEnum ? true : (bool?) null,
                        TypeNamespace = pi.Namespace,
                    };
                    if (prop.Name != column.ColumnName)
                    {
                        prop.Attributes = new List<MetadataAttribute> { 
                            toAlias(column.ColumnName)
                        };
                    }
                    to.Add(prop);
                }
                return to;
            }

            foreach (var entry in typesToGenerateMap)
            {
                var tableName = entry.Key.SafeVarName();
                var opName = tableName;
                if (tableName.IndexOf('_') >= 0)
                {
                    var parts = opName.Split('_').Where(x => !string.IsNullOrEmpty(x));
                    var pascalName = "";
                    foreach (var part in parts)
                    {
                        pascalName += char.ToUpper(part[0]) + part.Substring(1);
                    }
                    opName = pascalName;
                }
                var tableSchema = entry.Value;
                if (includeCrudServices != null)
                {
                    foreach (var operation in includeCrudServices)
                    {
                        if (!AutoCrudOperation.IsOperation(operation))
                            continue;

                        var requestType = operation + opName;
                        if (existingTypes.Contains(requestType))
                            continue;

                        var verb = opActions[operation].First();
                        var op = new MetadataOperationType {
                            Actions = opActions[operation],
                            Request = new MetadataType {
                                Routes = new List<MetadataRoute> {
                                    new MetadataRoute { Path = "/" + tableName.ToLower(), Verbs = verb }
                                },
                                Name = requestType,
                                Implements = new [] { 
                                    new MetadataTypeName {
                                        Name = $"I{operation}Db`1", 
                                        GenericArgs = new [] {
                                            tableName,
                                        }
                                    },
                                    new MetadataTypeName {
                                        Name = "I" + verb[0] + verb.Substring(1).ToLower(), //marker interface 
                                    },
                                },
                                ReturnVoidMarker = true,
                            }
                        };

                        var allProps = toMetaProps(tableSchema.Columns);
                        switch (operation)
                        {
                            case AutoCrudOperation.Query:
                                // no properties
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
                
                var modelType = new MetadataType {
                    Name = tableName,
                    Properties = toMetaProps(tableSchema.Columns, isModel:true),
                };
                if (tableName != tableSchema.Name)
                {
                    modelType.Attributes = new List<MetadataAttribute> { 
                        toAlias(tableSchema.Name)
                    };
                }
                crudMetadataTypes.Types.Add(modelType);
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

            try 
            { 
                var src = GenerateSourceCode(request, typesConfig, crudMetadataTypes);
                return src;
            }
            catch (Exception e)
            {
                return e.ToString();
            }
        }

        private string GenerateSourceCode(AutoCodeTypes request, MetadataTypesConfig typesConfig,
            MetadataTypes crudMetadataTypes)
        {
            var src = request.Lang switch {
                "csharp" => new CSharpGenerator(typesConfig).GetCode(crudMetadataTypes, base.Request),
                "fsharp" => new FSharpGenerator(typesConfig).GetCode(crudMetadataTypes, base.Request),
                "vbnet" => new VbNetGenerator(typesConfig).GetCode(crudMetadataTypes, base.Request),
                "typescript" => new TypeScriptGenerator(typesConfig).GetCode(crudMetadataTypes, base.Request, NativeTypesMetadata),
                "dart" => new DartGenerator(typesConfig).GetCode(crudMetadataTypes, base.Request, NativeTypesMetadata),
                "swift" => new SwiftGenerator(typesConfig).GetCode(crudMetadataTypes, base.Request),
                "java" => new JavaGenerator(typesConfig).GetCode(crudMetadataTypes, base.Request, NativeTypesMetadata),
                "kotlin" => new KotlinGenerator(typesConfig).GetCode(crudMetadataTypes, base.Request, NativeTypesMetadata),
                "typescript.d" => new FSharpGenerator(typesConfig).GetCode(crudMetadataTypes, base.Request),
            };
            return src;
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