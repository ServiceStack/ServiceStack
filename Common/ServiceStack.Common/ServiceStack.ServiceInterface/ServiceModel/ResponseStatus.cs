/*
// $Id: ResponseStatus.cs 11037 2010-02-03 12:36:14Z Demis Bellot $
//
// Revision      : $Revision: 11037 $
// Modified Date : $LastChangedDate: 2010-02-03 12:36:14 +0000 (Wed, 03 Feb 2010) $
// Modified By   : $LastChangedBy: Demis Bellot $
//
// (c) Copyright 2010 Liquidbit Ltd
*/

using System.Collections.Generic;
using System.Runtime.Serialization;

namespace ServiceStack.ServiceInterface.ServiceModel
{
	/// <summary>
	/// Common ResponseStatus class that should be present on all response DTO's
	/// </summary>
	[DataContract(Namespace = Config.DefaultNamespace)]
	public class ResponseStatus
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="ResponseStatus"/> class.
		/// 
		/// A response status without an errorcode == success
		/// </summary>
		public ResponseStatus()
		{
			this.Errors = new List<ResponseError>();
		}
        
		/// <summary>
		/// Holds the custom ErrorCode enum if provided in ValidationException
		/// otherwise will hold the name of the Exception type, e.g. typeof(Exception).Name
		/// 
		/// A value of non-null means the service encountered an error while processing the request.
		/// </summary>
		[DataMember]
		public string ErrorCode { get; set; }

		/// <summary>
		/// Not sent over the wire but makes client code more readable when checking for errors.
		/// </summary>
		public bool IsSuccess
		{
			get { return this.ErrorCode == null; }
		}

		/// <summary>
		/// A human friendly error message
		/// </summary>
		[DataMember]
		public string Message { get; set; }

		/// <summary>
		/// 
		/// </summary>
		[DataMember]
		public string StackTrace { get; set; }

		/// <summary>
		/// For multiple detailed validation errors.
		/// Can hold a specific error message for each named field.
		/// </summary>
		[DataMember]
		public List<ResponseError> Errors { get; set; }
	}
}