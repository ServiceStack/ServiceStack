using System.Collections.Generic;
using System.Runtime.Serialization;

namespace ServiceStack
{
    //Interfaces and DTO's used in AutoQuery
    public interface IQuery
    {
        int? Skip { get; set; }
        int? Take { get; set; }
    }

    public interface IQuery<From> : IQuery { }
    public interface IQuery<From,Into> : IQuery { }

    public interface IJoin { }
    public interface IJoin<Source, Join1> : IJoin { }
    public interface IJoin<Source, Join1, Join2> : IJoin { }
    public interface IJoin<Source, Join1, Join2, Join3> : IJoin { }
    public interface IJoin<Source, Join1, Join2, Join3, Join4> : IJoin { }

    public interface ILeftJoin<Source, Join1> : IJoin { }
    public interface ILeftJoin<Source, Join1, Join2> : IJoin { }
    public interface ILeftJoin<Source, Join1, Join2, Join3> : IJoin { }
    public interface ILeftJoin<Source, Join1, Join2, Join3, Join4> : IJoin { }

    public abstract class QueryBase : IQuery
    {
        [DataMember(Order = 1)]
        public int? Skip { get; set; }

        [DataMember(Order = 2)]
        public int? Take { get; set; }
    }

    public abstract class QueryBase<T> : QueryBase, IQuery<T>, IReturn<QueryResponse<T>> { }

    public abstract class QueryBase<From, Into> : QueryBase, IQuery<From, Into>, IReturn<QueryResponse<Into>> { }

    [DataContract]
    public class QueryResponse<T> : IHasResponseStatus, IMeta
    {
        [DataMember(Order = 1)]
        public int Offset { get; set; }

        [DataMember(Order = 2)]
        public int Total { get; set; }

        [DataMember(Order = 3)]
        public List<T> Results { get; set; }

        [DataMember(Order = 4)]
        public Dictionary<string, string> Meta { get; set; }

        [DataMember(Order = 5)]
        public ResponseStatus ResponseStatus { get; set; }
    }
}