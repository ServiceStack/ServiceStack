using System;
using System.Collections.Generic;
using System.Linq;

using Foundation;
using UIKit;

namespace PclTest.Ios10
{
	public class Application
	{
		// This is the main entry point of the application.
		static void Main(string[] args)
		{
			ServiceStack.IosPclExportClient.Configure();

			// if you want to use a different Application Delegate class from "AppDelegate"
			// you can specify it here.
			UIApplication.Main(args, null, "AppDelegate");
		}
	}
}
