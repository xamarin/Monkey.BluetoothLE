using System;
using Xamarin.Forms;
using Xamarin.Robotics.Mobile.Core.Bluetooth.LE;

namespace Xamarin.Robotics.Mobile.Robotroller
{
	public class App
	{
		public static App Shared { get; private set; }

		public IGyro Gyro { get; private set; }
		public IAdapter Adapter { get; private set; }

		public App (IAdapter adapter, IGyro gyro)
		{
			Shared = this;
			Adapter = adapter;
			Gyro = gyro;
		}

		public Page GetMainPage ()
		{	
			return new NavigationPage (new DeviceList (Adapter));
		}
	}
}

