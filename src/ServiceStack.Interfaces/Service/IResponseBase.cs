using System;
using System.Runtime.Serialization;

namespace ServiceStack.Service
{
	public interface IResponseBase<TData, TResponseStatus> 
	{
		int Version { get; set; }

		TResponseStatus ResponseStatus { get; set; }

		TData ResponseData { get; set; }
	}
}