using System;

namespace Xamarin.Robotics.Mobile.Core.Bluetooth.LE
{
	public class DeviceDiscoveredEventArgs : EventArgs
	{
		public IDevice Device;

		public DeviceDiscoveredEventArgs() : base()
		{}
	}
}

