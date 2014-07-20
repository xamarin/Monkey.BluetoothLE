using System;
using Android.Bluetooth;
using Java.Util;
using Ble = Xamarin.Robotics.Core.Bluetooth.LE;

namespace Xamarin.Robotics.BluetoothLEExplorer.Droid
{
	public class State
	{
		public Ble.IDevice SelectedDevice { get; set; }
		public Ble.IService SelectedService { get; set; }
		public Ble.ICharacteristic SelectedCharacteristic { get; set; }

		public State ()
		{
		}

		public void WireUpBleEvents ()
		{
			App.Current.BleAdapter.DeviceDisconnected += (object sender, Ble.DeviceConnectionEventArgs e) => {
				this.ClearSelectedState();
			};

		}

		protected void ClearSelectedState()
		{
			this.SelectedDevice = null;
			this.SelectedService = null;
			this.SelectedCharacteristic = null;
		}
	}
}

