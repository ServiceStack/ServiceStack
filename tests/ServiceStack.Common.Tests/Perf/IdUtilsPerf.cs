using System;
using System.Collections.Generic;
using NUnit.Framework;
using ServiceStack.Model;

namespace ServiceStack.Common.Tests.Perf
{
    [Ignore("Benchmarks for measuring Id access")]
    [TestFixture]
    public class IdUtilsPerf
        : PerfTestBase
    {
        public IdUtilsPerf()
        {
            this.MultipleIterations = new List<int> { 100000 };
        }

        public static object OldGetId<T>(T entity)
        {
            const string idField = "Id";

            var guidEntity = entity as IHasGuidId;
            if (guidEntity != null)
            {
                return guidEntity.Id;
            }

            var intEntity = entity as IHasIntId;
            if (intEntity != null)
            {
                return intEntity.Id;
            }

            var longEntity = entity as IHasLongId;
            if (longEntity != null)
            {
                return longEntity.Id;
            }

            var stringEntity = entity as IHasStringId;
            if (stringEntity != null)
            {
                return stringEntity.Id;
            }

            var propertyInfo = typeof(T).GetProperty(idField);
            if (propertyInfo != null)
            {
                return propertyInfo.GetGetMethod().Invoke(entity, new object[0]);
            }

            if (typeof(T).IsValueType() || typeof(T) == typeof(string))
            {
                return entity.GetHashCode();
            }

            throw new NotSupportedException("Cannot retrieve value of Id field, use IHasId<>");
        }

        private void CompareForInstance<T>(T obj)
        {
            CompareMultipleRuns(
                "OldGetId", () => OldGetId(obj),
                "obj.GetId()", () => obj.GetId()
            );
        }

        [Test]
        public void Compare_HasIntId()
        {
            CompareForInstance(new IdUtilsTests.HasIntId());
        }

        [Test]
        public void Compare_HasGenericIdInt()
        {
            CompareForInstance(new IdUtilsTests.HasGenericIdInt());
        }

        [Test]
        public void Compare_HasGenericIdString()
        {
            CompareForInstance(new IdUtilsTests.HasGenericIdString());
        }

        [Test]
        public void Compare_HasIdStringProperty()
        {
            CompareForInstance(new IdUtilsTests.HasIdStringProperty());
        }

        [Test]
        public void Compare_HasIdProperty()
        {
            CompareForInstance(new IdUtilsTests.HasIdProperty());
        }
    }
}