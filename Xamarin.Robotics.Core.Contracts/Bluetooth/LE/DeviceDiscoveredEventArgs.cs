using System;

namespace Xamarin.Robotics.Core.Bluetooth.LE
{
	public class DeviceDiscoveredEventArgs : EventArgs
	{
		public IDevice Device;

		public DeviceDiscoveredEventArgs() : base()
		{}
	}
}

