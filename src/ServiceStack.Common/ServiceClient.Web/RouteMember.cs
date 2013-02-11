using System.Reflection;

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
			return field.GetValue(target);
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
			return property.GetValue(target, null);
		}
	}
}