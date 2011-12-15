using System;
using System.Text;
using System.Web;
using System.Web.Mvc;
using ServiceStack.CacheAccess;
using ServiceStack.Text;

namespace $rootnamespace$
{
	public abstract class ControllerBase : Controller
	{
		public ICacheClient Cache { get; set; }

		protected override JsonResult Json(object data, string contentType, Encoding contentEncoding, JsonRequestBehavior behavior)
		{
			return new ServiceStackJsonResult {
				Data = data,
				ContentType = contentType,
				ContentEncoding = contentEncoding
			};
		}
	}

	public class ServiceStackJsonResult : JsonResult
	{
		public override void ExecuteResult(ControllerContext context)
		{
			var response = context.HttpContext.Response;
			response.ContentType = !string.IsNullOrEmpty(ContentType) ? ContentType : "application/json";

			if (ContentEncoding != null)
			{
				response.ContentEncoding = ContentEncoding;
			}

			if (Data != null)
			{
				response.Write(JsonSerializer.SerializeToString(Data));
			}
		}
	}
}