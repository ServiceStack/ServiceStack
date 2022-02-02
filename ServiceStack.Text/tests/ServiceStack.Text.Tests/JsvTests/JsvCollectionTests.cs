using System.Collections.Generic;
using NUnit.Framework;

namespace ServiceStack.Text.Tests.JsvTests
{
    public class Item
    {
        public Item()
        {
        }

        public Item(string key, string value)
        {
            this.Key = key;
            this.Value = value;
        }

        public string Key
        {
            get;
            set;
        }

        public string Value
        {
            get;
            set;
        }
    }

    public class ItemCollection : Dictionary<string, Item> { }

    [TestFixture]
    public class JsvCollectionTests
    {
        [Test]
        public void Can_serialize_empty_collections()
        {
            var collection = new ItemCollection();

            // TB: these work fine
            var item1 = new Item("key1", "somevalue");
            var item2 = new Item("key2", null);

            // TB: this is not deserialized correctly => can not be added to the dictionary, because key is null instead of string.Empty (a valid distionary key)
            var exceptionProducingItem = new Item(string.Empty, "somevalue");

            collection.Add(item1.Key, item1);
            collection.Add(item2.Key, item2);
            collection.Add(exceptionProducingItem.Key, exceptionProducingItem);

            string jsv = TypeSerializer.SerializeToString(collection);

            jsv.Print();


            collection = TypeSerializer.DeserializeFromString<ItemCollection>(jsv);

            collection.PrintDump();
        }
    }
}