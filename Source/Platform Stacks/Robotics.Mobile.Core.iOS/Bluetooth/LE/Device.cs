﻿using System;
using System.Collections.Generic;

#if __UNIFIED__
using CoreBluetooth;
using Foundation;
#else
using MonoTouch.CoreBluetooth;
using MonoTouch.Foundation;
#endif

namespace Robotics.Mobile.Core.Bluetooth.LE
{
	public class Device : DeviceBase
	{
		public override event EventHandler ServicesDiscovered = delegate {};

		protected CBPeripheral _nativeDevice;
	

		public Device (CBPeripheral nativeDevice, NSString localName = null)
		{
			this._nativeDevice = nativeDevice;

			this._nativeDevice.DiscoveredService += (object sender, NSErrorEventArgs e) => {
				// why we have to do this check is beyond me. if a service has been discovered, the collection
				// shouldn't be null, but sometimes it is. le sigh, apple.
				if (this._nativeDevice.Services != null) {
					foreach (CBService s in this._nativeDevice.Services) {
						Console.WriteLine ("Device.Discovered Service: " + s.Description);
						if(!ServiceExists(s)) {
							this._services.Add (new Service(s, this._nativeDevice));
						}
					}
					this.ServicesDiscovered(this, new EventArgs());
				}
			};

			#if __UNIFIED__
			// fixed for Unified https://bugzilla.xamarin.com/show_bug.cgi?id=14893
			this._nativeDevice.DiscoveredCharacteristic += (object sender, CBServiceEventArgs e) => {
			#else
			//BUGBUG/TODO: this event is misnamed in our SDK
			this._nativeDevice.DiscoverCharacteristic += (object sender, CBServiceEventArgs e) => {
			#endif
				Console.WriteLine ("Device.Discovered Characteristics.");
				//loop through each service, and update the characteristics
				foreach (CBService srv in ((CBPeripheral)sender).Services) {
					// if the service has characteristics yet
					if(srv.Characteristics != null) {

						// locate the our new service
						foreach (var item in this.Services) {
							// if we found the service
							if (item.ID == Service.ServiceUuidToGuid(srv.UUID) ) {
								item.Characteristics.Clear();

								// add the discovered characteristics to the particular service
								foreach (var characteristic in srv.Characteristics) {
									Console.WriteLine("Characteristic: " + characteristic.Description);
									Characteristic newChar = new Characteristic(characteristic, _nativeDevice);
									item.Characteristics.Add(newChar);
								}
								// inform the service that the characteristics have been discovered
								// TODO: really, we shoul just be using a notifying collection.
								(item as Service).OnCharacteristicsDiscovered();
							}
						}
					}
				}			
			};

			_name = localName == null ? _nativeDevice.Name : localName.ToString();


		}

		public override Guid ID {
			get {
				//TODO: not sure if this is right. hell, not even sure if a 
				// device should have a UDDI. iOS BLE peripherals do, though.
				// need to look at the BLE Spec
				// Actually.... deprecated in iOS7!
				// Actually again, Uuid is, but Identifier isn't.
				//return _nativeDevice.Identifier.AsString ();//.ToString();
				return Guid.ParseExact(_nativeDevice.Identifier.AsString (), "d");
			}
		}

		string _name;
		public override string Name {
			get {
				return _name;
			}
		}


		public override int Rssi {
			get {
				return this._rssi;
			}
		} protected int _rssi;

		public override object NativeDevice 
		{
			get {
				return this._nativeDevice;
			}
		}

		// TODO: investigate the validity of this. Android API seems to indicate that the
		// bond state is available, rather than the connected state, which are two different 
		// things. you can be bonded but not connected.
		public override DeviceState State {
			get { return this.GetState (); }
		}

		public override IList<IService> Services
		{
			get { return this._services; }
		} protected IList<IService> _services = new List<IService>();

		#region public methods 

		public override void DiscoverServices ()
		{
			this._nativeDevice.DiscoverServices();
		}

		public void Disconnect ()
		{
			Adapter.Current.DisconnectDevice (this);
			this._nativeDevice.Dispose ();
		}

		#endregion

		#region internal methods

		protected DeviceState GetState()
		{
			switch (this._nativeDevice.State) {
			case CBPeripheralState.Connected:
				return DeviceState.Connected;
			case CBPeripheralState.Connecting:
				return DeviceState.Connecting;
			case CBPeripheralState.Disconnected:
				return DeviceState.Disconnected;
			default:
				return DeviceState.Disconnected;
			}
		}

		protected bool ServiceExists(CBService service)
		{
			foreach (var s in this._services) {
				if (s.ID == Service.ServiceUuidToGuid(service.UUID))
					return true;
			}
			return false;
		}
		#endregion

	}
}

