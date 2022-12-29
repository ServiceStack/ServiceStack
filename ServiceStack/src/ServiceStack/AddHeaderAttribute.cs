using System;
using System.Net;
using ServiceStack.Web;

namespace ServiceStack
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, Inherited = true, AllowMultiple = true)]
    public class AddHeaderAttribute : RequestFilterAttribute
    {
        public string Name { get; set; }
        public string Value { get; set; }

        public HttpStatusCode Status
        {
            get => (HttpStatusCode)StatusCode.GetValueOrDefault(200);
            set => StatusCode = (int)value;
        }

        public int? StatusCode { get; set; }
        public string StatusDescription { get; set; }

        public AddHeaderAttribute() { }

        public AddHeaderAttribute(string name, string value)
        {
            Name = name;
            Value = value;
        }

        public AddHeaderAttribute(HttpStatusCode status, string statusDescription = null)
        {
            Status = status;
            StatusDescription = statusDescription;
        }

        public override void Execute(IRequest req, IResponse res, object requestDto)
        {
            if (StatusCode != null)
            {
                res.StatusCode = StatusCode.Value;
            }

            if (StatusDescription != null)
            {
                res.StatusDescription = StatusDescription;
            }

            if (!string.IsNullOrEmpty(Name) && !string.IsNullOrEmpty(Value))
            {
                if (Name.EqualsIgnoreCase(HttpHeaders.ContentType))
                {
                    req.ResponseContentType = Value; //Looked at in WriteResponse
                }
                else if (Name == "DefaultContentType")
                {
                    if (!req.HasExplicitResponseContentType)
                    {
                        req.ResponseContentType = Value; //Looked at in WriteResponse
                    }
                }
                else
                {
                    res.AddHeader(Name, Value);
                }
            }
        }

        public string ContentType
        {
            get => Name == HttpHeaders.ContentType ? Value : null;
            set
            {
                Name = HttpHeaders.ContentType;
                Value = value;
            }
        }

        public string DefaultContentType
        {
            get => Name == "DefaultContentType" ? Value : null;
            set
            {
                Name = "DefaultContentType";
                Value = value;
            }
        }

        public string ContentEncoding
        {
            get => Name == HttpHeaders.ContentEncoding ? Value : null;
            set
            {
                Name = HttpHeaders.ContentEncoding;
                Value = value;
            }
        }

        public string ContentLength
        {
            get => Name == HttpHeaders.ContentLength ? Value : null;
            set
            {
                Name = HttpHeaders.ContentLength;
                Value = value;
            }
        }

        public string ContentDisposition
        {
            get => Name == HttpHeaders.ContentDisposition ? Value : null;
            set
            {
                Name = HttpHeaders.ContentDisposition;
                Value = value;
            }
        }

        public string Location
        {
            get => Name == HttpHeaders.Location ? Value : null;
            set
            {
                Name = HttpHeaders.Location;
                Value = value;
            }
        }

        public string SetCookie
        {
            get => Name == HttpHeaders.SetCookie ? Value : null;
            set
            {
                Name = HttpHeaders.SetCookie;
                Value = value;
            }
        }

        public string ETag
        {
            get => Name == HttpHeaders.ETag ? Value : null;
            set
            {
                Name = HttpHeaders.ETag;
                Value = value;
            }
        }

        public string CacheControl
        {
            get => Name == HttpHeaders.CacheControl ? Value : null;
            set
            {
                Name = HttpHeaders.CacheControl;
                Value = value;
            }
        }

        public string LastModified
        {
            get => Name == HttpHeaders.LastModified ? Value : null;
            set
            {
                Name = HttpHeaders.LastModified;
                Value = value;
            }
        }

    }
}