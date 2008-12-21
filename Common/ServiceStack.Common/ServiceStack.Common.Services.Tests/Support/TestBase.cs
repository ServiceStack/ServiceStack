using System;
using System.Collections;
using System.Reflection;
using NUnit.Framework;

namespace ServiceStack.Common.Services.Tests.Support
{
    public class TestBase
    {

        [SetUp]
        public virtual void SetUp()
        {
        }

        [TearDown]
        public virtual void TearDown()
        {			
        }

        public void AssertList(IList resultList, params object[] expectedPropertyObjects)
        {			
            Assert.AreEqual(expectedPropertyObjects.Length, resultList.Count, "List contains incorrect number of items");

            foreach (object o in expectedPropertyObjects)
            {
                Assert.IsTrue(resultList.Contains(o), "List is missing object :"+o);	
            }
        }

        public void AssertList(IList resultList, string valuePropertyPath, params object[] expectedPropertyValues)
        {			
            Assert.AreEqual(expectedPropertyValues.Length, resultList.Count, "List contains incorrect number of items");
            AssertContains(resultList, valuePropertyPath, expectedPropertyValues);
        }

        public void AssertContains(IList resultList, string valuePropertyPath, params object[] expectedPropertyValues)
        {
            ArrayList foundObjects = new ArrayList();
            foreach(object val in expectedPropertyValues)
            {
                bool found = false;
                foreach(object obj in resultList)
                {
                    if(foundObjects.Contains(obj))
                    {
                        //aready matched to something
                        continue;
                    }

                    object actValue = ExtractPropertyValue(obj, valuePropertyPath);

                    if(actValue.GetType() != val.GetType())
                    {
                        Assert.Fail(string.Format("Expected type {0} does not match actual type of {1}", val.GetType().Name, actValue.GetType().Name) );
                    }

                    //dont use == as this fails with decimal
                    if (actValue.Equals(val))
                    {
                        found = true;
                        //so we dont find it again
                        foundObjects.Add(obj);
                        break;
                    }
                }
                Assert.IsTrue(found, "Value " + val + " not found in list");
            }
        }

        public void AssertListOrderAscending(IList resultList, string valuePropertyPath)
        {
            AssertListOrder(resultList, valuePropertyPath, true);
        }

        public void AssertListOrderDescending(IList resultList, string valuePropertyPath)
        {
            AssertListOrder(resultList, valuePropertyPath, false);
        }

        private void AssertListOrder(IList resultList, string valuePropertyPath, bool ascendingOrder)
        {
            object previousValue;

            Assert.IsTrue(resultList.Count >= 2, "HEY!! If you're going to assert order you need have at least 2 entries!!");
            previousValue = ExtractPropertyValue(resultList[0], valuePropertyPath);

            for(int i = 1; i < resultList.Count; i++)
            {
                var comparer = (IComparable) ExtractPropertyValue(resultList[i], valuePropertyPath);
				
                int direction;
                if(comparer is string)
                {
                    //Compare strings as case insensitive
                    direction = string.Compare((string) comparer, (string) previousValue, true);
                }
                else
                {
                    // use default comparer on object
                    direction = comparer.CompareTo(previousValue);				
                }

                if (ascendingOrder)
                {
                    Assert.IsTrue(direction >= 0, string.Format("{0} not >= {1}", comparer, previousValue));
                }
                else
                {
                    Assert.IsTrue(direction <= 0, string.Format("{0} not <= {1}", comparer, previousValue));
                }

                previousValue = comparer;
            }
        }

        public void AssertArray(Array resultArray, params object[] expectedPropertyValues)
        {
            Assert.AreEqual(expectedPropertyValues.Length, resultArray.Length, "List contains incorrect number of items");
            AssertArrayContains(resultArray, expectedPropertyValues);
        }

        public void AssertArrayContains(Array resultArray, params object[] expectedPropertyValues)
        {
            foreach(object val in expectedPropertyValues)
            {
                var found = false;
                foreach(object obj in resultArray)
                {
                    if (obj.Equals(val))//dont use == as this fails with decimal
                    {
                        found = true;
                        break;
                    }
                }
                Assert.IsTrue(found, "Value " + val + " not found in list");
            }
        }

        public void AssertHTMLContains(string html, params string[] subStrings)
        {
			
            var replHtml = html.Replace("\r\n", "");
            replHtml = replHtml.Replace("&nbsp;", " ");
            replHtml = replHtml.Replace("=", "");
            foreach(string subString in subStrings)
            {
                Assert.IsTrue(replHtml.IndexOf(subString) != -1, String.Format("HTML didn't contain substring \"{0}\"", subString));
            }
        }

        private static object ExtractPropertyValue(Object rootObject, string propertyPath)
        {
            var obj = rootObject;
            var props = propertyPath.Split('.');

            foreach (string propertyName in props)
            {
                var propInfo = obj.GetType().GetProperty(propertyName);
                if (propInfo == null)
                {
                    throw new ArgumentException(string.Format("Property path '{0}' invalid for object of type '{1}'"
                                                              , propertyPath, rootObject.GetType().Name));
                }
                obj = propInfo.GetValue(obj, null);
            }

            return obj;
        }
    }
}