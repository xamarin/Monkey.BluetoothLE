using System;
using System.Collections.Generic;
using Android.Bluetooth;
using System.Linq;
using Android.Content;
using System.Threading.Tasks;

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
		protected BluetoothGatt _gatt;
		/// <summary>
		/// we also track this because of gogole's weird API. the gatt callback is where
		/// we'll get notified when services are enumerated
		/// </summary>
        private GattCallback _gattCallback;

		internal Device (BluetoothDevice nativeDevice, BluetoothGatt gatt, 
			GattCallback gattCallback, int rssi) : base ()
		{
			this._nativeDevice = nativeDevice;
            _gatt = gatt;
            SetGattCallback(gattCallback);
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

        protected int _rssi;
        public override int Rssi
        {
            get
            {
                return _rssi;
            }
        }

		public override object NativeDevice 
		{
			get {
				return this._nativeDevice;
			}
		}

        private DeviceState _state;

        public override DeviceState State
        {
            get { return _state; }
		}
            
		//TODO: strongly type IService here
		public override IList<IService> Services
		{
			get { return this._services; }
		} protected IList<IService> _services = new List<IService>();

		#region public methods 

		public override void DiscoverServices ()
		{
            if (_gatt != null)
                _gatt.DiscoverServices();
		}

        internal async void Connect(Context context, GattCallback callback)
        {
            if (_gatt != null)
            {
                // connection attempt is already in progress, abort it
                Disconnect();
                await Task.Delay(1500);
            }

            SetGattCallback(callback);
            _gatt = _nativeDevice.ConnectGatt (context, true, callback);
        }

		internal void Disconnect ()
        {
            lock (this)
            {
                if (_gatt != null)
                {
                    _gattCallback.OnDeviceDisconnected(this);
                    _gatt.Disconnect();
                    _gatt.Close();
                    _gatt = null;
                    SetGattCallback(null);
                    _services.Clear();
                }
                SetState(DeviceState.Disconnected);
            }
        }

		#endregion

		#region internal methods

        internal void SetState(DeviceState state)
        {
            _state = state;
        }

        internal void SetGattCallback(GattCallback callback)
        {
            // remove event handler from the old callback
            if (_gattCallback != null)
                _gattCallback.ServicesDiscovered -= gattCallback_ServicesDiscovered;
            _gattCallback = callback; // set the current callback
            // when the services are discovered on the gatt callback, cache them here
            if (_gattCallback != null)
                _gattCallback.ServicesDiscovered += gattCallback_ServicesDiscovered;
        }
            
		#endregion

        void gattCallback_ServicesDiscovered (object sender, ServicesDiscoveredEventArgs e)
        {
            if (_gatt != null)
            {
                var services = this._gatt.Services;
                this._services = new List<IService>();
                foreach (var item in services)
                {
                    this._services.Add(new Service(item, _gatt, _gattCallback));
                }
                this.ServicesDiscovered(this, e);
            }
        }
	}
}
