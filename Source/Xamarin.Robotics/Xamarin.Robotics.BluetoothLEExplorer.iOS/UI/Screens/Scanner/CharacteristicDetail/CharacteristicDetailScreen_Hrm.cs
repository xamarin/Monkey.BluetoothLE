using System;
using MonoTouch.UIKit;
using System.Drawing;
using MonoTouch.CoreBluetooth;
using Xamarin.Robotics.Core.Bluetooth.LE;

namespace Xamarin.Robotics.BluetoothLEExplorer.iOS
{
	public class CharacteristicDetailScreen_Hrm : UIViewController
	{
		protected IDevice _connectedDevice;
		protected IService _currentService;
		protected ICharacteristic _characteristic;

		public CharacteristicDetailScreen_Hrm ()
		{
		}

		public void SetDeviceServiceAndCharacteristic (IDevice device, IService service, ICharacteristic characteristic)
		{
			this._connectedDevice = device;
			this._currentService = service;
			this._characteristic = characteristic;
		}

		public override void ViewDidLoad ()
		{
			base.ViewDidLoad ();
			Title = "CharacteristicDetail";
			View.BackgroundColor = UIColor.White;

			var label = new UILabel (new RectangleF (10, 100, 300, 60));
			label.Text = "Characteristic: " + _connectedDevice.Name + "\n" + _currentService.Name + "\n" + _characteristic.Name;
			label.Lines = 3;
			Add (label);


			var labelHR = new UILabel (new RectangleF (50, 200, 300, 100));
			labelHR.Font = UIFont.BoldSystemFontOfSize (48);
			labelHR.TextColor = UIColor.Red;
			labelHR.Text = "- bpm";
			labelHR.Lines = 3;
			Add (labelHR);


			// request the value to be read
			_characteristic.StartUpdates();

			((CBPeripheral)_connectedDevice.NativeDevice).UpdatedCharacterteristicValue += (object sender, CBCharacteristicEventArgs e) => {
				Console.WriteLine("UpdatedCharacterteristicValue:" + e.Characteristic.Description + " " + e.Characteristic.Value);

				if (e.Characteristic.Value != null) {
					var data = e.Characteristic.Value;
					byte[] dataBytes = new byte[data.Length];
					System.Runtime.InteropServices.Marshal.Copy(data.Bytes, dataBytes, 0, Convert.ToInt32(data.Length));

					if (dataBytes.Length == 1)
					{
						// position
						var position = dataBytes[0];
						var locationString = "-";
						Console.WriteLine("----------------------position:" + position);
						// https://developer.apple.com/library/mac/samplecode/HeartRateMonitor/Listings/HeartRateMonitor_HeartRateMonitorAppDelegate_m.html
						switch (position) {
						case 0:
							locationString = @"Other";
							break;
						case 1:
							locationString = @"Chest";
							break;
						case 2:
							locationString = @"Wrist";
							break;
						case 3:
							locationString = @"Finger";
							break;
						case 4:
							locationString = @"Hand";
							break;
						case 5:
							locationString = @"Ear Lobe";
							break;
						case 6: 
							locationString = @"Foot";
							break;
						default:
							locationString = @"Reserved";
							break;
						}
						labelHR.Text = "on " + locationString;
					} else {
						// heartrate
						int bpm = 0;
						//http://www.raywenderlich.com/52080/introduction-core-bluetooth-building-heart-rate-monitor
						//https://developer.bluetooth.org/gatt/characteristics/Pages/CharacteristicViewer.aspx?u=org.bluetooth.characteristic.heart_rate_measurement.xml
						if ( (dataBytes[0] & 0x01) == 0) {
							bpm = (int)dataBytes[1];
						} else {
							bpm = (int)dataBytes[1]; //HACK: wrong
						}
						Console.WriteLine("--------------------------hr:" + bpm);

						InvokeOnMainThread(() =>{
							labelHR.Text = bpm + " bpm";
						});
					}
				}
			};


			Console.WriteLine("ReadValue");
			((CBPeripheral)_connectedDevice.NativeDevice).ReadValue ((CBCharacteristic)_characteristic.NativeCharacteristic);

			Console.WriteLine("SetNotifyValue");
			((CBPeripheral)_connectedDevice.NativeDevice).SetNotifyValue(true, ((CBCharacteristic)_characteristic.NativeCharacteristic));
		}
	}
}

