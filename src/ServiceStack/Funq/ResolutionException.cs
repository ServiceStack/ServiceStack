using System;
using System.Globalization;

namespace Funq
{
    /// <summary>
    /// Exception thrown by the container when a service cannot be resolved.
    /// </summary>
#if !(SL5 || NETSTANDARD1_6)
    [Serializable]
#endif
    public class ResolutionException : Exception
    {
        /// <summary>
        /// Initializes the exception with the service that could not be resolved.
        /// </summary>
        public ResolutionException(Type missingServiceType)
            : base(String.Format(
                CultureInfo.CurrentCulture,
                ServiceStack.Properties.Resources.ResolutionException_MissingType,
                missingServiceType.FullName))
        { }

        /// <summary>
        /// Initializes the exception with the service (and its name) that could not be resolved.
        /// </summary>
        public ResolutionException(Type missingServiceType, string missingServiceName)
            : base(String.Format(
                CultureInfo.CurrentCulture,
                ServiceStack.Properties.Resources.ResolutionException_MissingNamedType,
                missingServiceType.FullName, missingServiceName))
        { }

        /// <summary>
        /// Initializes the exception with an arbitrary message.
        /// </summary>
        public ResolutionException(string message) : base(message) { }
    }
}
