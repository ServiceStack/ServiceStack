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

        public string ContentType
        {
            set 
            { 
                Name = HttpHeaders.ContentType;
                Value = value;
            }
        }

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
    }
}