using System.Collections.Specialized;
using System.Web;
using ServiceStack;

namespace Check.ServiceInterface
{
    [Route("/changerequest/{Id}")]
    public class ChangeRequest
    {
        public string Id { get; set; }
    }

    public class ChangeRequestResponse
    {
        public string ContentType { get; set; }
        public string Header { get; set; }
        public string QueryString { get; set; }
        public string Form { get; set; }

        public ResponseStatus ResponseStatus { get; set; }
    }

    public class CustomHttpRequest : HttpRequestBase
    {
        private readonly HttpRequestBase original;
        private readonly NameValueCollection queryString = new NameValueCollection();
        private readonly NameValueCollection formData = new NameValueCollection();

        public CustomHttpRequest(object original)
        {
            this.original = (HttpRequestBase)original;

            this.original.ContentType = this.original.ContentType;

            foreach (string key in this.original.QueryString.Keys)
            {
                queryString[key] = this.original.QueryString[key];
            }

            foreach (string key in this.original.Form.Keys)
            {
                formData[key] = this.original.Form[key];
            }
        }

        public override string ContentType
        {
            get
            {
                return original.ContentType;
            }
            set
            {
                original.ContentType = value;
            }
        }

        public override NameValueCollection QueryString
        {
            get { return queryString; }
        }

        public override NameValueCollection Form
        {
            get { return formData; }
        }

        public override NameValueCollection Headers
        {
            get
            {
                return original.Headers;
            }
        }
    }

    public class ChangeRequestService : Service
    {
        public object Any(ChangeRequest request)
        {
            var aspReq = new CustomHttpRequest(base.Request.OriginalRequest) {
                ContentType = MimeTypes.FormUrlEncoded
            };

            aspReq.QueryString["Id"] = request.Id;
            aspReq.Form["Id"] = request.Id;
            aspReq.Headers["Id"] = request.Id;

            return new ChangeRequestResponse {
                ContentType = aspReq.ContentType,
                Header = aspReq.Headers["Id"],
                QueryString = aspReq.QueryString["Id"],
                Form = aspReq.Form["Id"],                
            };
        }
    }
}