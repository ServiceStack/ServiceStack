using System;
using ServiceStack.ServiceHost;

namespace ServiceStack.ServiceInterface
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, Inherited = false, AllowMultiple = true)]
    public class AddHeaderAttribute : RequestFilterAttribute
    {
        public string Name { get; set; }
        public string Value { get; set; }

        public AddHeaderAttribute(string name, string value)
        {
            Name = name;
            Value = value;
        }

        public override void Execute(IHttpRequest req, IHttpResponse res, object requestDto)
        {
            if (string.IsNullOrEmpty(Name) || string.IsNullOrEmpty(Value)) return;
            
            res.AddHeader(Name, Value);
        }
    }
}