namespace Xilium.CefGlue.Wrapper
{
    using System;
    using System.Collections.Generic;
    using System.Text;

    internal static class CefMessageRouter
    {
        // ID value reserved for internal use.
        public const int ReservedId = 0;

        // Appended to the JS function name for related IPC messages.
        public const string MessageSuffix = "Msg";

        // JS object member argument names for cefQuery.
        public const string MemberRequest = "request";
        public const string MemberOnSuccess = "onSuccess";
        public const string MemberOnFailure = "onFailure";
        public const string MemberPersistent = "persistent";

        // Default error information when a query is canceled.
        public const int CanceledErrorCode = -1;
        public const string CanceledErrorMessage = "The query has been canceled";


        public sealed class IdGeneratorInt32
        {
            private int _next_id;

            public int GetNextId()
            {
                var id = ++_next_id;
                if (id == CefMessageRouter.ReservedId)
                    id = ++_next_id;
                return id;
            }
        }

        public sealed class IdGeneratorInt64
        {
            private long _next_id;

            public long GetNextId()
            {
                var id = ++_next_id;
                if (id == CefMessageRouter.ReservedId)
                    id = ++_next_id;
                return id;
            }
        }
    }
}
