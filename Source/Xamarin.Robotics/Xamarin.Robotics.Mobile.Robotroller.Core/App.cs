using System;
using Xamarin.Forms;
using Xamarin.Robotics.Mobile.Core.Bluetooth.LE;

namespace Xamarin.Robotics.Mobile.Robotroller
{
	public class App
	{
		public static Page GetMainPage (IAdapter adapter)
		{	
			return new NavigationPage (new DeviceList (adapter));
		}
	}
}

