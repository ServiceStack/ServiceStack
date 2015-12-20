// WARNING
//
// This file has been generated automatically by Xamarin Studio to store outlets and
// actions made in the UI designer. If it is removed, they will be lost.
// Manual changes to this file may not be handled correctly.
//
using Foundation;
using System.CodeDom.Compiler;

namespace PclTest.Mac
{
	[Register ("MainWindowController")]
	partial class MainWindowController
	{
		[Outlet]
		AppKit.NSButton btnGo { get; set; }

		[Outlet]
		AppKit.NSTextField txtResults { get; set; }

		[Action ("btnGo_Click:")]
		partial void btnGo_Click (Foundation.NSObject sender);
		
		void ReleaseDesignerOutlets ()
		{
			if (btnGo != null) {
				btnGo.Dispose ();
				btnGo = null;
			}

			if (txtResults != null) {
				txtResults.Dispose ();
				txtResults = null;
			}
		}
	}
}
