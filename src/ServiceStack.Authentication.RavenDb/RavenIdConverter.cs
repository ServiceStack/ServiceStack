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

        public static int ToInt(string ravenId)
        {
            string compositeId = ravenId.Split('/')[1];
            var idParts = compositeId.Split('-');
            return Convert.ToInt32(idParts[0]) * ClusterTagOffset + ClusterTagToInt(idParts[1]);
        }

        static int ClusterTagToInt(string tag)
        {
            return tag[0] - ASCIIOffset;
        }

        public static string ToString(string prefix, int id)
        {
            int sequenceValue = id / ClusterTagOffset;
            int ascii = id % ClusterTagOffset;
            char clusterTag = Convert.ToChar(ascii + ASCIIOffset);
            return $"{prefix}/{sequenceValue}-{clusterTag}";
        }
    }
}