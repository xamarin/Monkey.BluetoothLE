using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;
using System.Diagnostics;
using System.Linq;

#if __UNIFIED__
using CoreBluetooth;
using CoreFoundation;
using Foundation;
#else
using MonoTouch.Foundation;
using MonoTouch.CoreBluetooth;
using MonoTouch.CoreFoundation;
#endif

namespace Robotics.Mobile.Core.Bluetooth.LE
{
	public class Adapter : IAdapter
	{
		// events
		public event EventHandler<DeviceDiscoveredEventArgs> DeviceDiscovered = delegate {};
		public event EventHandler<DeviceConnectionEventArgs> DeviceConnected = delegate {};
		public event EventHandler<DeviceConnectionEventArgs> DeviceDisconnected = delegate {};
		public event EventHandler<DeviceConnectionEventArgs> DeviceFailedToConnect = delegate {};
		public event EventHandler ScanTimeoutElapsed = delegate {};
		public event EventHandler ConnectTimeoutElapsed = delegate {};

		public CBCentralManager Central
		{ get { return this._central; } }
		protected CBCentralManager _central;

		public bool IsScanning {
			get { return this._isScanning; }
		} protected bool _isScanning;

		public bool IsConnecting {
			get { return this._isConnecting; }
		} protected bool _isConnecting;

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

		public static Adapter Current
		{ get { return _current; } }
		private static Adapter _current;

		static Adapter ()
		{
			_current = new Adapter ();
		}

		public Adapter ()
		{
			this._central = new CBCentralManager (DispatchQueue.CurrentQueue);

			_central.DiscoveredPeripheral += (object sender, CBDiscoveredPeripheralEventArgs e) => {
				Console.WriteLine ("DiscoveredPeripheral: " + e.Peripheral.Name);

				NSString localName = null;

				try {
					localName = e.AdvertisementData[CBAdvertisement.DataLocalNameKey] as NSString;
				} catch {
					localName = new NSString(e.Peripheral.Name);
				}

				Device d = new Device(e.Peripheral, localName);

				if(!ContainsDevice(this._discoveredDevices, e.Peripheral ) ){

					byte[] scanRecord = null;


					try {

						Console.WriteLine ("ScanRecords: " + e.AdvertisementData.ToString());

						NSError error = null;

						var soft = Clean(e.AdvertisementData);

						var data = NSJsonSerialization.Serialize(soft, 0x00, out error);

						scanRecord = data.ToArray ();

					} catch (Exception exception)
					{
						Console.WriteLine ("ScanRecords to byte[] failed");
					}
					finally {
						this._discoveredDevices.Add (d);
						this.DeviceDiscovered(this, new DeviceDiscoveredEventArgs() { Device = d, RSSI = (int)e.RSSI, ScanRecords = scanRecord });
					}

				}
			};

			_central.UpdatedState += (object sender, EventArgs e) => {
				Console.WriteLine ("UpdatedState: " + _central.State);
				stateChanged.Set ();
			};


			_central.ConnectedPeripheral += (object sender, CBPeripheralEventArgs e) => {
				Console.WriteLine ("ConnectedPeripheral: " + e.Peripheral.Name);

				// when a peripheral gets connected, add that peripheral to our running list of connected peripherals
				if(!ContainsDevice(this._connectedDevices, e.Peripheral ) ){
					Device d = new Device(e.Peripheral);
					this._connectedDevices.Add (new Device(e.Peripheral));
					// raise our connected event
					this.DeviceConnected ( sender, new DeviceConnectionEventArgs () { Device = d } );
				}			
			};

			_central.DisconnectedPeripheral += (object sender, CBPeripheralErrorEventArgs e) => {
				Console.WriteLine ("DisconnectedPeripheral: " + e.Peripheral.Name);

				// when a peripheral disconnects, remove it from our running list.
				IDevice foundDevice = null;
				foreach (var d in this._connectedDevices) {
					if (d.ID == Guid.ParseExact(e.Peripheral.Identifier.AsString(), "d"))
						foundDevice = d;
				}
				if (foundDevice != null)
					this._connectedDevices.Remove(foundDevice);

				// raise our disconnected event
				this.DeviceDisconnected (sender, new DeviceConnectionEventArgs() { Device = new Device(e.Peripheral) });
			};

			_central.FailedToConnectPeripheral += (object sender, CBPeripheralErrorEventArgs e) => {
				// raise the failed to connect event
				this.DeviceFailedToConnect(this, new DeviceConnectionEventArgs() { 
					Device = new Device (e.Peripheral),
					ErrorMessage = e.Error.Description
				});
			};


		}
			
		public void StartScanningForDevices ()
		{
			StartScanningForDevices (serviceUuid: Guid.Empty);
		}

		readonly AutoResetEvent stateChanged = new AutoResetEvent (false);

		async Task WaitForState (CBCentralManagerState state)
		{
			Debug.WriteLine ("Adapter: Waiting for state: " + state);

			while (_central.State != state) {
				await Task.Run (() => stateChanged.WaitOne ());
			}
		}

		public async void StartScanningForDevices (Guid serviceUuid)
		{
			//
			// Wait for the PoweredOn state
			//
			await WaitForState (CBCentralManagerState.PoweredOn);

			Debug.WriteLine ("Adapter: Starting a scan for devices.");

			CBUUID[] serviceUuids = null; // TODO: convert to list so multiple Uuids can be detected
			if (serviceUuid != Guid.Empty) {
				var suuid = CBUUID.FromString (serviceUuid.ToString ());
				serviceUuids = new CBUUID[] { suuid };
				Debug.WriteLine ("Adapter: Scanning for " + suuid);
			}

			// clear out the list
			this._discoveredDevices = new List<IDevice> ();

			// start scanning
			this._isScanning = true;
			this._central.ScanForPeripherals ( serviceUuids );

			// in 10 seconds, stop the scan
			await Task.Delay (10000);

			// if we're still scanning
			if (this._isScanning) {
				Console.WriteLine ("BluetoothLEManager: Scan timeout has elapsed.");
				this._isScanning = false;
				this._central.StopScan ();
				this.ScanTimeoutElapsed (this, new EventArgs ());
			}
		}

		public void StopScanningForDevices ()
		{
			Console.WriteLine ("Adapter: Stopping the scan for devices.");
			this._isScanning = false;	
			this._central.StopScan ();
		}

		public void ConnectToDevice (IDevice device)
		{
			//TODO: if it doesn't connect after 10 seconds, cancel the operation
			// (follow the same model we do for scanning).
			this._central.ConnectPeripheral (device.NativeDevice as CBPeripheral, new PeripheralConnectionOptions());
				
//			// in 10 seconds, stop the connection
//			await Task.Delay (10000);
//
//			// if we're still trying to connect
//			if (this._isConnecting) {
//				Console.WriteLine ("BluetoothLEManager: Connect timeout has elapsed.");
//				this._central.
//				this.ConnectTimeoutElapsed (this, new EventArgs ());
//			}
		}
			
		public void DisconnectDevice (IDevice device)
		{
			this._central.CancelPeripheralConnection (device.NativeDevice as CBPeripheral);
		}

		// util
		protected bool ContainsDevice(IEnumerable<IDevice> list, CBPeripheral device)
		{
			foreach (var d in list) {
				if (Guid.ParseExact(device.Identifier.AsString(), "d") == d.ID)
					return true;
			}
			return false;
		}

		NSDictionary Clean (NSDictionary dict)
		{
			var result = new NSMutableDictionary ();

			foreach (var item in dict.Keys) {

				var obj = dict [item];

				// Si la clef est de type CBUUID, on utilisera l'id en NSString a la place
				var itemAsCBUUID = item as CBUUID;
				var key = itemAsCBUUID == null ? new NSString (item.Description) : new NSString (itemAsCBUUID.Uuid);

				// La valeur doit etre transformée
				NSObject value = null;

				if (obj as NSDictionary != null) // Cas d'un NSDictionary... Recursivité
				{
					value = Clean (obj as NSDictionary);
				} 
				else if (obj as NSData != null) // Cas d'un NSDictionary... Stringify
				{
					var data = obj as NSData;

					var networkBites = data.ToArray ();

					var str = BitConverter.ToString(networkBites).Replace("-","");

					value = new NSString (str);

				} 
				else if (obj as NSNull != null) // Cas d'un NSNull, on skip
				{
					// Do not...

				} else // Sinon on remet l'objet sous format string
				{
					value = new NSString(obj.Description);
				}

				result.SetObject (value, key);

			}

			return result.Copy() as NSDictionary;
		}
	}
}

