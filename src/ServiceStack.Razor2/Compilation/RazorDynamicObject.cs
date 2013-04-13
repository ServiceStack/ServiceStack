using System;
using System.Diagnostics;
using System.Dynamic;
using ServiceStack.Html;
using ServiceStack.Text;

namespace ServiceStack.Razor2.Compilation
{
    /// <summary>
    /// Defines a dynamic object.
    /// </summary>
    internal class RazorDynamicObject : DynamicObject
    {
        /// <summary>
        /// Gets or sets the model.
        /// </summary>
        public object Model { get; set; }

        /// <summary>
        /// Gets the value of the specified member.
        /// </summary>
        /// <param name="binder">The current binder.</param>
        /// <param name="result">The member result.</param>
        /// <returns>True.</returns>
        [DebuggerStepThrough]
        public override bool TryGetMember(GetMemberBinder binder, out object result)
        {
            var dynamicObject = Model as RazorDynamicObject;
            if (dynamicObject != null)
                return dynamicObject.TryGetMember(binder, out result);

            Type modelType = Model.GetType();
            var prop = modelType.GetProperty(binder.Name);
            if (prop == null)
            {
                result = null;
                return false;
            }

            object value = prop.GetValue(Model, null);
            if (value == null)
            {
                result = value;
                return true;
            }

            Type valueType = value.GetType();
            result = (CompilerServices.IsAnonymousType(valueType))
                         ? new RazorDynamicObject { Model = value }
                         : value;
            return true;
        }

        public override bool TryConvert(ConvertBinder binder, out object result)
        {
            if (binder.ReturnType == Model.GetType())
            {
                result = Model;
                return true;
            }
            return base.TryConvert(binder, out result);
        }

        public override bool TryInvokeMember(InvokeMemberBinder binder, object[] args, out object result)
        {
            if (binder.Name == "AsRawJson")
            {
                result = MvcHtmlString.Create(Model.ToJson());
                return true;
            }
            if (binder.Name == "AsRaw")
            {
                result = MvcHtmlString.Create((Model ?? "").ToString());
                return true;
            }
            return base.TryInvokeMember(binder, args, out result);
        }
        
    }
}
