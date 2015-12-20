namespace ServiceStack
{
    /// <summary>
    /// Provide unique, incrementing sequences. Used in PocoDynamo.
    /// </summary>
    public interface ISequenceSource : IRequiresSchema
    {
        long Increment(string key, int amount = 1);

        void Reset(string key, int startingAt = 0);
    }
}