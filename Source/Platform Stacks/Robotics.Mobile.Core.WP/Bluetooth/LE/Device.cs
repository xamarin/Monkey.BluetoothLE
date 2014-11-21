using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth;
using Windows.Devices.Enumeration;

namespace Robotics.Mobile.Core.Bluetooth.LE
{
    public class Device : DeviceBase
    {
        public override event EventHandler ServicesDiscovered = delegate { };

        protected BluetoothLEDevice _nativeDevice;

        public Device(BluetoothLEDevice nativeDevice)
        {
            this._nativeDevice = nativeDevice;
        }

        public override Guid ID
        {
            get { return ExtractGuid(_nativeDevice.DeviceId); }
        }

        public override string Name
        {
            get { return _nativeDevice.Name; }
        }

        public override object NativeDevice
        {
            get { return _nativeDevice; }
        }

        public override int Rssi
        {
            //throw new NotImplementedException();
            get { return 0; }
        }

        public override DeviceState State
        {
            get { return this.GetState(); }
        }

        Guid ExtractGuid(string id)
        {
            //there's probably a safer way
            int start = id.IndexOf('{') + 1;

            var guid = id.Substring(start, 36);

            return Guid.Parse(guid);

        }

        protected DeviceState GetState()
        {
            switch (_nativeDevice.ConnectionStatus)
            {
                case BluetoothConnectionStatus.Connected:
                    return DeviceState.Connected;
                case BluetoothConnectionStatus.Disconnected:
                default:
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
            foreach (var item in _nativeDevice.GattServices)
            {
                Debug.WriteLine("Device.Discovered Service: " + item.DeviceId);
                this._services.Add(new Service(item));
            }

            if (this.ServicesDiscovered != null && this._services.Count > 0)
            {
                this.ServicesDiscovered(this, new EventArgs());
            }

        }
    }
}
