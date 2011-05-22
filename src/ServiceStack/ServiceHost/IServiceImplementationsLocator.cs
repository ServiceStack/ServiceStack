using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace ServiceStack.ServiceHost
{
    public interface IServiceImplementationsLocator
    {
        /// <summary>
        /// Locates all of the services defined within the provided assemblies.
        /// </summary>
        /// <param name="assembliesWithServices"></param>
        /// <returns></returns>
        IEnumerable<ServiceImplementation> GetServiceImplementations(Assembly[] assembliesWithServices);
    }
}
