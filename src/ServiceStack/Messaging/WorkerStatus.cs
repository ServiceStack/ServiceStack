//Copyright (c) ServiceStack, Inc. All Rights Reserved.
//License: https://raw.github.com/ServiceStack/ServiceStack/master/license.txt

namespace ServiceStack.Messaging
{
    public static class WorkerOperation
    {
        public const string ControlCommand = "CTRL";

        public const int NoOp = 0;
        public const int Stop = 1;
        public const int Reset = 2;
        public const int Restart = 3;
    }
}