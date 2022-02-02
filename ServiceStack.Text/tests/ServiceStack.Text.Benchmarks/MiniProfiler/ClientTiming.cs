using System;
using System.Runtime.Serialization;

namespace StackExchange.Profiling
{
    /// <summary>
    /// A client timing probe
    /// </summary>
    [DataContract]
    public class ClientTiming
    {
        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        [DataMember(Order = 1)]
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the start.
        /// </summary>
        [DataMember(Order = 2)]
        public decimal Start { get; set; }

        /// <summary>
        /// Gets or sets the duration.
        /// </summary>
        [DataMember(Order = 3)]
        public decimal Duration { get; set; }

        /// <summary>
        /// Unique Identifier used for sql storage. 
        /// </summary>
        /// <remarks>Not set unless storing in Sql</remarks>
        public Guid Id { get; set; }

        /// <summary>
        /// Used for sql storage
        /// </summary>
        /// <remarks>Not set unless storing in Sql</remarks>
        public Guid MiniProfilerId { get; set; }
    }
}
