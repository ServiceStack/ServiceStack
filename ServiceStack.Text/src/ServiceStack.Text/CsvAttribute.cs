using System;

namespace ServiceStack.Text
{
    public enum CsvBehavior
    {
        FirstEnumerable
    }

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, Inherited = false, AllowMultiple = false)]
    public class CsvAttribute : Attribute
    {
        public CsvBehavior CsvBehavior { get; set; }
        public CsvAttribute(CsvBehavior csvBehavior)
        {
            this.CsvBehavior = csvBehavior;
        }
    }
}