using System;
using System.Collections.Generic;
using System.Linq;

using MonoTouch.Foundation;
using MonoTouch.UIKit;

using Xamarin.Forms;
using Robotics.Mobile.Core.Bluetooth.LE;
using Xamarin.Forms.Platform.iOS;

namespace Robotics.Mobile.BtLEExplorer.iOS
{
	[Register ("AppDelegate")]
	public partial class AppDelegate : FormsApplicationDelegate // new int 1.3
	{
		UIWindow window;

		public override bool FinishedLaunching (UIApplication app, NSDictionary options)
		{
			Xamarin.Forms.Forms.Init ();

			window = new UIWindow (UIScreen.MainScreen.Bounds);

			App.SetAdapter (Adapter.Current);

			LoadApplication (new App ());
			
			return base.FinishedLaunching (app, options);
		}
	}
}

