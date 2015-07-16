using System;

namespace Robotics.Mobile.Core.Bluetooth.LE
{
	public class DeviceDiscoveredEventArgs : EventArgs
	{
		public IDevice Device { get ; set ;}

		public int RSSI { get ; set ;}

		public byte[] ScanRecords { get ; set ; }

		public DeviceDiscoveredEventArgs() : base()
		{}
	}
}

