using System.Collections.Generic;

namespace ServiceStack.Redis.Tests.Generic
{
    public interface IRedisClientTestsBase<T>
    {
        void AssertListsAreEqual(List<T> actualList, IList<T> expectedList);
        void AssertIsEqual(T actual, T expected);

        T ExistingValue { get; }
        T NonExistingValue { get; }
        List<T> CreateList();
        List<T> CreateList2();
        T CreateInstance(int i);
    }
}