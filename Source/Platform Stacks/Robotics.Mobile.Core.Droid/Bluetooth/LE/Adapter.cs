using System;
using System.Collections.Generic;
using Android.Bluetooth;
using System.Threading.Tasks;
using Java.Util;

namespace Robotics.Mobile.Core.Bluetooth.LE
{
	/// <summary>
	/// TODO: this really should be a singleton.
	/// </summary>
	public class Adapter : Java.Lang.Object, BluetoothAdapter.ILeScanCallback, IAdapter
	{
		// events
		public event EventHandler<DeviceDiscoveredEventArgs> DeviceDiscovered = delegate {};
		public event EventHandler<DeviceConnectionEventArgs> DeviceConnected = delegate {};
		public event EventHandler<DeviceConnectionEventArgs> DeviceDisconnected = delegate {};
		public event EventHandler ScanTimeoutElapsed = delegate {};

		// class members
		protected BluetoothManager _manager;
		protected BluetoothAdapter _adapter;

		public bool IsScanning {
			get { return this._isScanning; }
		} protected bool _isScanning;

		public IList<IDevice> DiscoveredDevices {
			get {
				return this._discoveredDevices;
			}
		} protected IList<IDevice> _discoveredDevices = new List<IDevice> ();

		public IList<IDevice> ConnectedDevices {
			get {
				return this._connectedDevices;
			}
		} protected IList<IDevice> _connectedDevices = new List<IDevice>();


		public Adapter ()
		{
			var appContext = Android.App.Application.Context;
			// get a reference to the bluetooth system service
			this._manager = (BluetoothManager) appContext.GetSystemService("bluetooth");
			this._adapter = this._manager.Adapter;
		}

        public async void StartScanningForDevices ()
        {
            this.StartScanningForDevices(Guid.Empty);
        }

        public async void StartScanningForDevices (Guid serviceUuid)
		{
			Console.WriteLine ("Adapter: Starting a scan for devices.");

			// clear out the list
			this._discoveredDevices = new List<IDevice> ();

            UUID[] serviceUuids = null;
            if (serviceUuid != Guid.Empty)
            {
                serviceUuids = new UUID[1];
                serviceUuids[0] = UUID.FromString(serviceUuid.ToString());
            }

			// start scanning
			this._isScanning = true;
            this._adapter.StartLeScan (serviceUuids, this);

			// in 10 seconds, stop the scan
			await Task.Delay (10000);

			// if we're still scanning
			if (this._isScanning) {
				Console.WriteLine ("BluetoothLEManager: Scan timeout has elapsed.");
				this._adapter.StopLeScan (this);
				this.ScanTimeoutElapsed (this, new EventArgs ());
			}
		}

		public void StopScanningForDevices ()
		{
			Console.WriteLine ("Adapter: Stopping the scan for devices.");
			this._isScanning = false;	
			this._adapter.StopLeScan (this);
		}

		public void OnLeScan (BluetoothDevice bleDevice, int rssi, byte[] scanRecord)
		{
			Console.WriteLine ("Adapter.LeScanCallback: " + bleDevice.Name);
			// TODO: for some reason, this doesn't work, even though they have the same pointer,
			// it thinks that the item doesn't exist. so i had to write my own implementation
//			if(!this._discoveredDevices.Contains(device) ) {
//				this._discoveredDevices.Add (device );
//			}
			Device device = new Device (bleDevice, null, null, rssi);

			if (!DeviceExistsInDiscoveredList (bleDevice))
				this._discoveredDevices.Add	(device);
			// TODO: in the cross platform API, cache the RSSI
			// TODO: shouldn't i only raise this if it's not already in the list?
			this.DeviceDiscovered (this, new DeviceDiscoveredEventArgs { Device = device });
		}

		protected bool DeviceExistsInDiscoveredList(BluetoothDevice device)
		{
			foreach (var d in this._discoveredDevices) {
				// TODO: verify that address is unique
				if (device.Address == ((BluetoothDevice)d.NativeDevice).Address)
					return true;
			}
			return false;
		}


		public void ConnectToDevice (IDevice device)
		{
			var androidBleDevice = (Device)device;
			if (androidBleDevice._gatt == null) {
				var gattCallback = new GattCallback (androidBleDevice);
				gattCallback.DeviceConnected += (object sender, DeviceConnectionEventArgs e) => {
					this._connectedDevices.Add (e.Device);
					this.DeviceConnected (this, e);
				};

				gattCallback.DeviceDisconnected += (object sender, DeviceConnectionEventArgs e) => {
					this._connectedDevices.Remove (e.Device);
					this.DeviceDisconnected (this, e);
				};

				androidBleDevice.GattCallback = gattCallback;
				androidBleDevice._gatt = ((BluetoothDevice)device.NativeDevice).ConnectGatt (Android.App.Application.Context, false, gattCallback);
				var success = androidBleDevice._gatt.Connect ();
				Console.WriteLine(string.Format("Initial connection attempt is {0}", success));
			} else {
				switch (androidBleDevice.State) {
				case DeviceState.Disconnected:
					androidBleDevice.Disconnect ();
					this.ConnectToDevice (androidBleDevice);
					break;
				}
			}
		}

		public void DisconnectDevice (IDevice device)
		{
			((Device) device).Disconnect();
		}

	}
}

