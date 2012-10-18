/*
// $Id: ResponseError.cs 11037 2010-02-03 12:36:14Z Demis Bellot $
//
// Revision      : $Revision: 11037 $
// Modified Date : $LastChangedDate: 2010-02-03 12:36:14 +0000 (Wed, 03 Feb 2010) $
// Modified By   : $LastChangedBy: Demis Bellot $
//
// (c) Copyright 2010 Liquidbit Ltd
*/

using System.Runtime.Serialization;

namespace ServiceStack.ServiceInterface.ServiceModel
{
	/// <summary>
	/// Error information pertaining to a particular named field.
	/// Used for returning multiple field validation errors.s
	/// </summary>
	[DataContract]
	public class ResponseError
	{
		[DataMember]
		public string ErrorCode { get; set; }

		[DataMember]
		public string FieldName { get; set; }
	
		[DataMember]
		public string Message { get; set; }
	}
}