using System;
using System.Collections.Generic;
using System.Diagnostics;
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
            get { return isScanning;  }
        } protected bool isScanning;

        public IList<IDevice> DiscoveredDevices
        {
            get { return discoveredDevices; }
        } protected IList<IDevice> discoveredDevices = new List<IDevice>(); 

        public IList<IDevice> ConnectedDevices
        {
            get { return connectedDevices; }
        } protected IList<IDevice> connectedDevices = new List<IDevice>();

        public void StartScanningForDevices()
        {
            StartScanningForDevices(serviceUuid: Guid.Empty);
        }

        public async void StartScanningForDevices(Guid serviceUuid)
        {
            if(isScanning == true)
                return;

            discoveredDevices = new List<IDevice>();

            Debug.WriteLine ("Adapter: Starting a scan for devices.");

            //clear the list
            discoveredDevices = new List<IDevice>();

            isScanning = true;

            foreach (DeviceInformation di in await DeviceInformation.FindAllAsync(BluetoothLEDevice.GetDeviceSelector()))
            {
                BluetoothLEDevice bleDevice = await BluetoothLEDevice.FromIdAsync(di.Id);

                if (!DeviceExistsInDiscoveredList(bleDevice))
                {
                    var d = new Device(bleDevice);
        
                    discoveredDevices.Add(d);
                    DeviceDiscovered(this, new DeviceDiscoveredEventArgs() {Device = d});
                }

                if (isScanning == false)
                    break;
            }

            isScanning = false;
        }

        protected bool DeviceExistsInDiscoveredList(BluetoothLEDevice device)
        {
            foreach (var d in discoveredDevices)
            {
                if (device.BluetoothAddress == ((BluetoothLEDevice) d.NativeDevice).BluetoothAddress)
                    return true;
            }
            return false;
        }

        public void StopScanningForDevices()
        {
            this.isScanning = false;
        }

        public void ConnectToDevice(IDevice device)
        {
            //TODO ConectToDevice
            this.connectedDevices.Add(device);
            DeviceConnected(this, new DeviceConnectionEventArgs() {Device = device, ErrorMessage = "error"});
        }

        public void DisconnectDevice(IDevice device)
        {
            //TODO DisconnetDevice
            this.connectedDevices.Remove(device);
        }

    }
}
