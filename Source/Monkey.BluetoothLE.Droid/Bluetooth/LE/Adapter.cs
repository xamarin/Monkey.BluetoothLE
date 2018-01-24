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
		protected BluetoothManager blManager;
		protected BluetoothAdapter blAdapter;
		protected GattCallback gattCallback;

		public bool IsScanning
        {
			get { return isScanning; }
		}
        protected bool isScanning;

		public IList<IDevice> DiscoveredDevices
        {
			get
            {
				return discoveredDevices;
			}
		} protected IList<IDevice> discoveredDevices = new List<IDevice> ();

		public IList<IDevice> ConnectedDevices
        {
			get { return connectedDevices; }
		} protected IList<IDevice> connectedDevices = new List<IDevice>();


		public Adapter ()
		{
			var appContext = Android.App.Application.Context;
			// get a reference to the bluetooth system service
			this.blManager = (BluetoothManager) appContext.GetSystemService("bluetooth");
			this.blAdapter = this.blManager.Adapter;

			this.gattCallback = new GattCallback (this);

			this.gattCallback.DeviceConnected += (object sender, DeviceConnectionEventArgs e) => 
            {
				this.connectedDevices.Add ( e.Device);
				this.DeviceConnected (this, e);
			};

			this.gattCallback.DeviceDisconnected += (object sender, DeviceConnectionEventArgs e) => {
				// TODO: remove the disconnected device from the _connectedDevices list
				// i don't think this will actually work, because i'm created a new underlying device here.
				//if(this._connectedDevices.Contains(
				this.DeviceDisconnected (this, e);
			};
		}

		//TODO: scan for specific service type eg. HeartRateMonitor
		public /*async*/ void StartScanningForDevices (Guid serviceUuid)
		{
			StartScanningForDevices ();
//			throw new NotImplementedException ("Not implemented on Android yet, look at _adapter.StartLeScan() overload");
		}
		public async void StartScanningForDevices ()
		{
			Console.WriteLine ("Adapter: Starting a scan for devices.");

			// clear out the list
			discoveredDevices = new List<IDevice> ();

			// start scanning
			isScanning = true;

            //depricated in API level 21 
            blAdapter.StartLeScan(this);

			// in 10 seconds, stop the scan
			await Task.Delay (10000);

			// if we're still scanning
			if (this.isScanning) {
				Console.WriteLine ("BluetoothLEManager: Scan timeout has elapsed.");
				this.blAdapter.StopLeScan (this);
				this.ScanTimeoutElapsed (this, new EventArgs ());
			}
		}

		public void StopScanningForDevices ()
		{
			Console.WriteLine ("Adapter: Stopping the scan for devices.");
			this.isScanning = false;	
            this.blAdapter.StopLeScan (this);
		}

		public void OnLeScan (BluetoothDevice bleDevice, int rssi, byte[] scanRecord)
		{
			Console.WriteLine ("Adapter.LeScanCallback: " + bleDevice.Name);

			var device = new Device (bleDevice, null, null, rssi);

            if (DeviceExistsInDiscoveredList(bleDevice) == false)
            {
                discoveredDevices.Add(device);

                // TODO: in the cross platform API, cache the RSSI
                DeviceDiscovered(this, new DeviceDiscoveredEventArgs { Device = device });
            }
		}

		protected bool DeviceExistsInDiscoveredList(BluetoothDevice device)
		{
			foreach (var d in discoveredDevices)
            {
				if (device.Address == ((BluetoothDevice)d.NativeDevice).Address)
					return true;
			}
			return false;
		}

		public void ConnectToDevice (IDevice device)
		{
			// returns the BluetoothGatt, which is the API for BLE stuff
			((BluetoothDevice)device.NativeDevice).ConnectGatt (Android.App.Application.Context, true, gattCallback);
		}

		public void DisconnectDevice (IDevice device)
		{
			((Device) device).Disconnect();
		}

	}
}

