using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using ServiceStack.ServiceModel.Serialization;
using ServiceStack.Text;

namespace ServiceStack.ServiceHost
{
	public class RestPath
	{
		private readonly string restPath;

		private readonly string[] literalsToMatch = new string[0];

		private readonly string[] variablesNames = new string[0];

		public int ComponentsCount { get; set; }

		public string[] Verbs = new string[0];

		public Type RequestType { get; private set; }

		public RestPath(Type requestType, RestPathAttribute attr)
		{
			this.RequestType = requestType;

			this.restPath = attr.Path;
			var components = this.restPath.Split('/');
			this.ComponentsCount = components.Length;

			this.literalsToMatch = new string[this.ComponentsCount];
			this.variablesNames = new string[this.ComponentsCount];

			var sbHashKey = new StringBuilder();
			for (var i = 0; i < components.Length; i++)
			{
				var component = components[i];
				if (string.IsNullOrEmpty(component)) continue;

				if (component.StartsWith("{"))
				{
					this.variablesNames[i] = component.Substring(1, component.Length - 2);
				}
				else
				{
					this.literalsToMatch[i] = component.ToLower();
					sbHashKey.Append(i + "/" + this.literalsToMatch);
					
					if (this.FirstMatchHashKey == null)
					{
						this.FirstMatchHashKey = i + "/" + this.literalsToMatch;
					}
				}
			}
			this.IsValid = sbHashKey.Length > 0;
			this.UniqueMatchHashKey = sbHashKey.ToString();

			this.typeDeserializer = new StringMapTypeDeserializer(this.RequestType);
			RegisterCaseInsenstivePropertyNameMappings();
		}

		private void RegisterCaseInsenstivePropertyNameMappings()
		{
			var propertyName = "";
			try
			{
				foreach (var propertyInfo in this.RequestType.GetSerializableProperties())
				{
					propertyName = propertyInfo.Name;
					propertyNamesMap.Add(propertyName.ToLower(), propertyName);
				}
			}
			catch (Exception ex)
			{
				throw new AmbiguousMatchException("Property names are case-insensitive: " 
					+ this.RequestType.Name + "." + propertyName);
			}
		}

		public bool IsValid { get; set; }

		public string FirstMatchHashKey { get; private set; }
		
		public string UniqueMatchHashKey { get; private set; }

		private readonly StringMapTypeDeserializer typeDeserializer;

		private readonly Dictionary<string, string> propertyNamesMap = new Dictionary<string, string>();

		/// <summary>
		/// For performance withPathInfoParts should already be a lower case string
		/// to minimize redundant matching operations.
		/// </summary>
		/// <param name="withPathInfoParts"></param>
		/// <returns></returns>
		public bool IsMatch(string[] withPathInfoParts)
		{
			if (withPathInfoParts.Length != this.ComponentsCount) return false;

			for (var i = 0; i < this.ComponentsCount; i++)
			{
				var literalToMatch = this.literalsToMatch[i];
				if (literalToMatch == null) continue;

				if (withPathInfoParts[i] != literalToMatch) return false;
			}

			return true;
		}

		public object CreateRequest(string pathInfo)
		{
			var requestComponents = pathInfo.Split('/');
			if (requestComponents.Length != this.ComponentsCount)
				throw new ArgumentException(string.Format(
					"Path Mismatch: Request Path '{0}' has invalid number of components compared to: '{1}'",
					pathInfo, this.restPath));

			var requestKeyValuesMap = new Dictionary<string, string>();
			for (var i = 0; i < this.ComponentsCount; i++)
			{
				var variableName = this.variablesNames[i];
				if (variableName == null) continue;

				var requestPropertyName = this.propertyNamesMap[variableName.ToLower()];
				requestKeyValuesMap[requestPropertyName] = requestComponents[i];
			}

			return this.typeDeserializer.CreateFromMap(requestKeyValuesMap);
		}

		public override int GetHashCode()
		{
			return UniqueMatchHashKey.GetHashCode();
		}
	}
}