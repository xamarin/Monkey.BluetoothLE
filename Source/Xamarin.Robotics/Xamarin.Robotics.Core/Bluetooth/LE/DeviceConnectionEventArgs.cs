using System;

namespace Xamarin.Robotics.Core.Bluetooth.LE
{
	public class DeviceConnectionEventArgs : EventArgs
	{
		public IDevice Device;
		public string ErrorMessage;

		public DeviceConnectionEventArgs() : base()
		{}
	}
}

