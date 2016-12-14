Big questions:
*	should we follow more of a facade pattern and move EnumerateServices and EnumerateDescriptors to the
	IAdapter? i think this is how android does it. apple does it on the Device/

XPLAT:
======
BluetoothLE {
	IAdapter
		Properties
			.DiscoveredDevices { get; } // maybe AvailableDevices
			.ConnectedDevices { get; }
			.IsScanning { get; }
		Methods
			.StartScanningForDevices () <- async?
			.StopScanningForDevices ()
			.ConnectToDevice ()
			.DisconnectDevice ()
		Events
			DeviceDiscovered
			DeviceConnected
			DeviceDisconnected

	IDevice
		Events
			ServicesDiscovered
		Property
			int RSSI
			string Name
			IList<IService> Services
			.Id? Address?? UUID is deprecated in iOS7 (use Identifier), doesn't exist in Android
		Methods
			.ToString() <- should return .Name
			.DiscoverServices () <- async? will it know when it's finished? do we want them on at a time?

	IService
		.EnumerateCharacteristics (in android it's a property, but it's actually a method in the underlying framework)
		.IList<ICharacteristic> Characteristics
		FindCharacteristic(KnownCharacteristic)

	ICharacteristic
		Properties
			byte[] Value
			string StringValue
			string Uuid
			CharacteristicPropertyType Properties
		Methods
			.EnumerateDescriptors
			.RequestValue

			.StartUpdates?
			.StopUpdates? // right now we'e starting in RequestValue
	IDescriptor
		Properties
			Guid ID
			UUID
}

ANDROID:
=======
BluetoothAdapter {
	Methods
		StartLeScan()
		StopLeScan()

}
BluetoothGatt {
	Methods
		Disconnect
		Close
}
BluetoothGattService {
	Properties
		.Characteristics
}

APPLE:
======
CentralBleManager {
	Methods
		ScanForPeripherals
		StopScan
}
CBPeripheral
{
	Properties
		.IsConnected
		.RSSI (note, only avail after ReadRssi())
	Methods
		.DiscoverServices
		.DiscoverCharacteristics (Service)
		.DiscoverDescriptors (Characteristic)
		.ReadRssi

}
CBService
{
	Properties
	Methods
}

WINPHONE8.1 (draft - not sure these are all correct)
===========
https://dev.windowsphone.com/en-US/OEM/docs/Driver_Components/Bluetooth
http://www.apeoholic.se/post/2014/05/04/Windows-phone-81-and-Bluetooth-LE-Getting-started
https://github.com/danardelean/BLEExplorer.WP

BluetoothLEDevice
	.Name
	.GattServices
	.GetDeviceSelector()
	.GetGattService()
	.FromIdAsync()
DeviceInformation
	.FindAllAsync
GattService?
	.GetCharacteristics()
Characteristic
	.WriteValueAsync()