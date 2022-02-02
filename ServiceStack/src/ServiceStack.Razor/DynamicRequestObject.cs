using System;
using System.Collections.Generic;
using System.Dynamic;
using ServiceStack.Html;
using ServiceStack.Web;

namespace ServiceStack.Razor
{
    public class DynamicRequestObject : DynamicDictionary
    {
        private readonly IRequest httpReq;
        private readonly IDictionary<string, object> model;
        private readonly object originalModel;

        public static readonly Dictionary<string, Func<object, object>> ExtensionMethods = new Dictionary<string, Func<object, object>>
            {
                {"AsRaw", o => o.AsRaw()},
                {"AsRawJson", o => o.AsRawJson()},
            };

        public DynamicRequestObject() { }
        public DynamicRequestObject(IRequest httpReq, object model = null)
        {
            this.httpReq = httpReq;
            this.originalModel = model;
            if (model != null)
            {
                this.model = new RouteValueDictionary(model);
            }
        }

        public override bool TryGetMember(GetMemberBinder binder, out object result)
        {
            var name = binder.Name;
            if (model != null)
            {
                if (model.TryGetValue(name, out result))
                {
                    return true;
                }

                Func<object, object> modelFn;
                if (ExtensionMethods.TryGetValue(name, out modelFn))
                {
                    result = (Func<object>)(() => modelFn(originalModel ?? model));
                    return true;
                }
            }

            result = httpReq.GetParam(name);
            return result != null || base.TryGetMember(binder, out result);
        }
    }

    public class DynamicDictionary : System.Dynamic.DynamicObject, IViewBag
    {
        protected readonly Dictionary<string, object> dictionary = new Dictionary<string, object>();
        private readonly RenderingPage page;

        public DynamicDictionary() { }

        public DynamicDictionary(RenderingPage page)
        {
            this.page = page;
        }

        public int Count
        {
            get
            {
                return dictionary.Count;
            }
        }

        public override bool TryGetMember(System.Dynamic.GetMemberBinder binder, out object result)
        {
            result = null;
            var name = binder.Name.ToLower();
            if (!dictionary.TryGetValue(name, out result))
            {
                if (page != null)
                {
                    if (page.ChildPage != null)
                    {
                        page.ChildPage.TypedViewBag.TryGetItem(name, out result);
                    }
                    else if (page.ParentPage != null)
                    {
                        page.ParentPage.TypedViewBag.TryGetItem(name, out result);
                    }
                }

            }

            return true;
        }

        public bool TryGetItem(string name, out object result)
        {
            if (this.dictionary.TryGetValue(name, out result))
                return true;

            return page.ChildPage != null
                && page.ChildPage.TypedViewBag.TryGetItem(name, out result);
        }

        public override bool TrySetMember(System.Dynamic.SetMemberBinder binder, object value)
        {
            dictionary[binder.Name.ToLower()] = value;
            return true;
        }

        public override IEnumerable<string> GetDynamicMemberNames()
        {
            return dictionary.Keys;
        }
    }

    public static class DynamicUtils
    {
        public static ExpandoObject ToExpando(this object anonymousObject)
        {
            IDictionary<string, object> anonymousDictionary = new RouteValueDictionary(anonymousObject);
            IDictionary<string, object> expando = new ExpandoObject();
            foreach (var item in anonymousDictionary)
                expando.Add(item);
            return (ExpandoObject)expando;
        }
    }
}