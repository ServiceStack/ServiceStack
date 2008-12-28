using System.Collections.Generic;

namespace ServiceStack.WebHost.Endpoints.Support.Endpoints
{
    internal class OperationVerbs
    {
        public static IEnumerable<string> ReplyOperationVerbs
        {
            get
            {
                return new[] { "get", "search" };
            }
        }

        public static IEnumerable<string> OneWayOperationVerbs
        {
            get
            {
                return new[] { "store", "add", "assign", "delete", "remove", "notify" };
            }
        }
    }
}