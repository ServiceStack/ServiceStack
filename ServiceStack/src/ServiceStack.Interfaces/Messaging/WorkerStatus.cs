namespace ServiceStack.Messaging;

public static class WorkerStatus
{
    public const int Disposed = -1;
    public const int Stopped = 0;
    public const int Stopping = 1;
    public const int Starting = 2;
    public const int Started = 3;

    public static string ToString(int workerStatus) => workerStatus switch
    {
        Disposed => "Disposed",
        Stopped => "Stopped",
        Stopping => "Stopping",
        Starting => "Starting",
        Started => "Started",
        _ => "Unknown",
    };
}