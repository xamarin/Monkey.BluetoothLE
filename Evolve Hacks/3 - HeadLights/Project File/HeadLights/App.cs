using System;
using Xamarin.Forms;
using Robotics.Mobile.Core.Bluetooth.LE;

namespace HeadLights
{
	public class App
	{
		public static Page GetMainPage (IAdapter adapter)
		{	
			return new MainPage (adapter);
		}
	}
}

