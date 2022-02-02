using System.Collections.Generic;

namespace ServiceStack.Common.Tests.Models
{
    public interface IModelFactory<T>
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