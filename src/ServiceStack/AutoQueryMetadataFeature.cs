using System;
using System.Collections.Generic;
using System.Linq;
using ServiceStack.DataAnnotations;
using ServiceStack.NativeTypes;

namespace ServiceStack
{
    public class AutoQueryMetadataFeature : IPlugin
    {
        public AutoQueryViewerConfig AutoQueryViewerConfig { get; set; }
        public Action<AutoQueryMetadataResponse> MetadataFilter { get; set; }
        public List<Type> ExportTypes { get; set; } 
        public int? MaxLimit { get; set; }

        public AutoQueryMetadataFeature()
        {
            this.AutoQueryViewerConfig = GetAutoQueryViewerConfigDefaults();
            this.ExportTypes = new List<Type> {
                typeof(RequestLogEntry)
            };
        }

        internal static AutoQueryViewerConfig GetAutoQueryViewerConfigDefaults()
        {
            return new AutoQueryViewerConfig
            {
                Formats = new[] { "json", "xml", "csv" },
                ImplicitConventions = new List<AutoQueryConvention>
                {
                    new AutoQueryConvention { Name = "=", Value = "%" },
                    new AutoQueryConvention { Name = "!=", Value = "%!" },
                    new AutoQueryConvention { Name = ">=", Value = ">%" },
                    new AutoQueryConvention { Name = ">", Value = "%>" },
                    new AutoQueryConvention { Name = "<=", Value = "%<" },
                    new AutoQueryConvention { Name = "<", Value = "<%" },
                    new AutoQueryConvention { Name = "In", Value = "%In" },
                    new AutoQueryConvention { Name = "Between", Value = "%Between" },
                    new AutoQueryConvention { Name = "Starts With", Value = "%StartsWith", Types = "string" },
                    new AutoQueryConvention { Name = "Contains", Value = "%Contains", Types = "string" },
                    new AutoQueryConvention { Name = "Ends With", Value = "%EndsWith", Types = "string" },
                }
            };
        }

        public void Register(IAppHost appHost)
        {
            if (MaxLimit != null)
                AutoQueryViewerConfig.MaxLimit = MaxLimit;

            appHost.RegisterService<AutoQueryMetadataService>();
        }
    }

    public class AutoQueryViewerConfig : IMeta
    {
        /// <summary>
        /// The BaseUrl of the ServiceStack instance (inferred)
        /// </summary>
        public string ServiceBaseUrl { get; set; }
        /// <summary>
        /// Name of the ServiceStack Instance (inferred)
        /// </summary>
        public string ServiceName { get; set; }
        /// <summary>
        /// Textual description of the AutoQuery Services (shown in Home Services list)
        /// </summary>
        public string ServiceDescription { get; set; }
        /// <summary>
        /// Icon for this ServiceStack Instance (shown in Home Services list)
        /// </summary>
        public string ServiceIconUrl { get; set; }
        /// <summary>
        /// The different Content Type formats to display
        /// </summary>
        public string[] Formats { get; set; }
        /// <summary>
        /// The configured MaxLimit for AutoQuery
        /// </summary>
        public int? MaxLimit { get; set; }
        /// <summary>
        /// Whether to publish this Service to the public Services registry
        /// </summary>
        public bool IsPublic { get; set; }
        /// <summary>
        /// Only show AutoQuery Services attributed with [AutoQueryViewer]
        /// </summary>
        public bool OnlyShowAnnotatedServices { get; set; }
        /// <summary>
        /// List of different Search Filters available
        /// </summary>
        public List<AutoQueryConvention> ImplicitConventions { get; set; }

        /// <summary>
        /// The Column which should be selected by default
        /// </summary>
        public string DefaultSearchField { get; set; }
        /// <summary>
        /// The Query Type filter which should be selected by default
        /// </summary>
        public string DefaultSearchType { get; set; }
        /// <summary>
        /// The search text which should be populated by default
        /// </summary>
        public string DefaultSearchText { get; set; }

        /// <summary>
        /// Link to your website users can click to find out more about you
        /// </summary>
        public string BrandUrl { get; set; }
        /// <summary>
        /// A custom logo or image that users can click on to visit your site
        /// </summary>
        public string BrandImageUrl { get; set; }
        /// <summary>
        /// The default color of text
        /// </summary>
        public string TextColor { get; set; }
        /// <summary>
        /// The default color of links
        /// </summary>
        public string LinkColor { get; set; }
        /// <summary>
        /// The default background color of each screen
        /// </summary>
        public string BackgroundColor { get; set; }
        /// <summary>
        /// The default background image of each screen anchored to the bottom left
        /// </summary>
        public string BackgroundImageUrl { get; set; }
        /// <summary>
        /// The default icon for each of your AutoQuery Services
        /// </summary>
        public string IconUrl { get; set; }

        public Dictionary<string, string> Meta { get; set; }
    }

    [Exclude(Feature.Soap)]
    [Route("/autoquery/metadata")]
    public class AutoQueryMetadata : IReturn<AutoQueryMetadataResponse> { }

    public class AutoQueryViewerUserInfo : IMeta
    {
        /// <summary>
        /// Returns true if the User Is Authenticated
        /// </summary>
        public bool IsAuthenticated { get; set; }

        /// <summary>
        /// How many queries are available to this user
        /// </summary>
        public int QueryCount { get; set; }

        public Dictionary<string, string> Meta { get; set; }
    }

    public class AutoQueryConvention
    {
        public string Name { get; set; }
        public string Value { get; set; }
        public string Types { get; set; }
    }

    public class AutoQueryOperation : IMeta
    {
        public string Request { get; set; }
        public string From { get; set; }
        public string To { get; set; }
        public Dictionary<string, string> Meta { get; set; }
    }

    public class AutoQueryMetadataResponse : IMeta
    {
        public AutoQueryViewerConfig Config { get; set; }

        public AutoQueryViewerUserInfo UserInfo { get; set; }

        public List<AutoQueryOperation> Operations { get; set; }

        public List<MetadataType> Types { get; set; }

        public ResponseStatus ResponseStatus { get; set; }

        public Dictionary<string, string> Meta { get; set; }
    }

    [Restrict(VisibilityTo = RequestAttributes.None)]
    public class AutoQueryMetadataService : Service
    {
        public INativeTypesMetadata NativeTypesMetadata { get; set; }

        public object Any(AutoQueryMetadata request)
        {
            if (NativeTypesMetadata == null)
                throw new NotSupportedException("AutoQueryViewer requries NativeTypesFeature");

            var feature = HostContext.GetPlugin<AutoQueryMetadataFeature>();
            var config = feature.AutoQueryViewerConfig;

            if (config == null)
                throw new NotSupportedException("AutoQueryViewerConfig is missing");

            if (config.ServiceBaseUrl == null)
                config.ServiceBaseUrl = base.Request.GetBaseUrl();

            if (config.ServiceName == null)
                config.ServiceName = HostContext.ServiceName;

            var userSession = Request.GetSession();

            var typesConfig = NativeTypesMetadata.GetConfig(new TypesMetadata { BaseUrl = Request.GetBaseUrl() });
            foreach (var type in feature.ExportTypes)
            {
                typesConfig.ExportTypes.Add(type);
            }

            var metadataTypes = NativeTypesMetadata.GetMetadataTypes(Request, typesConfig, 
                op => HostContext.Metadata.IsAuthorized(op, Request, userSession));

            var response = new AutoQueryMetadataResponse {
                Config = config,
                UserInfo = new AutoQueryViewerUserInfo {
                    IsAuthenticated = userSession.IsAuthenticated,
                },
                Operations = new List<AutoQueryOperation>(),
                Types = new List<MetadataType>(),
            };

            var includeTypeNames = new HashSet<string>();

            foreach (var op in metadataTypes.Operations)
            {
                if (op.Request.Inherits != null 
                    && (op.Request.Inherits.Name.StartsWith("QueryDb`") || 
                        op.Request.Inherits.Name.StartsWith("QueryData`"))
                    )
                {
                    if (config.OnlyShowAnnotatedServices)
                    {
                        var serviceAttrs = op.Request.Attributes.Safe();
                        var attr = serviceAttrs.FirstOrDefault(x => x.Name + "Attribute" == nameof(AutoQueryViewerAttribute));
                        if (attr == null)
                            continue;
                    }

                    var inheritArgs = op.Request.Inherits.GenericArgs.Safe().ToArray();
                    response.Operations.Add(new AutoQueryOperation {
                        Request = op.Request.Name,
                        From = inheritArgs.First(),
                        To = inheritArgs.Last(),
                    });

                    response.Types.Add(op.Request);
                    op.Request.GetReferencedTypeNames().Each(x => includeTypeNames.Add(x));
                }
            }

            var allTypes = metadataTypes.GetAllTypes();
            var types = allTypes.Where(x => includeTypeNames.Contains(x.Name)).ToList();

            //Add referenced types to type name search
            types.SelectMany(x => x.GetReferencedTypeNames()).Each(x => includeTypeNames.Add(x));

            //Only need to seek 1-level deep in AutoQuery's (db.LoadSelect)
            types = allTypes.Where(x => includeTypeNames.Contains(x.Name)).ToList();

            response.Types.AddRange(types);

            response.UserInfo.QueryCount = response.Operations.Count;

            feature.MetadataFilter?.Invoke(response);

            return response;
        }
    }
}