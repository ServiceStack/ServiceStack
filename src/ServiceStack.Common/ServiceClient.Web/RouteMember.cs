using System.Reflection;
using ServiceStack.Text;

namespace ServiceStack.ServiceClient.Web
{
	internal abstract class RouteMember
	{
		public abstract object GetValue(object target);
	}

	internal class FieldRouteMember : RouteMember
	{
		private readonly FieldInfo field;

		public FieldRouteMember(FieldInfo field)
		{
			this.field = field;
		}

		public override object GetValue(object target)
		{
			var v = field.GetValue(target);
			if (Equals(v, field.FieldType.GetDefaultValue())) return null;
			return v;
		}
	}

	internal class PropertyRouteMember : RouteMember
	{
		private readonly PropertyInfo property;

		public PropertyRouteMember(PropertyInfo property)
		{
			this.property = property;
		}

		public override object GetValue(object target)
		{
			var v = property.GetValue(target, null);
			if (Equals(v, property.PropertyType.GetDefaultValue())) return null;
			return v;
		}
	}
}