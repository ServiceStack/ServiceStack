using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using ServiceStack.Common;
using ServiceStack.ServiceModel.Serialization;
using ServiceStack.Text;

namespace ServiceStack.ServiceHost
{
	public class RestPath
		: IRestPath
	{
	    private const string IgnoreParam = "ignore";
		private const string WildCard = "*";
		private const char WildCardChar = '*';
		private const string PathSeperator = "/";
		private const char PathSeperatorChar = '/';
		private const char ComponentSeperator = '.';
		private const string VariablePrefix = "{";

		readonly bool[] componentsWithSeparators = new bool[0];

		private readonly string restPath;
		private readonly string allowedVerbs;
		private readonly bool allowsAllVerbs;
		private readonly bool isWildCardPath;

		private readonly string[] literalsToMatch = new string[0];

		private readonly string[] variablesNames = new string[0];
        private int variableArgsCount;

		/// <summary>
		/// The number of segments separated by '/' determinable by path.Split('/').Length
		/// e.g. /path/to/here.ext == 3
		/// </summary>
		public int PathComponentsCount { get; set; }

		/// <summary>
		/// The total number of segments after subparts have been exploded ('.') 
		/// e.g. /path/to/here.ext == 4
		/// </summary>
        public int TotalComponentsCount { get; set; }

		public string[] Verbs = new string[0];

		public Type RequestType { get; private set; }

		public string Path { get { return this.restPath; } }

        public string Summary { get; private set; }

        public string Notes { get; private set; }

		public bool AllowsAllVerbs { get { return this.allowsAllVerbs; } }

		public string AllowedVerbs { get { return this.allowedVerbs; } }

		public static string[] GetPathPartsForMatching(string pathInfo)
		{
			var parts = pathInfo.ToLower().Split(PathSeperatorChar)
				.Where(x => !string.IsNullOrEmpty(x)).ToArray();
			return parts;
		}

		public static IEnumerable<string> GetFirstMatchHashKeys(string[] pathPartsForMatching)
		{
			var hashPrefix = pathPartsForMatching.Length + PathSeperator;
			return GetPotentialMatchesWithPrefix(hashPrefix, pathPartsForMatching);
		}

		public static IEnumerable<string> GetFirstMatchWildCardHashKeys(string[] pathPartsForMatching)
		{
			const string hashPrefix = WildCard + PathSeperator;
			return GetPotentialMatchesWithPrefix(hashPrefix, pathPartsForMatching);
		}

		private static IEnumerable<string> GetPotentialMatchesWithPrefix(string hashPrefix, string[] pathPartsForMatching)
		{
			foreach (var part in pathPartsForMatching)
			{
				yield return hashPrefix + part;
				var subParts = part.Split(ComponentSeperator);
				if (subParts.Length == 1) continue;

				foreach (var subPart in subParts)
				{
					yield return hashPrefix + subPart;
				}
			}
		}

		public RestPath(Type requestType, string path) : this(requestType, path, null) { }

		public RestPath(Type requestType, string path, string verbs, string summary = null, string notes = null)
		{
			this.RequestType = requestType;
		    this.Summary = summary;
		    this.Notes = notes;
			this.restPath = path;

			this.allowsAllVerbs = verbs == null || verbs == WildCard;
			if (!this.allowsAllVerbs)
			{
				this.allowedVerbs = verbs.ToUpper();
			}

			var componentsList = new List<string>();

			//We only split on '.' if the restPath has them. Allows for /{action}.{type}
			var hasSeparators = new List<bool>();
			foreach (var component in this.restPath.Split(PathSeperatorChar))
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
			string firstLiteralMatch = null;
			var lastVariableMatchPos = -1;

			var sbHashKey = new StringBuilder();
			for (var i = 0; i < components.Length; i++)
			{
				var component = components[i];

				if (component.StartsWith(VariablePrefix))
				{
					this.variablesNames[i] = component.Substring(1, component.Length - 2);
				    this.variableArgsCount++;
					lastVariableMatchPos = i;
				}
				else
				{
					this.literalsToMatch[i] = component.ToLower();
					sbHashKey.Append(i + PathSeperatorChar.ToString() + this.literalsToMatch);

					if (firstLiteralMatch == null)
					{
						firstLiteralMatch = this.literalsToMatch[i];
					}
				}
			}

			if (lastVariableMatchPos != -1)
			{
				var lastVariableMatch = this.variablesNames[lastVariableMatchPos];
				this.isWildCardPath = lastVariableMatch[lastVariableMatch.Length - 1] == WildCardChar;
				if (this.isWildCardPath)
				{
					this.variablesNames[lastVariableMatchPos] = lastVariableMatch.Substring(0, lastVariableMatch.Length - 1);
				}
			}

			this.FirstMatchHashKey = !this.isWildCardPath
				? this.PathComponentsCount + PathSeperator + firstLiteralMatch
				: WildCardChar + PathSeperator + firstLiteralMatch;

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
				if (JsConfig.IncludePublicFields)
				{
					foreach (var fieldInfo in this.RequestType.GetSerializableFields())
					{
						propertyName = fieldInfo.Name;
						propertyNamesMap.Add(propertyName.ToLower(), propertyName);
					}
				}
			}
			catch (Exception)
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

        public int MatchScore(string httpMethod, string[] withPathInfoParts)
        {
            var isMatch = IsMatch(httpMethod, withPathInfoParts);
            if (!isMatch) return -1;

            var exactVerb = httpMethod == AllowedVerbs;
            var score = exactVerb ? 10 : 1;
            score += Math.Max((10 - variableArgsCount), 1) * 100;

            return score;
        }

		/// <summary>
		/// For performance withPathInfoParts should already be a lower case string
		/// to minimize redundant matching operations.
		/// </summary>
		/// <param name="httpMethod"></param>
		/// <param name="withPathInfoParts"></param>
		/// <returns></returns>
		public bool IsMatch(string httpMethod, string[] withPathInfoParts)
		{
			if (withPathInfoParts.Length != this.PathComponentsCount && !this.isWildCardPath) return false;
			if (!this.allowsAllVerbs && !this.allowedVerbs.Contains(httpMethod)) return false;

			if (!ExplodeComponents(ref withPathInfoParts)) return false;
			if (this.TotalComponentsCount != withPathInfoParts.Length && !this.isWildCardPath) return false;

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
			return CreateRequest(pathInfo, null, null);
		}

		public object CreateRequest(string pathInfo, Dictionary<string, string> queryStringAndFormData, object fromInstance)
		{
			var requestComponents = pathInfo.Split(PathSeperatorChar)
				.Where(x => !string.IsNullOrEmpty(x)).ToArray();

			ExplodeComponents(ref requestComponents);

			if (requestComponents.Length != this.TotalComponentsCount)
			{
				var isValidWildCardPath = this.isWildCardPath
					&& requestComponents.Length >= this.TotalComponentsCount - 1;

				if (!isValidWildCardPath)
					throw new ArgumentException(string.Format(
						"Path Mismatch: Request Path '{0}' has invalid number of components compared to: '{1}'",
						pathInfo, this.restPath));
			}

			var requestKeyValuesMap = new Dictionary<string, string>();
			for (var i = 0; i < this.TotalComponentsCount; i++)
			{
				var variableName = this.variablesNames[i];
				if (variableName == null) continue;

				string propertyNameOnRequest;
				if (!this.propertyNamesMap.TryGetValue(variableName.ToLower(), out propertyNameOnRequest))
				{
                    if (IgnoreParam.EqualsIgnoreCase(variableName))
                        continue;

					throw new ArgumentException("Could not find property "
						+ variableName + " on " + RequestType.Name);
				}

				var value = requestComponents[i];
				if (i == this.TotalComponentsCount - 1)
				{
					var sb = new StringBuilder(value);
					for (var j = i + 1; j < requestComponents.Length; j++)
					{
						sb.Append(PathSeperatorChar + requestComponents[j]);
					}
					value = sb.ToString();
				}

				requestKeyValuesMap[propertyNameOnRequest] = value;
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

			return this.typeDeserializer.PopulateFromMap(fromInstance, requestKeyValuesMap);
		}

		public override int GetHashCode()
		{
			return UniqueMatchHashKey.GetHashCode();
		}
	}
}