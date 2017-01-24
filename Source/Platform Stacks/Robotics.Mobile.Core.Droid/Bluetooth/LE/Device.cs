using System;
using System.Collections.Generic;
using Android.Bluetooth;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;

namespace Robotics.Mobile.Core.Bluetooth.LE
{
	public class Device : DeviceBase
	{
		public override event EventHandler ServicesDiscovered = delegate {};

		protected BluetoothDevice _nativeDevice;
		/// <summary>
		/// we have to keep a reference to this because Android's api is weird and requires
		/// the GattServer in order to do nearly anything, including enumerating services
		/// 
		/// TODO: consider wrapping the Gatt and Callback into a single object and passing that around instead.
		/// </summary>
		internal BluetoothGatt _gatt;
		/// <summary>
		/// we also track this because of gogole's weird API. the gatt callback is where
		/// we'll get notified when services are enumerated
		/// </summary>
		private GattCallback _gattCallback;
		internal ProfileState _profileState;

		public Device (BluetoothDevice nativeDevice, BluetoothGatt gatt, 
			GattCallback gattCallback, int rssi) : base ()
		{
			this._nativeDevice = nativeDevice;
			this._gatt = gatt;
			this.GattCallback = gattCallback;
			this._rssi = rssi;
		}

		public override Guid ID {
			get {
				//TODO: verify - fix from Evolve player
				Byte[] deviceGuid = new Byte[16];
				String macWithoutColons = _nativeDevice.Address.Replace (":", "");
				Byte[] macBytes = Enumerable.Range (0, macWithoutColons.Length)
					.Where(x => x % 2 == 0)
					.Select(x => Convert.ToByte(macWithoutColons.Substring(x, 2), 16))
					.ToArray();
				macBytes.CopyTo (deviceGuid, 10);
				return new Guid(deviceGuid);
				//return _nativeDevice.Address;
				//return Guid.Empty;
			}
		}

		public override string Name {
			get {
				return this._nativeDevice.Name;
			}
		}

		public override int Rssi {
			get {
				return this._rssi;
			}
		} internal int _rssi;

		public override object NativeDevice 
		{
			get {
				return this._nativeDevice;
			}
		}

		public override DeviceState State {
			get {
				return this.GetState ();
			}
		}

		//TODO: strongly type IService here
		public override IList<IService> Services
		{
			get { return this._services; }
		} protected IList<IService> _services = new List<IService>();

		#region public methods 

		public override void DiscoverServices ()
		{
			this._gatt.DiscoverServices ();
		}

		public void Disconnect ()
		{
			if (this._gatt != null) {
                this._profileState = ProfileState.Disconnecting;

                var tcs = new TaskCompletionSource<IDevice>();
                EventHandler<DeviceConnectionEventArgs> h = null;
                h = (object sender, DeviceConnectionEventArgs args) =>
                {
                        this.GattCallback.DeviceDisconnected -=h;
                        this._gatt.Close ();
                        this.GattCallback = null;
                        this._gatt = null;
                        tcs.SetResult(this);

                };

                this.GattCallback.DeviceDisconnected += h;
                this._gatt.Disconnect ();

                // Observation if one calls Bluetooth.Close() immediately after BluetoothGatt.Disconnect()
                // we will encounter a race condition. So we have to wait for disconnect to propogate before
                // calling close. 
                Debug.WriteLine("about to wait for disconnect handlers before closing the device....");
                tcs.Task.Wait();
                Debug.WriteLine("exit waiting for disconnect handlers....");
			}
		}

		#endregion

		#region internal methods

		protected DeviceState GetState()
		{
			switch (this._profileState) {
			case ProfileState.Connected:
				return DeviceState.Connected;
			case ProfileState.Connecting:
				return DeviceState.Connecting;
			case ProfileState.Disconnected:
			default:
				return DeviceState.Disconnected;
			}
		}

		internal GattCallback GattCallback
		{
			get
			{
				return this._gattCallback;
			}

			set
			{
				this._gattCallback = value;
				// when the services are discovered on the gatt callback, cache them here
				if (this._gattCallback != null) {
					this._gattCallback.ServicesDiscovered += (s, e) => {
						var services = this._gatt.Services;
						this._services = new List<IService> ();
						foreach (var item in services) {
							this._services.Add (new Service (item, this));
						}
						this.ServicesDiscovered (this, e);
					};
				}
			}
		}
		#endregion
	}
}

