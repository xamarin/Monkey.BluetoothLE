using System;
using Android.Bluetooth;
using System.Diagnostics;

namespace Robotics.Mobile.Core.Bluetooth.LE
{
	public class GattCallback : BluetoothGattCallback
	{
		public event EventHandler<DeviceConnectionEventArgs> DeviceConnected = delegate {};
		public event EventHandler<DeviceConnectionEventArgs> DeviceDisconnected = delegate {};
		public event EventHandler<ServicesDiscoveredEventArgs> ServicesDiscovered = delegate {};
		public event EventHandler<CharacteristicReadEventArgs> CharacteristicValueUpdated = delegate {};

		protected Adapter _adapter;

		public GattCallback (Adapter adapter)
		{
			_adapter = adapter;
		}

		public override void OnConnectionStateChange (BluetoothGatt gatt, GattStatus status, ProfileState newState)
		{
			Console.WriteLine ("OnConnectionStateChange: ");

			//TODO: need to pull the cached RSSI in here, or read it (requires the callback)
            var device = _adapter.DiscoveredDevices.Find(gatt.Device);
            if (device == null)
            {
                Debug.WriteLine(string.Format("Device {0} has not been discovered yet.", gatt.Device.Name));
                device = new Device(gatt.Device, gatt, this, 0);
                _adapter.DiscoveredDevices.Add(device);
            }

            switch (newState)
            {
                // disconnected
                case ProfileState.Disconnected:
                    Console.WriteLine("Disconnected.");
                    device.Disconnect();
                    OnDeviceDisconnected(device);
                    break;
                // connecting
                case ProfileState.Connecting:
                    Console.WriteLine("Connecting");
                    device.SetState(DeviceState.Connecting);
                    break;
                // connected
                case ProfileState.Connected:
                    Console.WriteLine("Connected");
                    device.SetState(DeviceState.Connected);
                    OnDeviceConnected(device);
                    break;
                // disconnecting
                case ProfileState.Disconnecting:
                    Console.WriteLine("Disconnecting");
                    device.SetState(DeviceState.Disconnected);
                    break;
            }
		}

        public void OnDeviceConnected(IDevice device)
        {
            this.DeviceConnected(this, new DeviceConnectionEventArgs() { Device = device });
        }

        public void OnDeviceDisconnected(IDevice device)
        {
            this.DeviceDisconnected(this, new DeviceConnectionEventArgs() { Device = device });
        }

		public override void OnServicesDiscovered (BluetoothGatt gatt, GattStatus status)
		{
			base.OnServicesDiscovered (gatt, status);

			Console.WriteLine ("OnServicesDiscovered: " + status.ToString ());

			this.ServicesDiscovered (this, new ServicesDiscoveredEventArgs ());
		}

		public override void OnDescriptorRead (BluetoothGatt gatt, BluetoothGattDescriptor descriptor, GattStatus status)
		{
			base.OnDescriptorRead (gatt, descriptor, status);

			Console.WriteLine ("OnDescriptorRead: " + descriptor.ToString());
		}

		public override void OnCharacteristicRead (BluetoothGatt gatt, BluetoothGattCharacteristic characteristic, GattStatus status)
		{
			base.OnCharacteristicRead (gatt, characteristic, status);

			Console.WriteLine ("OnCharacteristicRead: " + characteristic.GetStringValue (0));

			this.CharacteristicValueUpdated (this, new CharacteristicReadEventArgs () { 
				Characteristic = new Characteristic (characteristic, gatt, this) }
			);
		}

		public override void OnCharacteristicChanged (BluetoothGatt gatt, BluetoothGattCharacteristic characteristic)
		{
			base.OnCharacteristicChanged (gatt, characteristic);

			Console.WriteLine ("OnCharacteristicChanged: " + characteristic.GetStringValue (0));

			this.CharacteristicValueUpdated (this, new CharacteristicReadEventArgs () { 
				Characteristic = new Characteristic (characteristic, gatt, this) }
			);
		}
	}
}
