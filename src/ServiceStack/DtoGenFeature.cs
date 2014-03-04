using System;
using System.Collections.Generic;
using ServiceStack.DtoGen;
using ServiceStack.Metadata;
using ServiceStack.Web;

namespace ServiceStack
{
    public class DtoGenFeature : IPlugin
    {
        public void Register(IAppHost appHost)
        {
            appHost.RegisterService<DtoGetService>();
        }

        public static MetadataTypes GetMetadataTypes(MetadataTypesConfig config, IRequest httpReq)
        {
            var metadata = new MetadataTypes
            {
                Config = config,
            };
            var existingTypes = new HashSet<Type>
                {
                    typeof (ResponseStatus),
                    typeof (ErrorResponse),
                };

            var meta = HostContext.Metadata;
            foreach (var operation in meta.Operations)
            {
                if (!meta.IsVisible(httpReq, operation))
                    continue;

                metadata.Operations.Add(new MetadataOperationType
                {
                    Actions = operation.Actions,
                    Request = operation.RequestType.ToType(),
                    Response = operation.ResponseType.ToType(),
                });

                existingTypes.Add(operation.RequestType);
                if (operation.ResponseType != null)
                {
                    existingTypes.Add(operation.ResponseType);
                }
            }

            foreach (var type in meta.GetAllTypes())
            {
                if (existingTypes.Contains(type))
                    continue;

                metadata.Operations.Add(new MetadataOperationType
                {
                    Request = type.ToType(),
                });

                existingTypes.Add(type);
            }

            var considered = new HashSet<Type>(existingTypes);
            var queue = new Queue<Type>(existingTypes);

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

    [Route("/dtogen/csharp")]
    public class DtoGenCSharp {} 

    public class DtoGetService : Service
    {
        [AddHeader(ContentType = MimeTypes.PlainText)]
        public object Any(DtoGenCSharp request)
        {
            var typesConfig = HostContext.Config.MetadataTypesConfig;
            var metadataTypes = DtoGenFeature.GetMetadataTypes(typesConfig, base.Request);
            var csharp = CSharpGenerator.GetCode(metadataTypes);
            return csharp;
        }
    }
}