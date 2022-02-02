using System;
using System.Collections.Generic;
using NUnit.Framework;
using ServiceStack.DataAnnotations;

namespace ServiceStack.Common.Tests.Models{
	
	[Alias("ModelWComplexT")]
	public class ModelWithComplexTypes
	{
		public ModelWithComplexTypes Child
		{
			get;
			set;
		}
	
		public long Id
		{
			get;
			set;
		}
	
		public List<int> IntList
		{
			get;
			set;
		}
	
		public Dictionary<int, int> IntMap
		{
			get;
			set;
		}
	
		public List<string> StringList
		{
			get;
			set;
		}
	
		public Dictionary<string, string> StringMap
		{
			get;
			set;
		}
	
		public ModelWithComplexTypes()
		{
			this.StringList = new List<string>();
			this.IntList = new List<int>();
			this.StringMap = new Dictionary<string, string>();
			this.IntMap = new Dictionary<int, int>();
		}
	
		public static void AssertIsEqual(ModelWithComplexTypes actual, ModelWithComplexTypes expected)
		{
			Assert.That(actual.Id, Is.EqualTo(expected.Id));
			Assert.That(actual.StringList, Is.EquivalentTo(expected.StringList));
			Assert.That(actual.IntList, Is.EquivalentTo(expected.IntList));
			Assert.That(actual.StringMap, Is.EquivalentTo(expected.StringMap));
			Assert.That(actual.IntMap, Is.EquivalentTo(expected.IntMap));
			if (expected.Child == null)
			{
				Assert.That(actual.Child, Is.Null);
				return;
			}
			Assert.That(actual.Child, Is.Not.Null);
			ModelWithComplexTypes.AssertIsEqual(actual.Child, expected.Child);
		}
	
		public static ModelWithComplexTypes Create(int id)
		{
			ModelWithComplexTypes modelWithComplexType1 = new ModelWithComplexTypes();
			modelWithComplexType1.Id = (long)id;
			modelWithComplexType1.StringList.Add(string.Concat("val", id, 1));
			modelWithComplexType1.StringList.Add(string.Concat("val", id, 2));
			modelWithComplexType1.StringList.Add(string.Concat("val", id, 3));
			modelWithComplexType1.IntList.Add(id + 1);
			modelWithComplexType1.IntList.Add(id + 2);
			modelWithComplexType1.IntList.Add(id + 3);
			modelWithComplexType1.StringMap.Add(string.Concat("key", id, 1), string.Concat("val", id, 1));
			modelWithComplexType1.StringMap.Add(string.Concat("key", id, 2), string.Concat("val", id, 2));
			modelWithComplexType1.StringMap.Add(string.Concat("key", id, 3), string.Concat("val", id, 3));
			modelWithComplexType1.IntMap.Add(id + 1, id + 2);
			modelWithComplexType1.IntMap.Add(id + 3, id + 4);
			modelWithComplexType1.IntMap.Add(id + 5, id + 6);
			modelWithComplexType1.Child = new ModelWithComplexTypes(){
				Id= (long)(id*2),
			};
			
			return modelWithComplexType1;
		}
	
		public static ModelWithComplexTypes CreateConstant(int i)
		{
			return ModelWithComplexTypes.Create(i);
		}
	}
}