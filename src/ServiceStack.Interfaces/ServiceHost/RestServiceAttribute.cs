using System;

namespace ServiceStack.ServiceHost
{
	/// <summary>
	///		Used to decorate Request DTO's to associate a RESTful request 
	///		path mapping with a service.  Multiple attributes can be applied to 
	///		each request DTO, to map multiple paths to the service.
	/// </summary>
    [Obsolete("Use [Route] instead of [RestService].")]
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = true)]
    public class RestServiceAttribute
        : RouteAttribute
    {
        /// <summary>
        /// 	<para>Initializes an instance of the <see cref="RestServiceAttribute"/> class.</para>
        /// </summary>
        /// <param name="path">
        /// 	<para>The path template to map to the request.  See 
        ///		<see cref="Path">RestServiceAttribute.Path</see>
        ///		for details on the correct format.</para>
        /// </param>
        public RestServiceAttribute(string path)
            : base(path, null)
        {
        }

        /// <summary>
        /// 	<para>Initializes an instance of the <see cref="RestServiceAttribute"/> class.</para>
        /// </summary>
        /// <param name="path">
        /// 	<para>The path template to map to the request.  See 
        ///		<see cref="Path">RestServiceAttribute.Path</see>
        ///		for details on the correct format.</para>
        /// </param>
        /// <param name="verbs">A comma-delimited list of HTTP verbs supported by the 
        ///		service.  If unspecified, all verbs are assumed to be supported.</param>
        public RestServiceAttribute(string path, string verbs)
            : base(path, verbs)
        {
        }
    }
}