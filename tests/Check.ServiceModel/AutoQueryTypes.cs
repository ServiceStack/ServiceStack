using ServiceStack;

namespace Check.ServiceModel
{
    public class OnlyDefinedInGenericType
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }

    public class QueryPocoBase : QueryBase<OnlyDefinedInGenericType>
    {
        public int Id { get; set; }
    }

    public class OnlyDefinedInGenericTypeFrom
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }

    public class OnlyDefinedInGenericTypeInto
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }

    public class QueryPocoIntoBase : QueryBase<OnlyDefinedInGenericTypeFrom, OnlyDefinedInGenericTypeInto>
    {
        public int Id { get; set; }
    }
}