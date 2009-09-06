using System;

namespace ServiceStack.Common
{
	/// <summary>
	/// Creates a Unified Resource Name (URN) with the following formats:
	/// 
	///		- urn:{TypeName}:{IdFieldValue}						e.g. urn:UserSession:1
	///		- urn:{TypeName}:{IdFieldName}:{IdFieldValue}		e.g. urn:UserSession:UserId:1			
	/// 
	/// </summary>
	public class UrnId
	{
		private const char FieldSeperator = ':';
		public string TypeName { get; private set; }
		public string IdFieldValue { get; private set; }
		public string IdFieldName { get; private set; }

		const int HasNoIdFieldName = 3;
		const int HasIdFieldName = 4;

		private UrnId() { }

		public static UrnId Parse(string urnId)
		{
			var urnParts = urnId.Split(FieldSeperator);
			if (urnParts.Length == HasNoIdFieldName)
			{
				return new UrnId { TypeName = urnParts[1], IdFieldValue = urnParts[2] };
			}
			if (urnParts.Length == HasIdFieldName)
			{
				return new UrnId { TypeName = urnParts[1], IdFieldName = urnParts[2], IdFieldValue = urnParts[3] };
			}
			throw new ArgumentException("Cannot parse invalid urn: '{0}'", urnId);
		}

		public static string Create(string objectTypeName, string idFieldValue)
		{
			if (objectTypeName.Contains(FieldSeperator.ToString()))
			{
				throw new ArgumentException("objectTypeName cannot have the illegal characters: ':'", "objectTypeName");
			}
			if (idFieldValue.Contains(FieldSeperator.ToString()))
			{
				throw new ArgumentException("idFieldValue cannot have the illegal characters: ':'", "idFieldValue");
			}
			return string.Format("urn:{0}:{1}", objectTypeName, idFieldValue);
		}

		public static string Create(Type objectType, string idFieldValue)
		{
			if (idFieldValue.Contains(FieldSeperator.ToString()))
			{
				throw new ArgumentException("idFieldValue cannot have the illegal characters: ':'", "idFieldValue");
			}
			return string.Format("urn:{0}:{1}", objectType.Name, idFieldValue);
		}

		public static string Create(Type objectType, string idFieldName, string idFieldValue)
		{
			if (idFieldValue.Contains(FieldSeperator.ToString()))
			{
				throw new ArgumentException("idFieldValue cannot have the illegal characters: ':'", "idFieldValue");
			}
			if (idFieldName.Contains(FieldSeperator.ToString()))
			{
				throw new ArgumentException("idFieldName cannot have the illegal characters: ':'", "idFieldName");
			}
			return string.Format("urn:{0}:{1}:{2}", objectType.Name, idFieldName, idFieldValue);
		}

		public static string GetStringId(string urn)
		{
			return Parse(urn).IdFieldValue;
		}

		public static Guid GetGuidId(string urn)
		{
			return new Guid(Parse(urn).IdFieldValue);
		}

		public static long GetLongId(string urn)
		{
			return long.Parse(Parse(urn).IdFieldValue);
		}
	}
}