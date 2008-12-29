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
		private const char FIELD_SEPERATOR = ':';
		public string TypeName { get; private set; }
		public string IdFieldValue { get; private set; }
		public string IdFieldName { get; private set; }

		private UrnId() { }

		public static UrnId Parse(string urnId)
		{
			const int HAS_NO_ID_FIELD_NAME = 2;
			const int HAS_ID_FIELD_NAME = 3;

			var urnParts = urnId.Split(FIELD_SEPERATOR);
			if (urnParts.Length == HAS_NO_ID_FIELD_NAME)
			{
				return new UrnId { TypeName = urnParts[0], IdFieldValue = urnParts[1] };
			}
			if (urnParts.Length == HAS_ID_FIELD_NAME)
			{
				return new UrnId { TypeName = urnParts[0], IdFieldName = urnParts[1], IdFieldValue = urnParts[2] };
			}
			throw new ArgumentException("Cannot parse invalid urn: '{0}'", urnId);
		}

		public static string Create(string objectTypeName, string idFieldValue)
		{
			if (objectTypeName.Contains(FIELD_SEPERATOR.ToString()))
			{
				throw new ArgumentException("objectTypeName cannot have the illegal characters: ':'", "objectTypeName");
			}
			if (idFieldValue.Contains(FIELD_SEPERATOR.ToString()))
			{
				throw new ArgumentException("idFieldValue cannot have the illegal characters: ':'", "idFieldValue");
			}
			return string.Format("urn:{0}:{1}", objectTypeName, idFieldValue);
		}

		public static string Create(Type objectType, string idFieldValue)
		{
			if (idFieldValue.Contains(FIELD_SEPERATOR.ToString()))
			{
				throw new ArgumentException("idFieldValue cannot have the illegal characters: ':'", "idFieldValue");
			}
			return string.Format("urn:{0}:{1}", objectType.Name, idFieldValue);
		}

		public static string Create(Type objectType, string idFieldName, string idFieldValue)
		{
			if (idFieldValue.Contains(FIELD_SEPERATOR.ToString()))
			{
				throw new ArgumentException("idFieldValue cannot have the illegal characters: ':'", "idFieldValue");
			}
			if (idFieldName.Contains(FIELD_SEPERATOR.ToString()))
			{
				throw new ArgumentException("idFieldName cannot have the illegal characters: ':'", "idFieldName");
			}
			return string.Format("urn:{0}:{1}:{2}", objectType.Name, idFieldName, idFieldValue);
		}
	}
}