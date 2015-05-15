using System;
using Android.Bluetooth;

namespace Robotics.Mobile.Core.Bluetooth.LE
{
	/// <summary>
	/// GattCallback has a 1 to 1 relation to each Device instance.
	/// </summary>
	public class GattCallback : BluetoothGattCallback
	{

		public event EventHandler<DeviceConnectionEventArgs> DeviceConnected = delegate {};
		public event EventHandler<DeviceConnectionEventArgs> DeviceDisconnected = delegate {};
		public event EventHandler<ServicesDiscoveredEventArgs> ServicesDiscovered = delegate {};
		public event EventHandler<CharacteristicReadEventArgs> CharacteristicValueUpdated = delegate {};

		private Device _device;

		public GattCallback(Device device)
		{
			this._device = device;
		}

		public override void OnConnectionStateChange (BluetoothGatt gatt, GattStatus status, ProfileState newState)
		{
			base.OnConnectionStateChange (gatt, status, newState);

			Console.WriteLine ("OnConnectionStateChange: ");

			this._device._profileState = newState;
			switch (newState) {
			// disconnected
			case ProfileState.Disconnected:
				Console.WriteLine ("disconnected");
				this.DeviceDisconnected (this, new DeviceConnectionEventArgs () { Device = this._device });
				break;
				// connecting
			case ProfileState.Connecting:
				Console.WriteLine ("Connecting");
				break;
				// connected
			case ProfileState.Connected:
				Console.WriteLine ("Connected");
				this.DeviceConnected (this, new DeviceConnectionEventArgs () { Device = this._device });
				break;
				// disconnecting
			case ProfileState.Disconnecting:
				Console.WriteLine ("Disconnecting");
				break;
			}
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

		public override void OnReadRemoteRssi (BluetoothGatt gatt, int rssi, GattStatus status)
		{
			base.OnReadRemoteRssi (gatt, rssi, status);
			this._device._rssi = rssi;
		}
	}
}

