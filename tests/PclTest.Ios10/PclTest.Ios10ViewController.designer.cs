// WARNING
//
// This file has been generated automatically by Xamarin Studio from the outlets and
// actions declared in your storyboard file.
// Manual changes to this file will not be maintained.
//
using System;
using Foundation;
using UIKit;
using System.CodeDom.Compiler;

namespace PclTest.Ios10
{
	[Register ("PclTest_Ios10ViewController")]
	partial class PclTest_Ios10ViewController
	{
		[Outlet]
		[GeneratedCode ("iOS Designer", "1.0")]
		UIButton btnGoAsync { get; set; }

		[Outlet]
		[GeneratedCode ("iOS Designer", "1.0")]
		UIButton btnGoAwait { get; set; }

		[Outlet]
		[GeneratedCode ("iOS Designer", "1.0")]
		UIButton btnGoSync { get; set; }

		[Outlet]
		[GeneratedCode ("iOS Designer", "1.0")]
		UILabel lblResults { get; set; }

		[Outlet]
		[GeneratedCode ("iOS Designer", "1.0")]
		UITextField txtName { get; set; }

		[Action ("btnGoAsync_TouchUpInside:")]
		[GeneratedCode ("iOS Designer", "1.0")]
		partial void btnGoAsync_TouchUpInside (UIButton sender);

		[Action ("btnGoAwait_TouchUpInside:")]
		[GeneratedCode ("iOS Designer", "1.0")]
		partial void btnGoAwait_TouchUpInside (UIButton sender);

		[Action ("btnGoSync_TouchUpInside:")]
		[GeneratedCode ("iOS Designer", "1.0")]
		partial void btnGoSync_TouchUpInside (UIButton sender);

		void ReleaseDesignerOutlets ()
		{
			if (btnGoAsync != null) {
				btnGoAsync.Dispose ();
				btnGoAsync = null;
			}
			if (btnGoAwait != null) {
				btnGoAwait.Dispose ();
				btnGoAwait = null;
			}
			if (btnGoSync != null) {
				btnGoSync.Dispose ();
				btnGoSync = null;
			}
			if (lblResults != null) {
				lblResults.Dispose ();
				lblResults = null;
			}
			if (txtName != null) {
				txtName.Dispose ();
				txtName = null;
			}
		}
	}
}
