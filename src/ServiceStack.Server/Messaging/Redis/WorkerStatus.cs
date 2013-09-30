//Copyright (c) Service Stack LLC. All Rights Reserved.
//License: https://raw.github.com/ServiceStack/ServiceStack/master/license.txt

namespace ServiceStack.Messaging.Redis
{
    public static class WorkerStatus
    {
        public const int Disposed = -1;
        public const int Stopped = 0;
        public const int Stopping = 1;
        public const int Starting = 2;
        public const int Started = 3;

        //Control Commands
        public const string StopCommand = "STOP";
        public const string ResetCommand = "RESET";

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
}