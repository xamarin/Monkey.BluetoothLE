using System;
using System.Collections.Generic;
using Android.Bluetooth;
using System.Linq;

namespace Robotics.Mobile.Core.Bluetooth.LE
{
	public class Device : DeviceBase
	{
		public override event EventHandler ServicesDiscovered = delegate {};

		protected BluetoothDevice nativeDevice;
		/// <summary>
		/// we have to keep a reference to this because Android's api is weird and requires
		/// the GattServer in order to do nearly anything, including enumerating services
		/// 
		/// TODO: consider wrapping the Gatt and Callback into a single object and passing that around instead.
		/// </summary>
		protected BluetoothGatt gatt;
		/// <summary>
		/// we also track this because of gogole's weird API. the gatt callback is where
		/// we'll get notified when services are enumerated
		/// </summary>
		protected GattCallback gattCallback;

		public Device (BluetoothDevice nativeDevice, BluetoothGatt gatt, 
			GattCallback gattCallback, int rssi) : base ()
		{
			this.nativeDevice = nativeDevice;
			this.gatt = gatt;
			this.gattCallback = gattCallback;
			this.rssi = rssi;

			// when the services are discovered on the gatt callback, cache them here
			if (this.gattCallback != null) {
				this.gattCallback.ServicesDiscovered += (s, e) => {
					var services = this.gatt.Services;
					this.services = new List<IService> ();
					foreach (var item in services) {
						this.services.Add (new Service (item, this.gatt, this.gattCallback));
					}
					this.ServicesDiscovered (this, e);
				};
			}
		}

        public override Guid ID
        {
			get {
				//TODO: verify - fix from Evolve player
				Byte[] deviceGuid = new Byte[16];
				String macWithoutColons = nativeDevice.Address.Replace (":", "");
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

		public override string Name { get { return nativeDevice.Name;	} }

		public override int Rssi { get { return rssi; } }
        protected int rssi;

		public override object NativeDevice { get { return nativeDevice; } }

		// TODO: investigate the validity of this. Android API seems to indicate that the
		// bond state is available, rather than the connected state, which are two different 
		// things. you can be bonded but not connected.
		public override DeviceState State {	get { return GetState (); }	}

		//TODO: strongly type IService here
		public override IList<IService> Services
		{
			get { return services; }
		} protected IList<IService> services = new List<IService>();

		#region public methods 

		public override void DiscoverServices ()
		{
			gatt.DiscoverServices ();
		}

		public void Disconnect ()
		{
			gatt?.Disconnect ();
			gatt?.Dispose ();
		}

		#endregion

		#region internal methods

		protected DeviceState GetState()
		{
			switch (this.nativeDevice.BondState) {
			case Bond.Bonded:
				return DeviceState.Connected;
			case Bond.Bonding:
				return DeviceState.Connecting;
			case Bond.None:
			default:
				return DeviceState.Disconnected;
			}
		}


		#endregion
	}
}

