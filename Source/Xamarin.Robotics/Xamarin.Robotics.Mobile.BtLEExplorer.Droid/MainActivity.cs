using System;

using Android.App;
using Android.Content;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.OS;

using Xamarin.Forms.Platform.Android;
using Android.Content.PM;


namespace Xamarin.Robotics.Mobile.BtLEExplorer.Droid
{
	[Activity (Label = "Xamarin.Robotics.Mobile.BtLEExplorer.Android.Android", 
		MainLauncher = true, 
		ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation)]
	public class MainActivity : AndroidActivity
	{
		protected override void OnCreate (Bundle bundle)
		{
			base.OnCreate (bundle);

			Xamarin.Forms.Forms.Init (this, bundle);

			var a = new Xamarin.Robotics.Mobile.Core.Bluetooth.LE.Adapter ();
			App.SetAdapter (a);

			SetPage (App.GetMainPage ());
		}
	}
}

