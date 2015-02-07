using System;
using System.IO;
using System.Text;
using ServiceStack.Host;
using ServiceStack.Logging;
using ServiceStack.Text;

namespace ServiceStack.Metadata
{
    public class CustomMetadataHandler
        : BaseMetadataHandler
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(CustomMetadataHandler));

        public CustomMetadataHandler(string contentType, string format)
        {
            base.ContentType = contentType;
            base.ContentFormat = format;
        }

        public override Format Format
        {
            get { return base.ContentFormat.ToFormat(); }
        }

        protected override string CreateMessage(Type dtoType)
        {
            try
            {
                var requestObj = AutoMappingUtils.PopulateWith(Activator.CreateInstance(dtoType));

                using (var ms = MemoryStreamFactory.GetStream())
                {
                    HostContext.ContentTypes.SerializeToStream(
                        new BasicRequest { ContentType = this.ContentType }, requestObj, ms);

                    return Encoding.UTF8.GetString(ms.ToArray());
                }
            }
            catch (Exception ex)
            {
                var error = string.Format("Error serializing type '{0}' with custom format '{1}'",
                    dtoType.GetOperationName(), this.ContentFormat);
                Log.Error(error, ex);

                return string.Format("{{Unable to show example output for type '{0}' using the custom '{1}' filter}}" + ex.Message,
                    dtoType.GetOperationName(), this.ContentFormat);
            }
        }
    }
}