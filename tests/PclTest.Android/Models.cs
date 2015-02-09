using System.Runtime.Serialization;
using ServiceStack;

namespace PclTest.Android
{
    [DataContract]
    public class BaseRequest
    {
        /// <summary>
        /// The api key is required on all requests else an exception will be thrown
        /// </summary>
        [DataMember]
        public string ApiKey { get; set; }

        /// <summary>
        /// The version number of the request object
        /// </summary>
        [DataMember]
        public double Version { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="BaseRequest" /> class.
        /// </summary>
        public BaseRequest()
        {
            //Default everything to 1.0 and then it can be overridden in the base
            //class as and when required
            Version = 1.0;
        }
    }
    [DataContract]
    public class BaseResponse : BaseEntity, IHasResponseStatus
    {

        /// <summary>
        /// Initializes a new instance of the <see cref="BaseResponse"/> class.
        /// </summary>
        public BaseResponse()
        {

        }

        /// <summary>
        /// The number of properties currently onview within 
        /// </summary>
        [DataMember]
        public int OnViewCount { get; set; }

        /// <summary>
        /// The response status object populated by service stack on the return of the application
        /// </summary>
        [DataMember]
        public ResponseStatus ResponseStatus { get; set; }
    }

    [DataContract]
    public class BaseEntity
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="BaseEntity"/> class.
        /// </summary>
        public BaseEntity()
        {
            //Default all entities to 1.0 and then we can override in the child class as we move forward
            Version = 1.0;
        }

        /// <summary>
        /// The version number of the entity
        /// </summary>
        [DataMember]
        public double Version { get; set; }
    }

    [Route("/account/forgotpassword", "POST")]
    [DataContract]
    public class ForgotPassword : BaseRequest, IReturn<ForgotPasswordResponse>
    {
        /// <summary>
        /// The email address the account is registered to
        /// </summary>
        [DataMember]
        public string EmailAddress { get; set; }
    }

    /// <summary>
    /// The response to request a password reset based on the users email address
    /// </summary>
    [DataContract]
    public class ForgotPasswordResponse : BaseResponse
    {

    }

}