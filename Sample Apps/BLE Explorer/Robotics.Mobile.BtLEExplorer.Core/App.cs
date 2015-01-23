using System;
using Xamarin.Forms;
using Robotics.Mobile.Core.Bluetooth.LE;

namespace Robotics.Mobile.BtLEExplorer
{
	public class App : Application // new in 1.3
	{
		static IAdapter Adapter;

		public App ()
		{	
			MainPage = new NavigationPage (new DeviceList (Adapter));
		}

		public static void SetAdapter (IAdapter adapter) {
			Adapter = adapter;
		}
	}
}

