using System;

namespace ServiceStack.Authentication.RavenDb
{
    /// <summary>
    /// Converts string Id to integer Id used by UserAuth.
    /// Supports up to 21,474,836 ids and 26 servers in the cluster (A-Z)
    /// </summary>
    public static class RavenIdConverter
    {
        const int ClusterTagOffset = 100;
        const int ASCIIOffset = 65;

        /// <summary>
        /// Attempts to convert a RavenDB string document ID to an integer ID using base-100 encoding:
        /// <code>id = (sequence * 100) + (clusterTag - 'A')</code>
        /// <para>
        /// In RavenDB clusters, document IDs follow the composite format <c>{Collection}/{Sequence}-{ClusterTag}</c>
        /// (e.g. <c>RavenUserAuths/1-A</c>, <c>RavenUserAuths/1-B</c>, <c>users/25-Z</c>).
        /// </para>
        /// <example>
        /// Examples:
        /// <list type="bullet">
        ///   <item><description><c>"RavenUserAuths/1-A"</c> -&gt; <c>(1 * 100) + 0 = 100</c></description></item>
        ///   <item><description><c>"RavenUserAuths/1-B"</c> -&gt; <c>(1 * 100) + 1 = 101</c></description></item>
        ///   <item><description><c>"users/25-Z"</c> -&gt; <c>(25 * 100) + 25 = 2525</c></description></item>
        ///   <item><description><c>"100"</c> (direct integer) -&gt; <c>100</c></description></item>
        ///   <item><description><c>"users/5"</c> (no cluster tag) -&gt; <c>5 * 100 = 500</c></description></item>
        /// </list>
        /// </example>
        /// </summary>
        /// <param name="ravenId">The RavenDB document ID or integer string.</param>
        /// <param name="id">When this method returns, contains the parsed integer ID if successful; otherwise, 0.</param>
        /// <returns><c>true</c> if the ID was successfully parsed; otherwise, <c>false</c>.</returns>
        public static bool TryToInt(string ravenId, out int id)
        {
            id = 0;
            if (string.IsNullOrEmpty(ravenId))
                return false;

            if (int.TryParse(ravenId, out id))
                return true;

            var lastSlash = ravenId.LastIndexOf('/');
            string compositeId = lastSlash >= 0 ? ravenId.Substring(lastSlash + 1) : ravenId;
            var idParts = compositeId.Split('-');
            if (idParts.Length >= 2 && int.TryParse(idParts[0], out var seq))
            {
                id = seq * ClusterTagOffset + ClusterTagToInt(idParts[1]);
                return true;
            }
            if (idParts.Length == 1 && int.TryParse(idParts[0], out var singleSeq))
            {
                id = singleSeq * ClusterTagOffset;
                return true;
            }

            return false;
        }

        public static int ToInt(string ravenId)
        {
            if (TryToInt(ravenId, out var id))
                return id;

            throw new FormatException($"Invalid RavenDB ID format: '{ravenId}'");
        }

        static int ClusterTagToInt(string tag)
        {
            if (string.IsNullOrEmpty(tag))
                return 0;

            char c = char.ToUpperInvariant(tag[0]);
            return (c >= 'A' && c <= 'Z') ? (c - ASCIIOffset) : 0;
        }

        public static string ToString(string prefix, int id)
        {
            if (id < 0) id = 0;
            int sequenceValue = id / ClusterTagOffset;
            int ascii = id % ClusterTagOffset;
            int clampedAscii = ascii < 0 ? 0 : (ascii > 25 ? 25 : ascii);
            char clusterTag = Convert.ToChar(clampedAscii + ASCIIOffset);
            prefix = string.IsNullOrEmpty(prefix) ? "" : (prefix.EndsWith("/") ? prefix.TrimEnd('/') : prefix);
            return $"{prefix}/{sequenceValue}-{clusterTag}";
        }
    }
}