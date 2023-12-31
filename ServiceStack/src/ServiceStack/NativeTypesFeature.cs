using System;
using System.Runtime.Serialization;
using Microsoft.Extensions.DependencyInjection;
using ServiceStack.DataAnnotations;
using ServiceStack.Host;
using ServiceStack.NativeTypes;

namespace ServiceStack;

public class NativeTypesFeature : IPlugin, IConfigureServices, Model.IHasStringId
{
    public string Id { get; set; } = Plugins.NativeTypes;

    public NativeTypesFeature()
    {
        DefaultGenerator = new NativeTypesMetadata(new ServiceMetadata([]), MetadataTypesConfig).GetGenerator();
    }

    public MetadataTypesConfig MetadataTypesConfig { get; set; } = new()
    {
        AddDefaultXmlNamespace = HostConfig.DefaultWsdlNamespace,
        ExportAttributes = [
            typeof(FlagsAttribute),
            typeof(ApiAttribute),
            typeof(ApiResponseAttribute),
            typeof(ApiMemberAttribute),
            typeof(StringLengthAttribute),
            typeof(IgnoreAttribute),
            typeof(IgnoreDataMemberAttribute),
            typeof(ComputedAttribute),
            typeof(MetaAttribute),
            typeof(RequiredAttribute),
            typeof(ReferencesAttribute),
            typeof(StringLengthAttribute),
            typeof(AutoQueryViewerAttribute),
            typeof(AutoQueryViewerFieldAttribute),

            typeof(ValidateRequestAttribute),
            typeof(ValidateIsAuthenticatedAttribute),
            typeof(ValidateIsAdminAttribute),
            typeof(ValidateHasRoleAttribute),
            typeof(ValidateHasPermissionAttribute),

            typeof(ValidateAttribute),
            typeof(ValidateNullAttribute),
            typeof(ValidateEmptyAttribute),
            typeof(ValidateEmailAttribute),
            typeof(ValidateNotNullAttribute),
            typeof(ValidateNotEmptyAttribute),
            typeof(ValidateCreditCardAttribute),
            typeof(ValidateLengthAttribute),
            typeof(ValidateExactLengthAttribute),
            typeof(ValidateMaximumLengthAttribute),
            typeof(ValidateMinimumLengthAttribute),
            typeof(ValidateLessThanAttribute),
            typeof(ValidateLessThanOrEqualAttribute),
            typeof(ValidateGreaterThanAttribute),
            typeof(ValidateGreaterThanOrEqualAttribute),
            typeof(ValidateScalePrecisionAttribute),
            typeof(ValidateRegularExpressionAttribute),
            typeof(ValidateEqualAttribute),
            typeof(ValidateNotEqualAttribute),
            typeof(ValidateInclusiveBetweenAttribute),
            typeof(ValidateExclusiveBetweenAttribute)
            //Already exported in 1st class attrs 
            //typeof(IconAttribute),
            //typeof(InputAttribute),
            //typeof(FieldAttribute),
            //typeof(FieldCssAttribute),
            //typeof(LocodeCssAttribute),
            //typeof(ExplorerCssAttribute),

            // typeof(Intl),
            // typeof(IntlNumber),
            // typeof(IntlDateTime),
            // typeof(IntlRelativeTime),

            //typeof(RefAttribute), in Ref
            //typeof(FormatAttribute), // in Format
        ],
        ExportTypes =
        [
            typeof(IGet),
            typeof(IPost),
            typeof(IPut),
            typeof(IDelete),
            typeof(IPatch),
            typeof(IMeta),
            typeof(IHasSessionId),
            typeof(IHasBearerToken),
            typeof(IHasVersion),
            typeof(ICreateDb<>),
            typeof(IUpdateDb<>),
            typeof(IPatchDb<>),
            typeof(IDeleteDb<>),
            typeof(ISaveDb<>)
        ],
        IgnoreTypes = [],
        IgnoreTypesInNamespaces =
        [
            "ServiceStack",
            "ServiceStack.Auth",
            "ServiceStack.Caching",
            "ServiceStack.Configuration",
            "ServiceStack.Data",
            "ServiceStack.IO",
            "ServiceStack.Logging",
            "ServiceStack.Messaging",
            "ServiceStack.Model",
            "ServiceStack.Redis",
            "ServiceStack.Web",
            "ServiceStack.Admin",
            "ServiceStack.NativeTypes",
            "ServiceStack.Api.OpenApi"
        ],
        DefaultNamespaces =
        [
            "System",
            "System.Collections",
            "System.Collections.Generic",
            "System.Runtime.Serialization",
            "ServiceStack",
            "ServiceStack.DataAnnotations"
        ],
    };

    public static bool DisableTokenVerification { get; set; }

    public static bool AllCollectionProperties(MetadataType type) => true;

    public static bool NonAutoQueryCollectionProperties(MetadataType type) => !(type.IsCrud() || type.IsAnyQueryData());

    public Func<MetadataType, bool> ShouldInitializeCollection { get; set; } = NonAutoQueryCollectionProperties;

    public void ExportAttribute<T>(Func<Attribute, MetadataAttribute> converter) =>
        ExportAttribute(typeof(T), converter);
        
    public void ExportAttribute(Type attributeType, Func<Attribute, MetadataAttribute> converter)
    {
        MetadataTypesConfig.ExportAttributes.Add(attributeType);
        MetadataTypesGenerator.AttributeConverters[attributeType] = converter;
    }

    public MetadataTypesGenerator GetGenerator() =>
        (HostContext.TryResolve<INativeTypesMetadata>() ??
         new NativeTypesMetadata(HostContext.AppHost.Metadata, MetadataTypesConfig))
        .GetGenerator();

    public MetadataTypesGenerator DefaultGenerator { get; private set; }
        
    public void Configure(IServiceCollection services)
    {
        services.AddSingleton<INativeTypesMetadata>(c => 
            new NativeTypesMetadata(HostContext.Metadata, MetadataTypesConfig));

        services.RegisterService<NativeTypesService>();
    }
    
    public void Register(IAppHost appHost)
    {
        var nativeTypesMeta = appHost.Resolve<INativeTypesMetadata>();
        DefaultGenerator = nativeTypesMeta.GetGenerator();
    }
}
