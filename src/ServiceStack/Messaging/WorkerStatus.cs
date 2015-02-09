//Copyright (c) Service Stack LLC. All Rights Reserved.
//License: https://raw.github.com/ServiceStack/ServiceStack/master/license.txt

namespace ServiceStack.Messaging
{
    public static class WorkerStatus
    {
        public const int Disposed = -1;
        public const int Stopped = 0;
        public const int Stopping = 1;
        public const int Starting = 2;
        public const int Started = 3;

        public static string ToString(int workerStatus)
        {
            switch (workerStatus)
            {
                case Disposed:
                    return "Disposed";
                case Stopped:
                    return "Stopped";
                case Stopping:
                    return "Stopping";
                case Starting:
                    return "Starting";
                case Started:
                    return "Started";
            }
            return "Unknown";
        }
    }

    public static class WorkerOperation
    {
        public const string ControlCommand = "CTRL";

        public const int NoOp = 0;
        public const int Stop = 1;
        public const int Reset = 2;
        public const int Restart = 3;
    }
}