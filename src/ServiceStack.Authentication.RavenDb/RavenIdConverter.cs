using System;

namespace ServiceStack.Authentication.RavenDb
{
    public static class RavenIdConverter
    {
        public const string RavenUserAuthsIdPrefix = "RavenUserAuths";
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