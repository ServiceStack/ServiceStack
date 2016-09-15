using System;
using System.ComponentModel;

namespace ServiceStack.Html
{
	public class ViewDataInfo
	{
		private object value;
		private Func<object> valueAccessor;

		public ViewDataInfo()
		{
		}

		public ViewDataInfo(Func<object> valueAccessor)
		{
			this.valueAccessor = valueAccessor;
		}

		public object Container { get; set; }
#if !NETSTANDARD1_6
		public PropertyDescriptor PropertyDescriptor { get; set; }
#endif
		public object Value
		{
			get
			{
				if (valueAccessor != null)
				{
					value = valueAccessor();
					valueAccessor = null;
				}

				return value;
			}
			set
			{
				this.value = value;
				valueAccessor = null;
			}
		}

	}
}
