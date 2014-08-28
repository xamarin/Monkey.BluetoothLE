using System;
using System.Collections.Generic;
using System.Linq;

using MonoTouch.Foundation;
using MonoTouch.UIKit;

using Xamarin.Forms;
using Xamarin.Robotics.Mobile.Core.Bluetooth.LE;

namespace Xamarin.Robotics.Mobile.Robotroller.iOS
{
	[Register ("AppDelegate")]
	public partial class AppDelegate : UIApplicationDelegate
	{
		UIWindow window;

		public override bool FinishedLaunching (UIApplication app, NSDictionary options)
		{
			Xamarin.Forms.Forms.Init ();

			window = new UIWindow (UIScreen.MainScreen.Bounds);

			window.RootViewController = App.GetMainPage (Adapter.Current).CreateViewController ();
			window.MakeKeyAndVisible ();

			return true;
		}
	}
}

