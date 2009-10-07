
using System;
using MonoTouch;

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

		public ViewTextFileController (string fileName, string fileContents) : base("ViewTextFileController", null)
		{
			Initialize ();
			
			this.Title = fileName;
			if (this.textView != null)
			{
				this.textView.Text = fileContents;
			}
		}

		void Initialize ()
		{
		}
		
		#endregion
		
		
		
	}
}
