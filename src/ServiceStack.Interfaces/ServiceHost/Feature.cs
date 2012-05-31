using System;

namespace ServiceStack.ServiceHost
{
	[Flags]
	public enum Feature : int
	{
		None         = 0,
		All          = int.MaxValue,
		Soap         = Soap11 | Soap12,

		Json         = 1 << 0,
		Xml          = 1 << 1,
		Jsv          = 1 << 2,
		Soap11       = 1 << 3,
		Soap12       = 1 << 4,
		Csv          = 1 << 5,
		Html         = 1 << 6,
		CustomFormat = 1 << 7,
		Metadata     = 1 << 8,
		Markdown     = 1 << 9,
		Razor        = 1 << 10,
		ProtoBuf     = 1 << 11,
	}
}