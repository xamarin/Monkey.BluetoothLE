using System;
using System.Collections.Generic;
using Android.Bluetooth;
using System.Threading.Tasks;
using System.Linq;
using Android.Content;
using Java.Interop;

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

        public TimeSpan ScanTimeout { get; set; }

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

        private Context _appContext;

		public Adapter (Context appContext)
		{
            ScanTimeout = TimeSpan.FromSeconds(10); // default timeout is 10 seconds
            _appContext = appContext;
			// get a reference to the bluetooth system service
            this._manager = appContext.GetSystemService("bluetooth").JavaCast<BluetoothManager>();
			this._adapter = this._manager.Adapter;

			this._gattCallback = new GattCallback (this);

			this._gattCallback.DeviceConnected += (object sender, DeviceConnectionEventArgs e) => {
                if (ConnectedDevices.Find((BluetoothDevice)e.Device.NativeDevice) == null)
                {
                    _connectedDevices.Add ( e.Device);
				    this.DeviceConnected (this, e);
                }
			};

			this._gattCallback.DeviceDisconnected += (object sender, DeviceConnectionEventArgs e) => {
                var device = ConnectedDevices.Find((BluetoothDevice)e.Device.NativeDevice);
                if (device != null)
                {
                    _connectedDevices.Remove(device);
				    this.DeviceDisconnected (this, e);
                }
			};
		}

		//TODO: scan for specific service type eg. HeartRateMonitor
		public void StartScanningForDevices (Guid serviceUuid)
		{
			StartScanningForDevices ();
//			throw new NotImplementedException ("Not implemented on Android yet, look at _adapter.StartLeScan() overload");
		}

		public async void StartScanningForDevices ()
		{
			Console.WriteLine ("Adapter: Starting a scan for devices.");

			// clear out the list
            _discoveredDevices.Clear();

			// start scanning
			this._isScanning = true;
			this._adapter.StartLeScan (this);

			// after the given timeout, stop the scan
            await Task.Delay (ScanTimeout);

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

            if (DiscoveredDevices.Find(bleDevice) == null)
            {
                var device = new Device (bleDevice, null, null, rssi);
                this._discoveredDevices.Add(device);
                // TODO: in the cross platform API, cache the RSSI
                this.DeviceDiscovered(this, new DeviceDiscoveredEventArgs { Device = device });
            }
		}

		public void ConnectToDevice (IDevice device)
		{
			// returns the BluetoothGatt, which is the API for BLE stuff
			// TERRIBLE API design on the part of google here.
            var androidDevice = device as Device;
            androidDevice.Connect(_appContext, _gattCallback);
		}

		public void DisconnectDevice (IDevice device)
		{
			((Device) device).Disconnect();
		}
	}
}

