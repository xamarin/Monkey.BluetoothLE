﻿using System;

using Android.App;
using Android.Content;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.OS;

using Xamarin.Forms.Platform.Android;
using Android.Content.PM;


namespace Robotics.Mobile.BtLEExplorer.Droid
{
	[Activity (Label = "Robotics.Mobile.BtLEExplorer.Android.Android", 
		MainLauncher = true, 
		ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation)]
	public class MainActivity : FormsApplicationActivity  // new int 1.3
	{
		protected override void OnCreate (Bundle bundle)
		{
			base.OnCreate (bundle);

			Xamarin.Forms.Forms.Init (this, bundle);

			var a = new Robotics.Mobile.Core.Bluetooth.LE.Adapter (this);
			App.SetAdapter (a);

			LoadApplication (new App ());
		}
	}
}

