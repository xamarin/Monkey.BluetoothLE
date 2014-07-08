using System;
using MonoTouch.UIKit;
using System.Drawing;
using MonoTouch.CoreBluetooth;
using Xamarin.Robotics.Core.Bluetooth.LE;
using System.Linq;

namespace Xamarin.Robotics.BluetoothLEExplorer.iOS
{
	public class CharacteristicDetailScreen : UIViewController
	{
		IDevice _connectedDevice;
		IService _currentService;
		ICharacteristic _characteristic;

		UILabel _characteristicNameText;
		UILabel _characteristicIDText;
		UILabel _rawValueText;
		UILabel _stringValueText;
		UILabel _valueUdpatedDateTime;

		EventHandler<CharacteristicReadEventArgs> _valueUpdatedHandler;

		public CharacteristicDetailScreen ()
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


			_characteristicNameText = new UILabel (new RectangleF (10, 70, 300, 60));
			_characteristicIDText = new UILabel (new RectangleF (10, 100, 300, 60));
			_rawValueText = new UILabel (new RectangleF (10, 130, 300, 60));
			_stringValueText = new UILabel (new RectangleF (10, 160, 300, 60));
			//_valueUdpatedDateTime = new UILabel (new RectangleF (10, 190, 300, 60));


			_rawValueText.Text = "raw";
			_stringValueText.Text = "string";
			//_valueUdpatedDateTime.Text = "-";

			// populate our page
			this._characteristicNameText.Text = _characteristic.Name;
			this._characteristicIDText.Text = _characteristic.ID.ToString ();



			Add (_characteristicNameText);
			Add (_characteristicIDText);
			Add (_rawValueText);
			Add (_stringValueText);
			//Add (_valueUdpatedDateTime);

			// NOTIFY-UPDATE
			this._valueUpdatedHandler = (s, e) => {
				Console.WriteLine("-- _valueUpdatedHandler: " +  e.Characteristic.Value);
				this.InvokeOnMainThread( () => {
					this.PopulateValueInfo();
				});
			};


			// request the value to be read
			_characteristic.StartUpdates();

			// READ
			var @type = new UILabel (new RectangleF (150, 250, 300, 60));
			var read = UIButton.FromType (UIButtonType.System);
			read.Frame = new RectangleF (10, 250, 50, 30);
			read.SetTitle ("Read", UIControlState.Normal);
			read.TouchUpInside += async (sender, e) => {
				@type.Text = "before";
				var c = await _characteristic.ReadAsync();
				@type.Text = "value:" + c.StringValue;
			};
			if (_characteristic.CanRead) {
				Add (@type);
				Add (read);
			}

//			// WRITE
//			var write = UIButton.FromType (UIButtonType.System);
//			write.Frame = new RectangleF (10, 300, 100, 30);
//			write.SetTitle ("Write On", UIControlState.Normal);
//			write.TouchUpInside += (sender, e) => {
//				_characteristic.Write(new byte[] {0x01});
//			};
//
//			var writeoff = UIButton.FromType (UIButtonType.System);
//			writeoff.Frame = new RectangleF (120, 300, 100, 30);
//			writeoff.SetTitle ("Write Off", UIControlState.Normal);
//			writeoff.TouchUpInside += (sender, e) => {
//				_characteristic.Write(new byte[] {0x00});
//			};
//			if (_characteristic.CanWrite) {
//				Add (write);
//				Add (writeoff);
//			}
		}


		public override void ViewWillAppear (bool animated)
		{
			base.ViewWillAppear (animated);
			_characteristic.ValueUpdated += _valueUpdatedHandler;
		}

		public override void ViewWillDisappear (bool animated)
		{
			base.ViewWillDisappear (animated);
			_characteristic.ValueUpdated -= _valueUpdatedHandler;
		}

		//TODO: check if i should be passing in the value from _valueUpdatedHandler, sometimes nulls get thru this null check!?
		protected void PopulateValueInfo()
		{
			if (_characteristic.Value == null) {
				this._rawValueText.Text = this._stringValueText.Text = "Waiting for update...";
			} else {
				var s = (from i in _characteristic.Value
					select i.ToString ("X").PadRight(2, '0')).ToArray ();

				this._rawValueText.Text = string.Join (":", s); // formatted as hex 
				this._stringValueText.Text = _characteristic.StringValue;

//
//
//				//
//				// TI SensorTag hardcoded read/write
//				//
//				// http://processors.wiki.ti.com/index.php/SensorTag_User_Guide
//				//
//				Console.WriteLine ("Populate switch for : " + _characteristic.ID.PartialFromUuid ());
//				var sensorData = _characteristic.Value;
//				if (_characteristic.ID.PartialFromUuid () == "0xaa01") {
//					// Temperature
//					var ambientTemperature = BitConverter.ToUInt16 (sensorData, 2) / 128.0;
//
//					// http://sensortag.codeplex.com/SourceControl/latest#SensorTagLibrary/SensorTagLibrary/Source/Sensors/IRTemperatureSensor.cs
//					double Vobj2 = BitConverter.ToInt16 (sensorData, 0);
//					Vobj2 *= 0.00000015625;
//
//					double Tdie = ambientTemperature + 273.15;
//
//					double S0 = 5.593E-14;
//					double a1 = 1.75E-3;
//					double a2 = -1.678E-5;
//					double b0 = -2.94E-5;
//					double b1 = -5.7E-7;
//					double b2 = 4.63E-9;
//					double c2 = 13.4;
//					double Tref = 298.15;
//					double S = S0 * (1 + a1 * (Tdie - Tref) + a2 * Math.Pow ((Tdie - Tref), 2));
//					double Vos = b0 + b1 * (Tdie - Tref) + b2 * Math.Pow ((Tdie - Tref), 2);
//					double fObj = (Vobj2 - Vos) + c2 * Math.Pow ((Vobj2 - Vos), 2);
//					double tObj = Math.Pow (Math.Pow (Tdie, 4) + (fObj / S), .25);
//
//					tObj -= 273.15;
//
//					this._stringValueText.Text = "ambient: " + ambientTemperature + "\ntarget: " + tObj + " C";
//				} else if (_characteristic.ID.PartialFromUuid () == "0xaa11") {
//					// Accelerometer
//					int x = sensorData [0];
//					int y = sensorData [1];
//					int z = sensorData [2];
//
//					double scaledX = x / 64.0;
//					double scaledY = y / 64.0;
//					double scaledZ = z / 64.0; //  * -1; ?????
//
//					this._stringValueText.Text = String.Format ("scaled: {0}, {1}, {2} xyz", scaledX, scaledY, scaledZ);
//				} else if (_characteristic.ID.PartialFromUuid () == "0xaa21") {
//					// Humidity
//					int a = BitConverter.ToUInt16 (sensorData, 2);
//					a = a - (a % 4);
//					double humidity = (-6f) + 125f * (a / 65535f);
//					this._stringValueText.Text = "humidity: " + humidity + "%";
//				} else if (_characteristic.ID.PartialFromUuid () == "0xaa31") {
//					// Magnometer
//					double x = BitConverter.ToInt16 (sensorData, 0) * (2000f / 65536f);
//					double y = BitConverter.ToInt16 (sensorData, 0) * (2000f / 65536f);
//					double z = BitConverter.ToInt16 (sensorData, 0) * (2000f / 65536f);
//
//					this._stringValueText.Text = String.Format ("heading: {0}, {1}, {2} xyz", x,y,z);
//					// TODO: http://cache.freescale.com/files/sensors/doc/app_note/AN4248.pdf?fpsp=1
//				} else if (_characteristic.ID.PartialFromUuid () == "0xaa41") {
//					// Barometer
//
//				} else if (_characteristic.ID.PartialFromUuid () == "0xaa51") {
//					// Gyroscope
//
//				} else if (_characteristic.ID.PartialFromUuid () == "0xffe1") {
//					// Smart Keys: Bit 2 - side key, Bit 1 - right key, Bit 0 – left key 
//					var b = ((int)_characteristic.Value[0]) % 4;
//					switch (this._rawValueText.Text) {
//					case "10":
//						this._stringValueText.Text = "Right button";
//						break;
//					case "20":
//						this._stringValueText.Text = "Left button";
//						break;
//					case "30":
//						this._stringValueText.Text = "Both buttons";
//						break;
//					default:
//						this._stringValueText.Text = "Neither button";
//						break;
//					}
//					this._stringValueText.Text += " " + b;
//				}
			} 
			//TODO: this._valueUpdatedDateTime
		}
	}
}

