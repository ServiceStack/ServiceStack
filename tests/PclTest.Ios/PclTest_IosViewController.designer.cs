// WARNING
//
// This file has been generated automatically by Xamarin Studio to store outlets and
// actions made in the UI designer. If it is removed, they will be lost.
// Manual changes to this file may not be handled correctly.
//
using MonoTouch.Foundation;
using System.CodeDom.Compiler;

namespace PclTest.Ios
{
	[Register ("PclTest_IosViewController")]
	partial class PclTest_IosViewController
	{
		[Outlet]
		MonoTouch.UIKit.UIButton btnGoAsync { get; set; }

		[Outlet]
		MonoTouch.UIKit.UIButton btnGoSync { get; set; }

		[Outlet]
		MonoTouch.UIKit.UILabel lblResults { get; set; }

		[Outlet]
		MonoTouch.UIKit.UITextField txtName { get; set; }

		[Action ("btnGoAsync_Click:")]
		partial void btnGoAsync_Click (MonoTouch.Foundation.NSObject sender);

		[Action ("btnGoSync_Click:")]
		partial void btnGoSync_Click (MonoTouch.Foundation.NSObject sender);
		
		void ReleaseDesignerOutlets ()
		{
			if (btnGoSync != null) {
				btnGoSync.Dispose ();
				btnGoSync = null;
			}

			if (btnGoAsync != null) {
				btnGoAsync.Dispose ();
				btnGoAsync = null;
			}

			if (txtName != null) {
				txtName.Dispose ();
				txtName = null;
			}

			if (lblResults != null) {
				lblResults.Dispose ();
				lblResults = null;
			}
		}
	}
}
