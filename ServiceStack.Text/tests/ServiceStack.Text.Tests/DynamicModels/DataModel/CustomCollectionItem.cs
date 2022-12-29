using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ServiceStack.Text.Tests.DynamicModels.DataModel
{
#if !NETCORE
    [Serializable]
#endif
    public class CustomCollectionItem
    {
        public CustomCollectionItem()
        { }

        public CustomCollectionItem(string name, object value)
        {
            Name = name;
            Value = value;
        }

        public string Name { get; set; }
        public object Value { get; set; }

        public override string ToString()
        {
            return string.Concat("Name = '", Name, "' Value = '", Value, "'");
        }
    }
}