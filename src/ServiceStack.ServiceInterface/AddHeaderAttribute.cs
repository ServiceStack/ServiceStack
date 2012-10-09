using System;
using ServiceStack.Common.Web;
using ServiceStack.ServiceHost;

namespace ServiceStack.ServiceInterface
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, Inherited = true, AllowMultiple = true)]
    public class AddHeaderAttribute : RequestFilterAttribute
    {
        public string Name { get; set; }
        public string Value { get; set; }
        
        public AddHeaderAttribute() { }

        public AddHeaderAttribute(string name, string value)
        {
            Name = name;
            Value = value;
        }

        public override void Execute(IHttpRequest req, IHttpResponse res, object requestDto)
        {
            if (string.IsNullOrEmpty(Name) || string.IsNullOrEmpty(Value)) return;

            if (Name.Equals(HttpHeaders.ContentType, StringComparison.InvariantCultureIgnoreCase))
            {
                res.ContentType = Value;
            }
            else
            {
                res.AddHeader(Name, Value);
            }
        }

        public string ContentType
        {
            get { return Name == HttpHeaders.ContentType ? Value : null; }
            set
            {
                Name = HttpHeaders.ContentType;
                Value = value;
            }
        }

        public string ContentEncoding
        {
            get { return Name == HttpHeaders.ContentEncoding ? Value : null; }
            set
            {
                Name = HttpHeaders.ContentEncoding;
                Value = value;
            }
        }

        public string ContentLength
        {
            get { return Name == HttpHeaders.ContentLength ? Value : null; }
            set
            {
                Name = HttpHeaders.ContentLength;
                Value = value;
            }
        }

        public string ContentDisposition
        {
            get { return Name == HttpHeaders.ContentDisposition ? Value : null; }
            set
            {
                Name = HttpHeaders.ContentDisposition;
                Value = value;
            }
        }

        public string Location
        {
            get { return Name == HttpHeaders.Location ? Value : null; }
            set
            {
                Name = HttpHeaders.Location;
                Value = value;
            }
        }

        public string SetCookie
        {
            get { return Name == HttpHeaders.SetCookie ? Value : null; }
            set
            {
                Name = HttpHeaders.SetCookie;
                Value = value;
            }
        }

        public string ETag
        {
            get { return Name == HttpHeaders.ETag ? Value : null; }
            set
            {
                Name = HttpHeaders.ETag;
                Value = value;
            }
        }

        public string CacheControl
        {
            get { return Name == HttpHeaders.CacheControl ? Value : null; }
            set
            {
                Name = HttpHeaders.CacheControl;
                Value = value;
            }
        }

        public string LastModified
        {
            get { return Name == HttpHeaders.LastModified ? Value : null; }
            set
            {
                Name = HttpHeaders.LastModified;
                Value = value;
            }
        }

    }
}