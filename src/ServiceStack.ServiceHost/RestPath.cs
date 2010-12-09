using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using ServiceStack.ServiceModel.Serialization;
using ServiceStack.Text;

namespace ServiceStack.ServiceHost
{
	public class RestPath
		: IRestPath
	{
		private const char PathSeperator = '/';
		private const char ComponentSeperator = '.';
		private const string VariablePrefix = "{";

		readonly bool[] componentsWithSeparators = new bool[0];

		public string DefaultContentType { get; private set; }
		private readonly string restPath;
		private readonly string allowedVerbs;
		private readonly bool allowsAllVerbs;

		private readonly string[] literalsToMatch = new string[0];

		private readonly string[] variablesNames = new string[0];

		public int PathComponentsCount { get; set; }

		public int TotalComponentsCount { get; set; }

		public string[] Verbs = new string[0];

		public Type RequestType { get; private set; }

		public static string[] GetPathPartsForMatching(string pathInfo)
		{
			var parts = pathInfo.ToLower().Split(PathSeperator)
				.Where(x => !string.IsNullOrEmpty(x)).ToArray();
			return parts;
		}

		public static IEnumerable<string> GetFirstMatchHashKeys(string[] pathPartsForMatching)
		{
			var hashPrefix = pathPartsForMatching.Length + "/";
			foreach (var part in pathPartsForMatching)
			{
				yield return hashPrefix + part;
				var subParts = part.Split(ComponentSeperator);
				foreach (var subPart in subParts)
				{
					yield return hashPrefix + subPart;
				}
			}
		}

		public RestPath(Type requestType, RestServiceAttribute attr)
		{
			this.RequestType = requestType;

			this.restPath = attr.Path;
			this.DefaultContentType = attr.DefaultContentType;
			this.allowsAllVerbs = attr.Verbs == null || attr.Verbs == "*";
			if (!this.allowsAllVerbs)
			{
				this.allowedVerbs = attr.Verbs.ToUpper();
			}

			var componentsList = new List<string>();

			//We only split on '.' if the restPath has them. Allows for /{action}.{type}
			var hasSeparators = new List<bool>();
			foreach (var component in this.restPath.Split(PathSeperator))
			{
				if (string.IsNullOrEmpty(component)) continue;

				if (component.Contains(VariablePrefix)
					&& component.Contains(ComponentSeperator))
				{
					hasSeparators.Add(true);
					componentsList.AddRange(component.Split(ComponentSeperator));
				}
				else
				{
					hasSeparators.Add(false);
					componentsList.Add(component);
				}
			}

			var components = componentsList.ToArray();
			this.TotalComponentsCount = components.Length;

			this.literalsToMatch = new string[this.TotalComponentsCount];
			this.variablesNames = new string[this.TotalComponentsCount];
			this.componentsWithSeparators = hasSeparators.ToArray();
			this.PathComponentsCount = this.componentsWithSeparators.Length;

			var sbHashKey = new StringBuilder();
			for (var i = 0; i < components.Length; i++)
			{
				var component = components[i];

				if (component.StartsWith(VariablePrefix))
				{
					this.variablesNames[i] = component.Substring(1, component.Length - 2);
				}
				else
				{
					this.literalsToMatch[i] = component.ToLower();
					sbHashKey.Append(i + "/" + this.literalsToMatch);

					if (this.FirstMatchHashKey == null)
					{
						this.FirstMatchHashKey = this.PathComponentsCount + "/" + this.literalsToMatch[i];
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

		/// <summary>
		/// Provide for quick lookups based on hashes that can be determined from a request url
		/// </summary>
		public string FirstMatchHashKey { get; private set; }

		public string UniqueMatchHashKey { get; private set; }

		private readonly StringMapTypeDeserializer typeDeserializer;

		private readonly Dictionary<string, string> propertyNamesMap = new Dictionary<string, string>();

		/// <summary>
		/// For performance withPathInfoParts should already be a lower case string
		/// to minimize redundant matching operations.
		/// </summary>
		/// <param name="httpMethod"></param>
		/// <param name="withPathInfoParts"></param>
		/// <returns></returns>
		public bool IsMatch(string httpMethod, string[] withPathInfoParts)
		{
			if (withPathInfoParts.Length != this.PathComponentsCount) return false;
			if (!this.allowsAllVerbs && !this.allowedVerbs.Contains(httpMethod)) return false;

			if (!ExplodeComponents(ref withPathInfoParts)) return false;
			if (this.TotalComponentsCount != withPathInfoParts.Length) return false;

			for (var i = 0; i < this.TotalComponentsCount; i++)
			{
				var literalToMatch = this.literalsToMatch[i];
				if (literalToMatch == null) continue;

				if (withPathInfoParts[i] != literalToMatch) return false;
			}

			return true;
		}

		private bool ExplodeComponents(ref string[] withPathInfoParts)
		{
			var totalComponents = new List<string>();
			for (var i = 0; i < withPathInfoParts.Length; i++)
			{
				var component = withPathInfoParts[i];
				if (string.IsNullOrEmpty(component)) continue;

				if (this.PathComponentsCount != this.TotalComponentsCount
					&& this.componentsWithSeparators[i])
				{
					var subComponents = component.Split(ComponentSeperator);
					if (subComponents.Length < 2) return false;
					totalComponents.AddRange(subComponents);
				}
				else
				{
					totalComponents.Add(component);
				}
			}

			withPathInfoParts = totalComponents.ToArray();
			return true;
		}

		public object CreateRequest(string pathInfo)
		{
			return CreateRequest(pathInfo, null);
		}

		public object CreateRequest(string pathInfo, Dictionary<string, string> queryStringAndFormData)
		{
			var requestComponents = pathInfo.Split(PathSeperator)
				.Where(x => !string.IsNullOrEmpty(x)).ToArray();

			ExplodeComponents(ref requestComponents);

			if (requestComponents.Length != this.TotalComponentsCount)
				throw new ArgumentException(string.Format(
					"Path Mismatch: Request Path '{0}' has invalid number of components compared to: '{1}'",
					pathInfo, this.restPath));

			var requestKeyValuesMap = new Dictionary<string, string>();
			for (var i = 0; i < this.TotalComponentsCount; i++)
			{
				var variableName = this.variablesNames[i];
				if (variableName == null) continue;

				string propertyNameOnRequest;
				if (!this.propertyNamesMap.TryGetValue(variableName.ToLower(), out propertyNameOnRequest))
				{
					throw new ArgumentException("Could not find property "
						+ variableName + " on " + RequestType.Name);
				}
				requestKeyValuesMap[propertyNameOnRequest] = requestComponents[i];
			}

			if (queryStringAndFormData != null)
			{
				//Query String and form data can override variable path matches
				//path variables < query string < form data
				foreach (var name in queryStringAndFormData)
				{
					requestKeyValuesMap[name.Key] = name.Value;
				}
			}

			return this.typeDeserializer.CreateFromMap(requestKeyValuesMap);
		}

		public override int GetHashCode()
		{
			return UniqueMatchHashKey.GetHashCode();
		}
	}
}