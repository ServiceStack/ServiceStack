using System;
using ServiceStack.Common.Web;
using ServiceStack.ServiceHost;

namespace ServiceStack.ServiceInterface
{
    /// <summary>
    ///		Base class for services that support HTTP verbs.
    /// </summary>
    /// <typeparam name="TRequest">The request class that the descendent class
    ///	is responsible for processing.</typeparam>
    [Obsolete("Use the New API (ServiceStack.ServiceInterface.Service) for future services. See: https://github.com/ServiceStack/ServiceStack/wiki/New-Api")]
    public abstract class RestServiceBase<TRequest>
        : ServiceBase<TRequest>,
        IRestGetService<TRequest>,
        IRestPutService<TRequest>,
        IRestPostService<TRequest>,
        IRestDeleteService<TRequest>,
        IRestPatchService<TRequest>
    {

        /// <summary>
        /// What gets run when the request is sent to AsyncOneWay endpoint.
        /// For a REST service the OnPost() method is called.
        /// </summary>
        protected override object Run(TRequest request)
        {
            return OnPost(request);
        }

        /// <summary>
        ///		The method may overriden by the descendent class to provide support for the 
        ///		GET verb on <see cref="TRequest"/> objects.
        /// </summary>
        /// <param name="request">The request object containing parameters for the GET
        ///		operation.</param>
        /// <returns>
        ///		A response object that the client expects, <see langword="null"/> if a response
        ///		object is not applicable, or an <see cref="HttpResult"/> object, to indicate an 
        ///		error condition to the client. The <see cref="HttpResult"/> object allows the 
        ///		implementation to control the exact HTTP status code returned to the client.  
        ///		Any return value other than <see cref="HttpResult"/> causes the HTTP status code 
        ///		of 200 to be returned to the client.
        /// </returns>
        public virtual object OnGet(TRequest request)
        {
            throw new NotImplementedException("This base method should be overridden but not called");
        }

        public object Get(TRequest request)
        {
            try
            {
                BeforeEachRequest(request);
                return AfterEachRequest(request, OnGet(request));
            }
            catch (Exception ex)
            {
                var result = HandleException(request, ex);

                if (result == null) throw;

                return result;
            }
        }

        /// <summary>
        ///		The method may overriden by the descendent class to provide support for the 
        ///		PUT verb on <see cref="TRequest"/> objects.
        /// </summary>
        /// <param name="request">The request object containing parameters for the PUT
        ///		operation.</param>
        /// <returns>
        ///		A response object that the client expects, <see langword="null"/> if a response
        ///		object is not applicable, or an <see cref="HttpResult"/> object, to indicate an 
        ///		error condition to the client. The <see cref="HttpResult"/> object allows the 
        ///		implementation to control the exact HTTP status code returned to the client.  
        ///		Any return value other than <see cref="HttpResult"/> causes the HTTP status code 
        ///		of 200 to be returned to the client.
        /// </returns>
        public virtual object OnPut(TRequest request)
        {
            throw new NotImplementedException("This base method should be overridden but not called");
        }

        public object Put(TRequest request)
        {
            try
            {
                BeforeEachRequest(request);
                return AfterEachRequest(request, OnPut(request));
            }
            catch (Exception ex)
            {
                var result = HandleException(request, ex);

                if (result == null) throw;

                return result;
            }
        }

        /// <summary>
        ///		The method may overriden by the descendent class to provide support for the 
        ///		POST verb on <see cref="TRequest"/> objects.
        /// </summary>
        /// <param name="request">The request object containing parameters for the POST
        ///		operation.</param>
        /// <returns>
        ///		A response object that the client expects, <see langword="null"/> if a response
        ///		object is not applicable, or an <see cref="HttpResult"/> object, to indicate an 
        ///		error condition to the client. The <see cref="HttpResult"/> object allows the 
        ///		implementation to control the exact HTTP status code returned to the client.  
        ///		Any return value other than <see cref="HttpResult"/> causes the HTTP status code 
        ///		of 200 to be returned to the client.
        /// </returns>
        public virtual object OnPost(TRequest request)
        {
            throw new NotImplementedException("This base method should be overridden but not called");
        }

        public object Post(TRequest request)
        {
            try
            {
                BeforeEachRequest(request);
                return AfterEachRequest(request, OnPost(request));
            }
            catch (Exception ex)
            {
                var result = HandleException(request, ex);

                if (result == null) throw;

                return result;
            }
        }

        /// <summary>
        ///		The method may overriden by the descendent class to provide support for the 
        ///		DELETE verb on <see cref="TRequest"/> objects.
        /// </summary>
        /// <param name="request">The request object containing parameters for the DELETE
        ///		operation.</param>
        /// <returns>
        ///		A response object that the client expects, <see langword="null"/> if a response
        ///		object is not applicable, or an <see cref="HttpResult"/> object, to indicate an 
        ///		error condition to the client. The <see cref="HttpResult"/> object allows the 
        ///		implementation to control the exact HTTP status code returned to the client.  
        ///		Any return value other than <see cref="HttpResult"/> causes the HTTP status code 
        ///		of 200 to be returned to the client.
        /// </returns>
        public virtual object OnDelete(TRequest request)
        {
            throw new NotImplementedException("This base method should be overridden but not called");
        }

        public object Delete(TRequest request)
        {
            try
            {
                BeforeEachRequest(request);
                return AfterEachRequest(request, OnDelete(request));
            }
            catch (Exception ex)
            {
                var result = HandleException(request, ex);

                if (result == null) throw;

                return result;
            }
        }

        /// <summary>
        ///		The method may overriden by the descendent class to provide support for the 
        ///		PATCH verb on <see cref="TRequest"/> objects.
        /// </summary>
        /// <param name="request">The request object containing parameters for the PATCH
        ///		operation.</param>
        /// <returns>
        ///		A response object that the client expects, <see langword="null"/> if a response
        ///		object is not applicable, or an <see cref="HttpResult"/> object, to indicate an 
        ///		error condition to the client. The <see cref="HttpResult"/> object allows the 
        ///		implementation to control the exact HTTP status code returned to the client.  
        ///		Any return value other than <see cref="HttpResult"/> causes the HTTP status code 
        ///		of 200 to be returned to the client.
        /// </returns>
        public virtual object OnPatch(TRequest request)
        {
            throw new NotImplementedException("This base method should be overridden but not called");
        }

        public object Patch(TRequest request)
        {
            try
            {
                BeforeEachRequest(request);
                return AfterEachRequest(request, OnPatch(request));
            }
            catch (Exception ex)
            {
                var result = HandleException(request, ex);

                if (result == null) throw;

                return result;
            }
        }
    }
}