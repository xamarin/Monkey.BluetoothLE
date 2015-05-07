using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth;
using Windows.Devices.Enumeration;

namespace Robotics.Mobile.Core.Bluetooth.LE
{
    public class Adapter : IAdapter
    {
        //events
        public event EventHandler<DeviceDiscoveredEventArgs> DeviceDiscovered;
        public event EventHandler<DeviceConnectionEventArgs> DeviceConnected;
        public event EventHandler<DeviceConnectionEventArgs> DeviceDisconnected;
        public event EventHandler ScanTimeoutElapsed;

        //class members
        
        public bool IsScanning
        {
            get { return this._isScanning;  }
        } protected bool _isScanning;

        public IList<IDevice> DiscoveredDevices
        {
            get { return this._discoveredDevices; }
        } protected IList<IDevice> _discoveredDevices = new List<IDevice>(); 

        public IList<IDevice> ConnectedDevices
        {
            get { return this._connectedDevices; }
        } protected IList<IDevice> _connectedDevices = new List<IDevice>();

        public void StartScanningForDevices()
        {
            StartScanningForDevices(serviceUuid: Guid.Empty);
        }

        public async void StartScanningForDevices(Guid serviceUuid)
        {
            if(this._isScanning == true)
                return;

            this._discoveredDevices = new List<IDevice>();

            Console.WriteLine ("Adapter: Starting a scan for devices.");

            //clear the list
            this._discoveredDevices = new List<IDevice>();

            this._isScanning = true;

            foreach (DeviceInformation di in await DeviceInformation.FindAllAsync(BluetoothLEDevice.GetDeviceSelector()))
            {
                BluetoothLEDevice bleDevice = await BluetoothLEDevice.FromIdAsync(di.Id);

                if (!DeviceExistsInDiscoveredList(bleDevice))
                {
                    var d = new Device(bleDevice);
                    this._discoveredDevices.Add(d);
                    this.DeviceDiscovered(this, new DeviceDiscoveredEventArgs() {Device = d});
                }

                if (_isScanning == false)
                    break;
            }

            this._isScanning = false;
        }

        protected bool DeviceExistsInDiscoveredList(BluetoothLEDevice device)
        {
            foreach (var d in _discoveredDevices)
            {
                if (device.BluetoothAddress == ((BluetoothLEDevice) d.NativeDevice).BluetoothAddress)
                    return true;
            }
            return false;
        }

        public void StopScanningForDevices()
        {
            this._isScanning = false;
        }

        public void ConnectToDevice(IDevice device)
        {
            //TODO ConectToDevice
            this._connectedDevices.Add(device);
            DeviceConnected(this, new DeviceConnectionEventArgs() {Device = device, ErrorMessage = "error"});
        }

        public void DisconnectDevice(IDevice device)
        {
            //TODO DisconnetDevice
            this._connectedDevices.Remove(device);
        }

    }
}
