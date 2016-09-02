using System;
using System.Collections.Generic;
using Android.Bluetooth;
using System.Threading.Tasks;

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
		protected GattCallback _gattCallback;

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

			this._gattCallback = new GattCallback (this);

			this._gattCallback.DeviceConnected += (object sender, DeviceConnectionEventArgs e) => {
				this._connectedDevices.Add ( e.Device);
				this.DeviceConnected (this, e);
			};

			this._gattCallback.DeviceDisconnected += (object sender, DeviceConnectionEventArgs e) => {
				// TODO: remove the disconnected device from the _connectedDevices list
				// i don't think this will actually work, because i'm created a new underlying device here.
				//if(this._connectedDevices.Contains(
				this.DeviceDisconnected (this, e);
			};
		}

		//TODO: scan for specific service type eg. HeartRateMonitor
		public async void StartScanningForDevices (Guid serviceUuid)
		{
			StartScanningForDevices ();
//			throw new NotImplementedException ("Not implemented on Android yet, look at _adapter.StartLeScan() overload");
		}
		
		public async void StartScanningForDevices (int timeOutSeconds = 10)
		{
			Console.WriteLine ("Adapter: Starting a scan for devices.");

			// clear out the list
			this._discoveredDevices = new List<IDevice> ();

			// start scanning
			this._isScanning = true;
			this._adapter.StartLeScan (this);

			// in 10 seconds, stop the scan
			await Task.Delay(TimeSpan.FromSeconds(timeOutSeconds));

			// if we're still scanning
			if (this._isScanning) {
				Console.WriteLine ("BluetoothLEManager: Scan timeout has elapsed.");
				// Use the method, because it also updates the _isScanning portion.
				StopScanningForDevices();
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
			// returns the BluetoothGatt, which is the API for BLE stuff
			// TERRIBLE API design on the part of google here.
			((BluetoothDevice)device.NativeDevice).ConnectGatt (Android.App.Application.Context, true, this._gattCallback);
		}

		public void DisconnectDevice (IDevice device)
		{
			((Device) device).Disconnect();
		}

	}
}

