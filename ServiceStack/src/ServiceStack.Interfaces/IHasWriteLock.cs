namespace ServiceStack;

public interface IHasWriteLock
{
    object WriteLock { get; }
}
