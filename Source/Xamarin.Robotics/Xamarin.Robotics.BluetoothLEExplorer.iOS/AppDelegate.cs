using System;
using System.Collections.Generic;
using System.Linq;
using MonoTouch.Foundation;
using MonoTouch.UIKit;
using Xamarin.Robotics.BluetoothLEExplorer.iOS.UI.Screens.Scanner.Home;

namespace Xamarin.Robotics.BluetoothLEExplorer.iOS
{
	// The UIApplicationDelegate for the application. This class is responsible for launching the
	// User Interface of the application, as well as listening (and optionally responding) to
	// application events from iOS.
	[Register ("AppDelegate")]
	public partial class AppDelegate : UIApplicationDelegate
	{
		// class-level declarations
		UIWindow window;
		UINavigationController _nav;

		public override bool FinishedLaunching (UIApplication app, NSDictionary options)
		{
			// create a new window instance based on the screen size
			window = new UIWindow (UIScreen.MainScreen.Bounds);
			
			this._nav = new UINavigationController (new ScannerHome ());

			this.window.RootViewController = _nav;

			// make the window visible
			window.MakeKeyAndVisible ();
			
			return true;
		}
	}
}

