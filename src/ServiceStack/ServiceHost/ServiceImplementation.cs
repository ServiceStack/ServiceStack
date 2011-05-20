using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ServiceStack.ServiceHost
{
    public struct ServiceImplementation
    {
        /// <summary>
        /// The type of the request dto
        /// </summary>
        public Type RequestType { get; set; }

        /// <summary>
        /// The type that implements IService{RequestType}
        /// </summary>
        public Type ServiceType { get; set; }
    }
}
