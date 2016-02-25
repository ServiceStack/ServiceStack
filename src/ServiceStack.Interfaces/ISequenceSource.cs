namespace ServiceStack
{
    /// <summary>
    /// Provide unique, incrementing sequences. Used in PocoDynamo.
    /// </summary>
    public interface ISequenceSource : IRequiresSchema
    {
        long Increment(string key, long amount = 1);

        void Reset(string key, long startingAt = 0);
    }
}