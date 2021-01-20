using System;

namespace ServiceStack.Authentication.RavenDb
{
    public static class RavenIdConverter
    {
        public static int ToInt(string ravenId)
        {
            string compositeId = ravenId.Split('/')[1];
            var idParts = compositeId.Split('-');
            return Convert.ToInt32(idParts[0]) * 100 + ClusterTagToInt(idParts[1]);
        }

            static int ClusterTagToInt(string tag)
            {
                return tag[0] - 65;
            }

        public static string ToString(string prefix, int id)
        {
            int sequenceValue = id / 100;
            int ascii = id % 100;
            char clusterTag = Convert.ToChar(ascii + 65);
            return $"{prefix}/{sequenceValue}-{clusterTag}";
        }
    }
}