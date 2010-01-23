/*
// $Id$
//
// Revision      : $Revision: 258 $
// Modified Date : $LastChangedDate: 2008-11-28 17:02:44 +0000 (Fri, 28 Nov 2008) $
// Modified By   : $LastChangedBy$
//
// (c) Copyright 2008 Digital Distribution Networks Ltd
*/

using System;
using System.Runtime.Serialization;

namespace ServiceStack.DataAccess
{
	public class DataAccessException : Exception
	{
		public DataAccessException()
		{
		}

		public DataAccessException(string message) 
			: base(message)
		{
		}

		public DataAccessException(string message, Exception innerException) 
			: base(message, innerException)
		{
		}

		protected DataAccessException(SerializationInfo info, StreamingContext context) 
			: base(info, context)
		{
		}
	}
}