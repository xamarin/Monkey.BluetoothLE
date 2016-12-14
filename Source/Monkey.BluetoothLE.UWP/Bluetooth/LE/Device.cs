using System;
using System.Collections.Generic;
using System.Diagnostics;
using Windows.Devices.Bluetooth;
using Windows.Devices.Enumeration;

//http://stackoverflow.com/questions/35420940/windows-uwp-connect-to-ble-device-after-discovery

namespace Robotics.Mobile.Core.Bluetooth.LE
{
    public class Device : DeviceBase
    {
        public override event EventHandler ServicesDiscovered = delegate { };

        protected BluetoothLEDevice nativeDevice;

        public Device(BluetoothLEDevice nativeDevice)
        {
            this.nativeDevice = nativeDevice;
         }

        public override Guid ID
        {
            get { return ExtractGuid(nativeDevice.DeviceId); }
        }

        public override string Name
        {
            get { return nativeDevice.Name; }
        }

        public override object NativeDevice
        {
            get { return nativeDevice; }
        }

        public override int Rssi
        {
            //ToDo - not sure if it's possible with the current APIs
            get { return 0; }
        }

        public override DeviceState State
        {
            get { return this.GetState(); }
        }

        Guid ExtractGuid(string id)
        {
            //ToDo get from DeviceInformation class
            return new Guid();
        }

        //old code from WPSL
        Guid ExtractGuidWPSL(string id)
        {
            //there's probably a safer way
            int start = id.IndexOf('{') + 1;

            var guid = id.Substring(start, 36);

            return Guid.Parse(guid);
        }

        protected DeviceState GetState()
        {
            switch (nativeDevice.ConnectionStatus)
            {
                case BluetoothConnectionStatus.Connected:
                    return DeviceState.Connected;
                case BluetoothConnectionStatus.Disconnected:
                default:
#if DEBUG
   //               return DeviceState.Connected;
#endif
                    return DeviceState.Disconnected;
            }
        }

        public override IList<IService> Services
        {
            get { return _services; } 
        } protected IList<IService> _services = new List<IService>();

        public override void DiscoverServices()
        {
            this._services.Clear();
            //find the services
            foreach (var item in nativeDevice.GattServices)
            {
                Debug.WriteLine("Device.Discovered Service: " + item.DeviceId);
                this._services.Add(new Service(item));
            }

            if (ServicesDiscovered != null && this._services.Count > 0)
            {
                ServicesDiscovered(this, new EventArgs());
            }

        }
    }
}
