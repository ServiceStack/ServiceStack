using System;
using System.Reflection;
using ServiceStack.Common.Services.Service;

namespace ServiceStack.Common.Services.Support.Service
{
    public class AssemblyTypeServiceResolver 
    {
        public IXmlService Find(IXmlServiceRequest xmlServiceRequest)
        {
            foreach (var type in Assembly.GetEntryAssembly().GetTypes())
            {
                if (type.IsInstanceOfType(typeof(IXmlService)))
                {
                    return (IXmlService)Activator.CreateInstance(type);
                }
            }
            return null;
        }
    }
}