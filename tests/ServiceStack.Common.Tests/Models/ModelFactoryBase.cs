using System.Collections.Generic;
using NUnit.Framework;

namespace ServiceStack.Common.Tests.Models
{
    public abstract class ModelFactoryBase<T>
        : IModelFactory<T>
    {
        #region Implementation of IModelFactory<T>

        public void AssertListsAreEqual(List<T> actualList, IList<T> expectedList)
        {
            Assert.That(actualList, Has.Count.EqualTo(expectedList.Count));
            var i = 0;

            actualList.ForEach(x =>
                AssertIsEqual(x, expectedList[i++]));
        }

        public abstract T CreateInstance(int i);

        public abstract void AssertIsEqual(T actual, T expected);

        public T ExistingValue
        {
            get
            {
                return CreateInstance(4);
            }
        }

        public T NonExistingValue
        {
            get
            {
                return CreateInstance(5);
            }
        }

        public List<T> CreateList()
        {
            return new List<T>
            {
                CreateInstance(1),
                CreateInstance(2),
                CreateInstance(3),
                CreateInstance(4),
            };
        }
        public List<T> CreateList2()
        {
            return new List<T>
            {
                CreateInstance(5),
                CreateInstance(6),
                CreateInstance(7),
            };
        }

        #endregion
    }
}