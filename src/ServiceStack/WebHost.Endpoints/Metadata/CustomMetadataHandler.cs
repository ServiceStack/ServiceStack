using System;
using System.IO;
using System.Text;
using System.Web.UI;
using ServiceStack.Logging;
using ServiceStack.Server;
using ServiceStack.ServiceHost;
using ServiceStack.Utils;
using ServiceStack.Web;
using ServiceStack.WebHost.Endpoints.Support.Metadata.Controls;

namespace ServiceStack.WebHost.Endpoints.Metadata
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
				var requestObj = ReflectionUtils.PopulateObject(Activator.CreateInstance(dtoType));

				using (var ms = new MemoryStream())
				{
					EndpointHost.ContentTypes.SerializeToStream(
						new SerializationContext(this.ContentType), requestObj, ms);

					return Encoding.UTF8.GetString(ms.ToArray());
				}
			}
			catch (Exception ex)
			{
				var error = string.Format("Error serializing type '{0}' with custom format '{1}'",
					dtoType.Name, this.ContentFormat);
				Log.Error(error, ex);

				return string.Format("{{Unable to show example output for type '{0}' using the custom '{1}' filter}}" + ex.Message,
					dtoType.Name, this.ContentFormat);
			}
		}

        protected override void RenderOperations(HtmlTextWriter writer, IHttpRequest httpReq, ServiceMetadata metadata)
		{
			var defaultPage = new OperationsControl
			{
				Title = EndpointHost.Config.ServiceName,
                OperationNames = metadata.GetOperationNamesForMetadata(httpReq, Format),
                MetadataOperationPageBodyHtml = EndpointHost.Config.MetadataOperationPageBodyHtml,
			};

			defaultPage.RenderControl(writer);
		}
	}
}