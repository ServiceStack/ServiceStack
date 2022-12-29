using System;
using System.Collections.ObjectModel;
using System.Linq;

namespace ServiceStack.Text.Tests.DynamicModels.DataModel
{
#if !NETCORE
    [Serializable]
#endif
    public class CustomCollection : Collection<CustomCollectionItem>
    {
        public int FindItemIndex(string name)
        {
            return IndexOf((from item in this
                            where item.Name == name
                            select item).FirstOrDefault());
        }

        public void RemoveAll(string name)
        {
            while (true)
            {
                var idx = FindItemIndex(name);
                if (idx < 0)
                    break;

                RemoveAt(idx);
            }
        }

        public Uri AddressUri
        {
            get
            {
                var idx = FindItemIndex("AddressUri");
                //Cater for value containing a real value or a serialized string value
                //Using 'FromCsvField()' because 'Value' may have escaped chars
                return idx < 0 ? null :
                    (
                        this[idx].Value is string
                        ? new Uri(((string)this[idx].Value).FromCsvField())
                        : this[idx].Value as Uri
                    );
            }
            set
            {
                RemoveAll("AddressUri");
                Add(new CustomCollectionItem("AddressUri", value));
            }
        }

        public Type SomeType
        {
            get
            {
                var idx = FindItemIndex("SomeType");
                //Cater for value containing a real value or a serialized string value
                //Using 'FromCsvField()' because 'Value' may have escaped chars
                return idx < 0 ? null :
                    (
                        this[idx].Value is string
                        ? AssemblyUtils.FindType(((string)this[idx].Value).FromCsvField())
                        : this[idx].Value as Type
                    );
            }
            set
            {
                RemoveAll("SomeType");
                Add(new CustomCollectionItem("SomeType", value));
            }
        }

        public int IntValue
        {
            get
            {
                var idx = FindItemIndex("IntValue");
                //Cater for value containing a real value or a serialized string value
                return idx < 0 ? -1 :
                    (
                        this[idx].Value is string
                        ? int.Parse((string)this[idx].Value)
                        : (int)this[idx].Value
                    );
            }
            set
            {
                RemoveAll("IntValue");
                Add(new CustomCollectionItem("IntValue", value));
            }
        }
    }
}