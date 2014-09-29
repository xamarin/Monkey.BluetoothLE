using System;
using System.Collections.Generic;
using System.Linq;

using MonoTouch.Foundation;
using MonoTouch.UIKit;

using Xamarin.Forms;
using Robotics.Mobile.Core.Bluetooth.LE;
using MonoTouch.CoreMotion;

namespace Robotics.Mobile.Robotroller.iOS
{
	[Register ("AppDelegate")]
	public partial class AppDelegate : UIApplicationDelegate, IGyro
	{
		UIWindow window;

		App app;

		CMMotionManager man = new CMMotionManager ();

		public override bool FinishedLaunching (UIApplication application, NSDictionary launchOptions)
		{
			Xamarin.Forms.Forms.Init ();

			// Pitch is speed pi/2 -> 0
			// Roll turning speed -p1/2 -> p1/2
			man.StartDeviceMotionUpdates (new NSOperationQueue (), (m, e) => {
				Roll = m.Attitude.Roll;
				Pitch = m.Attitude.Pitch;
				Yaw = m.Attitude.Yaw;
				GyroUpdated (this, EventArgs.Empty);
			});

			app = new App (Adapter.Current, this);

			window = new UIWindow (UIScreen.MainScreen.Bounds);
			window.RootViewController = app.GetMainPage ().CreateViewController ();
			window.MakeKeyAndVisible ();

			return true;
		}

		public double Roll { get; private set; }
		public double Pitch { get; private set; }
		public double Yaw { get; private set; }

		public event EventHandler GyroUpdated = delegate {};
	}
}

