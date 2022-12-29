using System;
using System.Collections.Generic;

namespace ServiceStack.Text.Tests.DynamicModels
{
    public class ModelWithComplexTypes
    {
        public class NestedType
        {
            public int IntValue { get; set; }
        }

        [Flags]
        public enum MyEnum
        {
            Value = 12
        }

        public IList<string> ListValue { get; set; }
        public IDictionary<string, string> DictionaryValue { get; set; }
        public string[] ArrayValue { get; set; }
        public byte[] ByteArrayValue { get; set; }
        public MyEnum? EnumValue { get; set; }
        public NestedType NestedTypeValue { get; set; }

        public static ModelWithComplexTypes Create(int i)
        {
            return new ModelWithComplexTypes
            {
                DictionaryValue = new Dictionary<string, string> {{"a", i.ToString()}},
                ListValue = new List<string> {i.ToString()},
                ArrayValue = new string[]{},
                EnumValue = MyEnum.Value,
                ByteArrayValue = new byte[]{(byte)i},
            };
        }
    }
}