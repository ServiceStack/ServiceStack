using System;

using Foundation;
using AppKit;

using ServiceStack;
using ServiceStack.Text;
using TechStacks.ServiceModel;

namespace PclTest.Mac
{
	public partial class MainWindowController : NSWindowController
	{
		JsonServiceClient client;

		public MainWindowController(IntPtr handle) : base(handle)
		{
		}

		[Export("initWithCoder:")]
		public MainWindowController(NSCoder coder) : base(coder)
		{
		}

		public MainWindowController() : base("MainWindow")
		{
		}

		public override void AwakeFromNib()
		{
			base.AwakeFromNib();
			client = new JsonServiceClient("http://techstacks.io");
		}

		public new MainWindow Window {
			get { return (MainWindow)base.Window; }
		}

		partial void btnGo_Click(NSObject sender)
		{
			var response = client.Get(new GetTechnology { Slug = "servicestack" });

			txtResults.StringValue = response.Dump();
		}
	}
}
