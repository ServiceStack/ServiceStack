
namespace RemoteControlClient
{
	// Base type probably should be MonoTouch.Foundation.NSObject or subclass
	[MonoTouch.Foundation.Register("AppDelegate")]
	public partial class AppDelegate
	{

		[MonoTouch.Foundation.Connect("window")]
		private MonoTouch.UIKit.UIWindow window {
			get { return ((MonoTouch.UIKit.UIWindow)(this.GetNativeField ("window"))); }
			set { this.SetNativeField ("window", value); }
		}
	}
}
