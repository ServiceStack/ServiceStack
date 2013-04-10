using System;
using System.Linq.Expressions;
using System.Web;

namespace ServiceStack.Html
{
	public class MvcHtmlString
	{
		private delegate MvcHtmlString MvcHtmlStringCreator(string value);
		private static readonly MvcHtmlStringCreator _creator = GetCreator();

		// imporant: this declaration must occur after the _creator declaration
		public static readonly MvcHtmlString Empty = Create(String.Empty);

		private readonly string _value;

		protected MvcHtmlString(string value)
		{
			_value = value ?? String.Empty;
		}

		public static MvcHtmlString Create(string value)
		{
			return _creator(value);
		}

		// in .NET 4, we dynamically create a type that subclasses MvcHtmlString and implements IHtmlString
		private static MvcHtmlStringCreator GetCreator()
		{
			var iHtmlStringType = typeof(HttpContext).Assembly.GetType("System.Web.IHtmlString");
			if (iHtmlStringType != null)
			{
				// first, create the dynamic type
				var dynamicType = DynamicTypeGenerator.GenerateType("DynamicMvcHtmlString", typeof(MvcHtmlString), new[] { iHtmlStringType });

				// then, create the delegate to instantiate the dynamic type
				var valueParamExpr = Expression.Parameter(typeof(string), "value");
				var newObjExpr = Expression.New(dynamicType.GetConstructor(new[] { typeof(string) }), valueParamExpr);
				var lambdaExpr = Expression.Lambda<MvcHtmlStringCreator>(newObjExpr, valueParamExpr);
				return lambdaExpr.Compile();
			}
			else
			{
				// disabling 0618 allows us to call the MvcHtmlString() constructor
#pragma warning disable 0618
				return value => new MvcHtmlString(value);
#pragma warning restore 0618
			}
		}

		public static bool IsNullOrEmpty(MvcHtmlString value)
		{
			return (value == null || value._value.Length == 0);
		}

		// IHtmlString.ToHtmlString()
		public string ToHtmlString()
		{
			return _value;
		}

		public override string ToString()
		{
			return _value;
		}
	}
}
