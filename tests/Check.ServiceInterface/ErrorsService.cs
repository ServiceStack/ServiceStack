﻿using System;
using System.IO;
using System.Net;
using Check.ServiceModel;
using ServiceStack;
using ServiceStack.Data;
using ServiceStack.FluentValidation;

namespace Check.ServiceInterface
{
    public class ErrorsService : Service
    {
        public object Any(ThrowHttpError request)
        {
            throw new HttpError(request.Status, request.Message);
        }

        public object Any(Throw404 request)
        {
            throw HttpError.NotFound(request.Message ?? "Custom Status Description");
        }

        public object Any(Return404 request)
        {
            return HttpError.NotFound("Custom Status Description");
        }

        public object Any(Return404Result request)
        {
            return new HttpResult(HttpStatusCode.NotFound, "Custom Status Description");
        }

        public object Any(ThrowType request)
        {
            switch (request.Type ?? "Exception")
            {
                case "Exception":
                    throw new Exception(request.Message ?? "Server Error");
                case "NotFound":
                    throw HttpError.NotFound(request.Message ?? "What you're looking for isn't here");
                case "Unauthorized":
                    throw HttpError.Unauthorized(request.Message ?? "You shall not pass!");
                case "Conflict":
                    throw HttpError.Conflict(request.Message ?? "We haz Conflict!");
                case "NotImplementedException":
                    throw new NotImplementedException(request.Message ?? "Not implemented yet, try again later");
                case "ArgumentException":
                    throw new ArgumentException(request.Message ?? "Client Argument Error");
                case "AuthenticationException":
                    throw new AuthenticationException(request.Message ?? "We haz issue Authenticatting");
                case "UnauthorizedAccessException":
                    throw new UnauthorizedAccessException(request.Message ?? "You shall not pass!");
                case "OptimisticConcurrencyException":
                    throw new OptimisticConcurrencyException(request.Message ?? "Sorry too optimistic");
                case "UnhandledException":
                    throw new FileNotFoundException(request.Message ?? "File was never here");
                case "RawResponse":
                    Response.StatusCode = 418;
                    Response.StatusDescription = request.Message ?? "On a tea break";
                    Response.EndRequest();
                    break;
            }

            return request;
        }

        public object Any(ThrowValidation request)
        {
            return request.ConvertTo<ThrowValidationResponse>();
        }
    }

    public class ThrowValidationValidator : AbstractValidator<ThrowValidation>
    {
        public ThrowValidationValidator()
        {
            RuleFor(x => x.Age).InclusiveBetween(1, 120);
            RuleFor(x => x.Required).NotEmpty();
            RuleFor(x => x.Email).NotEmpty().EmailAddress();
        }
    }
}