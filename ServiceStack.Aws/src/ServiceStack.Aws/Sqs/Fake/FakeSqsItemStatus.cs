namespace ServiceStack.Aws.Sqs.Fake
{
    public enum FakeSqsItemStatus
    {
        Unknown = 0,
        Queued = 1,
        InFlight = 2,
        Deleted = 3
    }
}