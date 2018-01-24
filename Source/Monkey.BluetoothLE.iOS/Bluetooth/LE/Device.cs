using System;
using CoreBluetooth;
using System.Collections.Generic;
using Foundation;

namespace Robotics.Mobile.Core.Bluetooth.LE
{
	public class Device : DeviceBase
	{
		public override event EventHandler ServicesDiscovered = delegate {};

        public override string Name => nativeDevice.Name;

        public override int Rssi => rssi;
        protected int rssi;

        public override object NativeDevice => nativeDevice;

        // TODO: investigate the validity of this. Android API seems to indicate that the
        // bond state is available, rather than the connected state, which are two different 
        // things. you can be bonded but not connected.
        public override DeviceState State => GetState();

        public override IList<IService> Services
        {
            get { return this.services; }
        }
        protected IList<IService> services = new List<IService>();

        protected CBPeripheral nativeDevice;

		public Device (CBPeripheral nativeDevice)
		{
			this.nativeDevice = nativeDevice;

			this.nativeDevice.DiscoveredService += (object sender, NSErrorEventArgs e) => {
				// why we have to do this check is beyond me. if a service has been discovered, the collection
				// shouldn't be null, but sometimes it is. le sigh, apple.
				if (this.nativeDevice.Services != null)
                {
					foreach (CBService s in this.nativeDevice.Services)
                    {
						Console.WriteLine ("Device.Discovered Service: " + s.Description);

                        if (!ServiceExists(s))
							services.Add (new Service(s, this.nativeDevice));
					}
					ServicesDiscovered(this, new EventArgs());
				}
			};

            this.nativeDevice.DiscoveredCharacteristic += NativeDeviceDiscoveredCharacteristic;
   		}

        public override Guid ID => Guid.ParseExact(nativeDevice.Identifier.AsString(), "d");
 			//TODO: not sure if this is right. hell, not even sure if a 
			// device should have a UDDI. iOS BLE peripherals do, though.
			// need to look at the BLE Spec
			// Actually.... deprecated in iOS7!
			// Actually again, Uuid is, but Identifier isn't.
			//return _nativeDevice.Identifier.AsString ();//.ToString();
		

		#region public methods 

		public override void DiscoverServices ()
		{
			this.nativeDevice.DiscoverServices();
		}

		public void Disconnect ()
		{
			Adapter.Current.DisconnectDevice (this);
			this.nativeDevice.Dispose ();
		}

		#endregion

		#region internal methods

		protected DeviceState GetState()
		{
			switch (this.nativeDevice.State) {
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
			foreach (var s in this.services) {
				if (s.ID == Service.ServiceUuidToGuid(service.UUID))
					return true;
			}
			return false;
		}
       
        void NativeDeviceDiscoveredCharacteristic(object sender, CBServiceEventArgs e)
        {
            Console.WriteLine("Device.Discovered Characteristics.");
            //loop through each service, and update the characteristics
            foreach (CBService srv in ((CBPeripheral)sender).Services)
            {
                // if the service has characteristics yet
                if (srv.Characteristics != null)
                {
                    // locate the our new service
                    foreach (var item in this.Services)
                    {
                        // if we found the service
                        if (item.ID == Service.ServiceUuidToGuid(srv.UUID))
                        {
                            item.Characteristics.Clear();

                            // add the discovered characteristics to the particular service
                            foreach (var characteristic in srv.Characteristics)
                            {
                                Console.WriteLine("Characteristic: " + characteristic.Description);
                                Characteristic newChar = new Characteristic(characteristic, this.nativeDevice);
                                item.Characteristics.Add(newChar);
                            }

                            // inform the service that the characteristics have been discovered
                            // TODO: really, we should just be using a notifying collection.
                            (item as Service).OnCharacteristicsDiscovered();
                        }
                    }
                }
            }
        }

        #endregion
    }
}