
using System;
using MonoTouch.Foundation;
using MonoTouch.UIKit;

namespace RemoteInfoClient
{

	public partial class ViewTextFileController : UIViewController
	{
		#region Constructors

		// The IntPtr and NSCoder constructors are required for controllers that need 
		// to be able to be created from a xib rather than from managed code

		public ViewTextFileController (IntPtr handle) : base(handle)
		{
			Initialize ();
		}

		[Export("initWithCoder:")]
		public ViewTextFileController (NSCoder coder) : base(coder)
		{
			Initialize ();
		}

		public ViewTextFileController (string fileName, string fileContents) : base()
		{
			Initialize ();
			
			this.Title = fileName;
			this.fileContents = fileContents;
		}

		void Initialize ()
		{
		}
		
		#endregion
		
		private string fileContents;
		
		public override void ViewDidLoad ()
		{
			base.ViewDidLoad ();
			
			this.textView.Text = fileContents ?? string.Empty;
		}
		
	}
}
