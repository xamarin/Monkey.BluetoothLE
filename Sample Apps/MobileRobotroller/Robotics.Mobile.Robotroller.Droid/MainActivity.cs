using System;

using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.OS;

using Xamarin.Forms.Platform.Android;

namespace Robotics.Mobile.Robotroller.Droid
{
	[Activity (Label = "Robotroller", 
		MainLauncher = true, 
		ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation)]
	public class MainActivity : AndroidActivity, IGyro
	{
		Robotics.Mobile.Robotroller.App app;

		protected override void OnCreate (Bundle bundle)
		{
			base.OnCreate (bundle);

			Xamarin.Forms.Forms.Init (this, bundle);

			var a = new Robotics.Mobile.Core.Bluetooth.LE.Adapter ();

			app = new Robotics.Mobile.Robotroller.App (a, this);

			SetPage (app.GetMainPage ());
		}

		#region IGyro implementation

		//HACK: this still needs to be implemented for Android

		public event EventHandler GyroUpdated;

		public double Roll {
			get {
				return 0.5;
				//throw new NotImplementedException ();
			}
		}

		public double Pitch {
			get {
				return 0.5;
				//throw new NotImplementedException ();
			}
		}

		public double Yaw {
			get {
				return 0.1;
				//throw new NotImplementedException ();
			}
		}

		#endregion
	}
}

