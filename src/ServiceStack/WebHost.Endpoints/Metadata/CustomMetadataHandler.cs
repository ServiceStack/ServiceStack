using System;
using System.IO;
using System.Text;
using System.Web.UI;
using ServiceStack.Common.Utils;
using ServiceStack.Common.Web;
using ServiceStack.Logging;
using ServiceStack.ServiceHost;
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

		public override EndpointType EndpointType
		{
			get { return EndpointType.None; }
		}

		protected override string CreateMessage(Type dtoType)
		{
			try
			{
				var requestObj = ReflectionUtils.PopulateObject(Activator.CreateInstance(dtoType));

				using (var ms = new MemoryStream())
				{
					EndpointHost.ContentTypeFilter.SerializeToStream(
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

		protected override void RenderOperations(HtmlTextWriter writer, IHttpRequest httpReq, Operations allOperations)
		{
			var defaultPage = new OperationsControl
			{
				Title = EndpointHost.Config.ServiceName,
				OperationNames = allOperations.Names,
				MetadataOperationPageBodyHtml = EndpointHost.Config.MetadataOperationPageBodyHtml,
			};

			defaultPage.RenderControl(writer);
		}
	}
}