using System;

namespace ServiceStack.Text.Tests.DynamicModels.DataModel
{
    public class DynamicType
    {
        public string Name { get; set; }
        public Type Type { get; set; }
        public object Value { get; set; }

        public object GetTypedValue()
        {
            var strValue = this.Value as string;
            if (strValue != null)
            {
                var unescapedValue = strValue.FromCsvField();
                return TypeSerializer.DeserializeFromString(unescapedValue, this.Type);
            }
            return Value;
        }
    }
}