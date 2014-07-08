using System;
using System.Collections.Generic;
using ServiceStack.Host;
using ServiceStack.Web;

namespace ServiceStack.NativeTypes
{
    public class NativeTypesMetadata : INativeTypesMetadata
    {
        private ServiceMetadata meta;
        private MetadataTypesConfig defaults;

        public NativeTypesMetadata(ServiceMetadata meta, MetadataTypesConfig defaults)
        {
            this.meta = meta;
            this.defaults = defaults;
        }

        public MetadataTypesConfig GetConfig(NativeTypesBase req)
        {
            return new MetadataTypesConfig
            {
                BaseUrl = req.BaseUrl ?? defaults.BaseUrl,
                MakePartial = req.MakePartial ?? defaults.MakePartial,
                MakeVirtual = req.MakeVirtual ?? defaults.MakeVirtual,
                AddReturnMarker = req.AddReturnMarker ?? defaults.AddReturnMarker,
                AddDescriptionAsComments = req.AddDescriptionAsComments ?? defaults.AddDescriptionAsComments,
                AddDataContractAttributes = req.AddDataContractAttributes ?? defaults.AddDataContractAttributes,
                MakeDataContractsExtensible = req.MakeDataContractsExtensible ?? defaults.MakeDataContractsExtensible,
                AddIndexesToDataMembers = req.AddIndexesToDataMembers ?? defaults.AddIndexesToDataMembers,
                InitializeCollections = req.InitializeCollections ?? defaults.InitializeCollections,
                AddImplicitVersion = req.AddImplicitVersion ?? defaults.AddImplicitVersion,
                AddResponseStatus = req.AddResponseStatus ?? defaults.AddResponseStatus,
                AddDefaultXmlNamespace = req.AddDefaultXmlNamespace ?? defaults.AddDefaultXmlNamespace,
                DefaultNamespaces = req.DefaultNamespaces ?? defaults.DefaultNamespaces,
                SkipExistingTypes = defaults.SkipExistingTypes,
                IgnoreTypes = defaults.IgnoreTypes,
                TypeAlias = defaults.TypeAlias,
            };
        }

        public MetadataTypes GetMetadataTypes(IRequest req, MetadataTypesConfig config = null)
        {
            if (config == null)
                config = defaults;

            var metadata = new MetadataTypes
            {
                Config = config,
            };

            var skipTypes = new HashSet<Type>(config.SkipExistingTypes);
            config.IgnoreTypes.Each(x => skipTypes.Add(x));

            foreach (var operation in meta.Operations)
            {
                if (!meta.IsVisible(req, operation))
                    continue;

                metadata.Operations.Add(new MetadataOperationType
                {
                    Actions = operation.Actions,
                    Request = operation.RequestType.ToType(),
                    Response = operation.ResponseType.ToType(),
                });

                skipTypes.Add(operation.RequestType);
                if (operation.ResponseType != null)
                {
                    skipTypes.Add(operation.ResponseType);
                }
            }

            foreach (var type in meta.GetAllTypes())
            {
                if (skipTypes.Contains(type))
                    continue;

                metadata.Operations.Add(new MetadataOperationType
                {
                    Request = type.ToType(),
                });

                skipTypes.Add(type);
            }

            var considered = new HashSet<Type>(skipTypes);
            var queue = new Queue<Type>(skipTypes);

            while (queue.Count > 0)
            {
                var type = queue.Dequeue();
                foreach (var pi in type.GetSerializableProperties())
                {
                    if (pi.PropertyType.IsUserType())
                    {
                        if (considered.Contains(pi.PropertyType))
                            continue;

                        considered.Add(pi.PropertyType);
                        queue.Enqueue(pi.PropertyType);
                        metadata.Types.Add(pi.PropertyType.ToType());
                    }
                }

                if (type.BaseType != null
                    && type.BaseType.IsUserType()
                    && !considered.Contains(type.BaseType))
                {
                    considered.Add(type.BaseType);
                    queue.Enqueue(type.BaseType);
                    metadata.Types.Add(type.BaseType.ToType());
                }
            }
            return metadata;
        }
    }
}