using System;
using System.Runtime.Serialization;
using ServiceStack.DataAnnotations;

//using System.Web.Script.Serialization;

namespace ServiceStack.MiniProfiler
{
    /// <summary>
    /// Information about a DbParameter used in the sql statement profiled by SqlTiming.
    /// </summary>
    [Exclude(Feature.Soap)]
    [DataContract] 
    public class SqlTimingParameter
    {
        /// <summary>
        /// Which SqlTiming this Parameter was executed with.
        /// </summary>
		//[ScriptIgnore]
        public Guid ParentSqlTimingId { get; set; }

        /// <summary>
        /// Parameter name, e.g. "@routeName"
        /// </summary>
        [DataMember(Order = 1)]
        public string Name { get; set; }

        /// <summary>
        /// The value submitted to the database.
        /// </summary>
        [DataMember(Order = 2)]
        public string Value { get; set; }

        /// <summary>
        /// System.Data.DbType, e.g. "String", "Bit"
        /// </summary>
        [DataMember(Order = 3)]
        public string DbType { get; set; }

        /// <summary>
        /// How large the type is, e.g. for string, size could be 4000
        /// </summary>
        [DataMember(Order = 4)]
        public int Size { get; set; }

        /// <summary>
        /// Returns true if this has the same parent <see cref="SqlTiming.Id"/>, <see cref="Name"/> and <see cref="Value"/> as <paramref name="obj"/>.
        /// </summary>
        public override bool Equals(object obj)
        {
            var other = obj as SqlTimingParameter;
            return other != null && ParentSqlTimingId.Equals(other.ParentSqlTimingId) && string.Equals(Name, other.Name) && string.Equals(Value, other.Value);
        }

        /// <summary>
        /// Returns the XOR of certain properties.
        /// </summary>
        public override int GetHashCode()
        {
            return ParentSqlTimingId.GetHashCode() ^ Name.GetHashCode() ^ (Value != null ? Value.GetHashCode() : 0);
        }
    }
}
