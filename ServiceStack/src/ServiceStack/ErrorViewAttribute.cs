using System;
using ServiceStack.Web;

namespace ServiceStack
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
    public class ErrorViewAttribute : RequestFilterAttribute
    {
        public string FieldName { get; set; }

        public ErrorViewAttribute() : this("ErrorView") {}
        public ErrorViewAttribute(string fieldName)
        {
            FieldName = fieldName;
            Priority = -1;
        }

        public override void Execute(IRequest req, IResponse res, object requestDto)
        {
            var errorViewGetter = TypeProperties.Get(requestDto.GetType()).GetPublicGetter(FieldName);
            if (errorViewGetter?.Invoke(requestDto) is string errorView)
            {
                req.SetErrorView(errorView);
            }
        }
    }
}