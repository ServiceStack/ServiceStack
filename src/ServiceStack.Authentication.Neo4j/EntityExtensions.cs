using Neo4j.Driver.V1;

namespace ServiceStack.Authentication.Neo4j
{
    internal static class EntityExtensions
    {
        public static T Map<T>(this IEntity cypherValue)
        {
            return cypherValue.Properties.FromObjectDictionary<T>();
        }
    }
}
