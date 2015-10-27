using System;

using AppKit;

namespace PclTest.Mac
{
	static class MainClass
	{
		static void Main(string[] args)
		{
			ServiceStack.MacPclExportClient.Configure();

			NSApplication.Init();
			NSApplication.Main(args);
		}
	}
}
