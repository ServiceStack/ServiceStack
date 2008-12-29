using System;
using System.ComponentModel;

namespace ServiceStack.Common.Extensions
{
	public static class EnumExtensions
	{
		/// <summary>
		/// Gets the textual description of the enum if it has one. e.g.
		/// 
		/// <code>
		/// enum UserColors
		/// {
		///     [Description("Bright Red")]
		///     BrightRed
		/// }
		/// UserColors.BrightRed.ToDescription();
		/// </code>
		/// </summary>
		/// <param name="enum"></param>
		/// <returns></returns>
		public static string ToDescription(this Enum @enum) 
		{
			var type = @enum.GetType();
			var memInfo = type.GetMember(@enum.ToString());
			if (memInfo != null && memInfo.Length > 0)
			{
				var attrs = memInfo[0].GetCustomAttributes(
					typeof(DescriptionAttribute),
					false);

				if (attrs != null && attrs.Length > 0)
					return ((DescriptionAttribute)attrs[0]).Description;
			}

			return @enum.ToString();
		}


	}
}