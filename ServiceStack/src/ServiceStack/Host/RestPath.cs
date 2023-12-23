using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using ServiceStack.Serialization;
using ServiceStack.Text;
using ServiceStack.Web;

namespace ServiceStack.Host;

public class RestPath
    : IRestPath
{
    private const string WildCard = "*";
    private const char WildCardChar = '*';
    private const string PathSeparator = "/";
    private const char PathSeparatorChar = '/';
    private static readonly char[] PathSeparatorCharArray = {'/'};
    private const char ComponentSeparator = '.';
    private const string VariablePrefix = "{";

    //in most cases URL parts are short-lengthly and we can create lookup for
    //most used constant prefix values for path parts
    //and reuse them to avoid slow string concatenations operations
    private static readonly string[] prefixesLookup = {
        "0" + PathSeparator, "1" + PathSeparator, "2" + PathSeparator, "3" + PathSeparator, 
        "4" + PathSeparator, "5" + PathSeparator, "6" + PathSeparator, "7" + PathSeparator, 
        "8" + PathSeparator, "9" + PathSeparator, "10" + PathSeparator, "11" + PathSeparator, 
        "12" + PathSeparator, "13" + PathSeparator, "14" + PathSeparator, "15" + PathSeparator
    };

    private readonly bool[] componentsWithSeparators;

    public bool IsWildCardPath { get; private set; }

    private readonly string[] literalsToMatch;

    private readonly string[] variablesNames;

    private readonly bool[] isWildcard;
    private readonly int wildcardCount = 0;

    public int VariableArgsCount { get; set; }

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

    public string[] Verbs => AllowsAllVerbs 
        ? new[] { ActionContext.AnyAction } 
        : AllowedVerbs.Split(new[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries);

    public Type RequestType { get; }

    public string Path { get; }

    public string Summary { get; set; }

    public string Notes { get; set; }
        
    public string MatchRule { get; private set; }

    private Func<IHttpRequest, bool> matchRuleFn;

    public bool AllowsAllVerbs { get; }

    public string AllowedVerbs { get; }

    public int Priority { get; set; } //passed back to RouteAttribute

    public static string[] GetPathPartsForMatching(string pathInfo)
    {
        var parts = pathInfo.ToLowerInvariant()
            .Split(PathSeparatorCharArray, StringSplitOptions.RemoveEmptyEntries);

        return parts;
    }

    private static string GetHashPrefix(string[] pathPartsForMatching)
    {
        //array lookup for predefined hashes is 7 times faster than switch-case [0 to 15]
        //and 20 times faster than simple string concatenation
        return pathPartsForMatching.Length < prefixesLookup.Length 
            ? prefixesLookup[pathPartsForMatching.Length]
            : pathPartsForMatching.Length + PathSeparator;
    }

    public static IEnumerable<string> GetFirstMatchHashKeys(string[] pathPartsForMatching)
    {
        return GetPotentialMatchesWithPrefix(GetHashPrefix(pathPartsForMatching), pathPartsForMatching);
    }

    public static IEnumerable<string> GetFirstMatchWildCardHashKeys(string[] pathPartsForMatching)
    {
        const string hashPrefix = WildCard + PathSeparator;
        return GetPotentialMatchesWithPrefix(hashPrefix, pathPartsForMatching);
    }

    private static IEnumerable<string> GetPotentialMatchesWithPrefix(string hashPrefix, string[] pathPartsForMatching)
    {
        foreach (var part in pathPartsForMatching)
        {
            yield return hashPrefix + part;
            var subParts = part.Split(ComponentSeparator);
            if (subParts.Length == 1) continue;

            foreach (var subPart in subParts)
            {
                yield return hashPrefix + subPart;
            }
        }
    }

    public RestRoute ToRestRoute()
    {
        return new RestRoute(RequestType, Path, AllowedVerbs, 0);
    }

    public RestPath(Type requestType, string path) : this(requestType, path, null) { }

    public RestPath(Type requestType, string path, string verbs, string summary = null, string notes = null, string matchRule = null)
    {
        this.RequestType = requestType;
        this.Summary = summary;
        this.Notes = notes;
        this.MatchRule = matchRule;
        this.Path = path;

        this.AllowsAllVerbs = verbs is null or WildCard;
        if (!this.AllowsAllVerbs)
        {
            this.AllowedVerbs = verbs?.ToUpper();
        }

        var componentsList = new List<string>();

        //We only split on '.' if the restPath has them. Allows for /{action}.{type}
        var hasSeparators = new List<bool>();
        foreach (var component in this.Path.Split(PathSeparatorCharArray, StringSplitOptions.RemoveEmptyEntries))
        {
            if (component.Contains(VariablePrefix)
                && component.Contains(ComponentSeparator))
            {
                hasSeparators.Add(true);
                componentsList.AddRange(component.Split(ComponentSeparator));
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
        this.isWildcard = new bool[this.TotalComponentsCount];
        this.componentsWithSeparators = hasSeparators.ToArray();
        this.PathComponentsCount = this.componentsWithSeparators.Length;
        string firstLiteralMatch = null;

        var sbHashKey = StringBuilderCache.Allocate();
        for (var i = 0; i < components.Length; i++)
        {
            var component = components[i];

            if (component.StartsWith(VariablePrefix))
            {
                var variableName = component.Substring(1, component.Length - 2);
                if (variableName[variableName.Length - 1] == WildCardChar)
                {
                    this.isWildcard[i] = true;
                    variableName = variableName.Substring(0, variableName.Length - 1);
                }
                this.variablesNames[i] = variableName;
                this.VariableArgsCount++;
            }
            else
            {
                this.literalsToMatch[i] = component.ToLowerInvariant();
                sbHashKey.Append(i + PathSeparator + this.literalsToMatch);

                if (firstLiteralMatch == null)
                {
                    firstLiteralMatch = this.literalsToMatch[i];
                }
            }
        }

        for (var i = 0; i < components.Length - 1; i++)
        {
            if (!this.isWildcard[i]) continue;
            if (this.literalsToMatch[i + 1] == null)
            {
                throw new ArgumentException(
                    "A wildcard path component must be at the end of the path or followed by a literal path component.");
            }
        }

        this.wildcardCount = this.isWildcard.Count(x => x);
        this.IsWildCardPath = this.wildcardCount > 0;

        this.FirstMatchHashKey = !this.IsWildCardPath
            ? this.PathComponentsCount + PathSeparator + firstLiteralMatch
            : WildCardChar + PathSeparator + firstLiteralMatch;

        this.IsValid = sbHashKey.Length > 0;
        this.UniqueMatchHashKey = StringBuilderCache.ReturnAndFree(sbHashKey);

        this.typeDeserializer = new StringMapTypeDeserializer(this.RequestType);
        RegisterCaseInsensitivePropertyNameMappings();
    }

    private void RegisterCaseInsensitivePropertyNameMappings()
    {
        var propertyName = "";
        try
        {
            foreach (var propertyInfo in this.RequestType.GetSerializableProperties())
            {
                propertyName = propertyInfo.Name;
                propertyNamesMap.Add(propertyName, propertyName);
            }
            if (JsConfig.IncludePublicFields)
            {
                foreach (var fieldInfo in this.RequestType.GetSerializableFields())
                {
                    propertyName = fieldInfo.Name;
                    propertyNamesMap.Add(propertyName, propertyName);
                }
            }
        }
        catch (Exception)
        {
            throw new AmbiguousMatchException("Property names are case-insensitive: "
                                              + this.RequestType.GetOperationName() + "." + propertyName);
        }
    }

    public bool IsValid { get; set; }

    /// <summary>
    /// Provide for quick lookups based on hashes that can be determined from a request url
    /// </summary>
    public string FirstMatchHashKey { get; private set; }

    public string UniqueMatchHashKey { get; }

    private readonly StringMapTypeDeserializer typeDeserializer;

    private readonly Dictionary<string, string> propertyNamesMap = new(StringComparer.OrdinalIgnoreCase);

    public static Func<RestPath, string, string[], int> CalculateMatchScore { get; set; }

    public int MatchScore(string httpMethod, string[] withPathInfoParts)
    {
        if (CalculateMatchScore != null)
            return CalculateMatchScore(this, httpMethod, withPathInfoParts);

        var isMatch = IsMatch(httpMethod, withPathInfoParts, out var wildcardMatchCount);
        if (!isMatch) return -1;

        var score = 0;

        //Routes with least wildcard matches get the highest score
        score += Math.Max((100 - wildcardMatchCount), 1) * 1000;

        //Routes with less variable (and more literal) matches
        score += Math.Max((10 - VariableArgsCount), 1) * 100;

        //Exact verb match is better than ANY
        var exactVerb = httpMethod == AllowedVerbs;
        score += exactVerb ? 10 : 1;

        return score;
    }

    /// <summary>
    /// For performance withPathInfoParts should already be a lower case string
    /// to minimize redundant matching operations.
    /// </summary>
    /// <returns></returns>
    public bool IsMatch(IHttpRequest httpReq)
    {
        var pathInfo = httpReq.PathInfo;

        var matchFn = GetRequestRule();
        if (matchFn != null)
        {
            var validRoute = matchFn(httpReq);
            if (!validRoute)
                return false;
        }

        pathInfo = RestHandler.GetSanitizedPathInfo(pathInfo, out var contentType);
                        
        var pathInfoParts = GetPathPartsForMatching(pathInfo);

        return IsMatch(httpReq.HttpMethod, pathInfoParts, out var wildcardMatchCount);
    }

    public void AfterInit()
    {
        if (this.MatchRule != null)
        {
            if (!HostContext.Config.RequestRules.TryGetValue(this.MatchRule, out this.matchRuleFn))
            {
                var regexParts = this.MatchRule.SplitOnFirst("=~");
                if (regexParts.Length == 2)
                {
                    var field = regexParts[0].Trim();
                    var regex = regexParts[1].Trim();
                    var compiledRegex = new Regex(regex, RegexOptions.Compiled);

                    matchRuleFn = req =>
                    {
                        var reqValue = req.GetRequestValue(field);
                        return compiledRegex.IsMatch(reqValue);
                    };
                }
                else
                {
                    var exactMatchParts = this.MatchRule.SplitOnFirst('=');
                    if (exactMatchParts.Length == 2)
                    {
                        var field = exactMatchParts[0].Trim();
                        var exactMatch = exactMatchParts[1].Trim();
                        var isNull = exactMatch == "null";

                        matchRuleFn = req =>
                        {
                            var reqValue = req.GetRequestValue(field);
                            if (reqValue == null)
                                return isNull;
                            return reqValue == exactMatch;
                        };
                    }
                    else throw new NotSupportedException($"Unknown Matches Rule '{MatchRule}' in Route '{Path}'");
                }
            }
        }
    }

    public Func<IHttpRequest, bool> GetRequestRule() => this.matchRuleFn;

    /// <summary>
    /// For performance withPathInfoParts should already be a lower case string
    /// to minimize redundant matching operations.
    /// </summary>
    /// <param name="httpMethod"></param>
    /// <param name="withPathInfoParts"></param>
    /// <param name="wildcardMatchCount"></param>
    /// <returns></returns>
    public bool IsMatch(string httpMethod, string[] withPathInfoParts, out int wildcardMatchCount)
    {
        wildcardMatchCount = 0;

        if (withPathInfoParts.Length != this.PathComponentsCount && !this.IsWildCardPath) return false;
        if (!this.AllowsAllVerbs && !this.AllowedVerbs.Contains(httpMethod.ToUpper())) return false;

        if (!ExplodeComponents(ref withPathInfoParts)) return false;
        if (this.TotalComponentsCount != withPathInfoParts.Length && !this.IsWildCardPath) return false;

        int pathIx = 0;
        for (var i = 0; i < this.TotalComponentsCount; i++)
        {
            if (this.isWildcard[i])
            {
                if (i < this.TotalComponentsCount - 1)
                {
                    // Continue to consume up until a match with the next literal
                    while (pathIx < withPathInfoParts.Length && withPathInfoParts[pathIx] != this.literalsToMatch[i + 1])
                    {
                        pathIx++;
                        wildcardMatchCount++;
                    }

                    // Ensure there are still enough parts left to match the remainder
                    if ((withPathInfoParts.Length - pathIx) < (this.TotalComponentsCount - i - 1))
                    {
                        return false;
                    }
                }
                else
                {
                    // A wildcard at the end matches the remainder of path
                    wildcardMatchCount += withPathInfoParts.Length - pathIx;
                    pathIx = withPathInfoParts.Length;
                }
            }
            else
            {
                var literalToMatch = this.literalsToMatch[i];
                if (literalToMatch == null)
                {
                    // Matching an ordinary (non-wildcard) variable consumes a single part
                    pathIx++;
                    continue;
                }

                if (withPathInfoParts.Length <= pathIx || withPathInfoParts[pathIx] != literalToMatch) return false;
                pathIx++;
            }
        }

        return pathIx == withPathInfoParts.Length;
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
                var subComponents = component.Split(ComponentSeparator);
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
        var requestComponents = pathInfo.Split(PathSeparatorCharArray, StringSplitOptions.RemoveEmptyEntries);

        ExplodeComponents(ref requestComponents);

        if (requestComponents.Length != this.TotalComponentsCount)
        {
            var isValidWildCardPath = this.IsWildCardPath
                                      && requestComponents.Length >= this.TotalComponentsCount - this.wildcardCount;

            if (!isValidWildCardPath)
                throw new ArgumentException($"Path Mismatch: Request Path '{pathInfo}' has invalid number of components compared to: '{this.Path}'");
        }

        var requestKeyValuesMap = new Dictionary<string, string>();
        var pathIx = 0;
        for (var i = 0; i < this.TotalComponentsCount; i++)
        {
            var variableName = this.variablesNames[i];
            if (variableName == null)
            {
                pathIx++;
                continue;
            }

            if (!this.propertyNamesMap.TryGetValue(variableName, out var propertyNameOnRequest))
            {
                if (Keywords.Ignore.EqualsIgnoreCase(variableName))
                {
                    pathIx++;
                    continue;                       
                }
 
                throw new ArgumentException("Could not find property "
                                            + variableName + " on " + RequestType.GetOperationName());
            }

            var value = requestComponents.Length > pathIx ? requestComponents[pathIx] : null; //wildcard has arg mismatch
            if (value != null && this.isWildcard[i])
            {
                if (i == this.TotalComponentsCount - 1)
                {
                    // Wildcard at end of path definition consumes all the rest
                    var sb = StringBuilderCache.Allocate();
                    sb.Append(value);
                    for (var j = pathIx + 1; j < requestComponents.Length; j++)
                    {
                        sb.Append(PathSeparatorChar + requestComponents[j]);
                    }
                    value = StringBuilderCache.ReturnAndFree(sb);
                }
                else
                {
                    // Wildcard in middle of path definition consumes up until it
                    // hits a match for the next element in the definition (which must be a literal)
                    // It may consume 0 or more path parts
                    var stopLiteral = i == this.TotalComponentsCount - 1 ? null : this.literalsToMatch[i + 1];
                    if (!string.Equals(requestComponents[pathIx], stopLiteral, StringComparison.OrdinalIgnoreCase))
                    {
                        var sb = StringBuilderCache.Allocate();
                        sb.Append(value);
                        pathIx++;
                        while (!string.Equals(requestComponents[pathIx], stopLiteral, StringComparison.OrdinalIgnoreCase))
                        {
                            sb.Append(PathSeparatorChar + requestComponents[pathIx++]);
                        }
                        value = StringBuilderCache.ReturnAndFree(sb);
                    }
                    else
                    {
                        value = null;
                    }
                }
            }
            else
            {
                // Variable consumes single path item
                pathIx++;
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

        return this.typeDeserializer.PopulateFromMap(fromInstance, requestKeyValuesMap, 
            HostContext.Config.IgnoreWarningsOnAllProperties 
                ? null 
                : HostContext.Config.IgnoreWarningsOnAutoQueryApis && fromInstance is IQuery
                    ? null
                    : HostContext.Config.IgnoreWarningsOnPropertyNames);
    }

    public bool IsVariable(string name)
    {
        return name != null && variablesNames.Any(name.EqualsIgnoreCase);
    }

    public override int GetHashCode()
    {
        return UniqueMatchHashKey.GetHashCode();
    }
}
