using System;
using System.Runtime.Serialization;

namespace ServiceStack.Model.Version100
{
	public interface IResponseBase<TData, TResponseStatus> : IExtensibleDataObject
	{
		int Version { get; set; }

		TResponseStatus ResponseStatus { get; set; }

		TData ResponseData { get; set; }
	}
}