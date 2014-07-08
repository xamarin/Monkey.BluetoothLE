using System;
using MonoTouch.UIKit;
using System.Drawing;
using MonoTouch.CoreBluetooth;
using Xamarin.Robotics.Core.Bluetooth.LE;
using System.Linq;

namespace Xamarin.Robotics.BluetoothLEExplorer.iOS
{
	public class CharacteristicDetailScreen_TISensorTag : UIViewController
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

		double gyro_calX, gyro_calY, gyro_calZ;
		double magno_calX, magno_calY, magno_calZ;
		bool gyro_calibrated = false, magno_calibrated = false;

		public CharacteristicDetailScreen_TISensorTag ()
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
			_stringValueText = new UILabel (new RectangleF (10, 160, 300, 120));
			_stringValueText.Lines = 2;
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

			// WRITE
			var write = UIButton.FromType (UIButtonType.System);
			write.Frame = new RectangleF (10, 300, 100, 30);
			write.SetTitle ("Write On", UIControlState.Normal);
			write.TouchUpInside += (sender, e) => {
				Console.WriteLine("turning on: " + _characteristic.ID.PartialFromUuid ());
				if (_characteristic.ID.PartialFromUuid () == "0xaa52") // gyroscope on/off
					_characteristic.Write(new byte[] {0x07}); // enable XYZ axes 
				else if (_characteristic.ID.PartialFromUuid () == "0xaa23") // humidity period
					_characteristic.Write(new byte[] {0x02}); // 
				else
					_characteristic.Write(new byte[] {0x01});
			};

			var writeoff = UIButton.FromType (UIButtonType.System);
			writeoff.Frame = new RectangleF (120, 300, 100, 30);
			writeoff.SetTitle ("Write Off", UIControlState.Normal);
			writeoff.TouchUpInside += (sender, e) => {
				_characteristic.Write(new byte[] {0x00});
			};
			if (_characteristic.CanWrite) {
				Add (write);
				Add (writeoff);
			}
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
				// TI SensorTag hardcoded read/write
				//
				// http://processors.wiki.ti.com/index.php/SensorTag_User_Guide
				//
				Console.WriteLine ("PopulateValueInfo switch for : " + _characteristic.ID.PartialFromUuid ());
				var sensorData = _characteristic.Value;
				if (_characteristic.ID.PartialFromUuid () == "0xaa01") {
					// Temperature sensorTMP006 - works
					var ambientTemperature = BitConverter.ToUInt16 (sensorData, 2) / 128.0;
					double Tdie = ambientTemperature + 273.15;


					// http://sensortag.codeplex.com/SourceControl/latest#SensorTagLibrary/SensorTagLibrary/Source/Sensors/IRTemperatureSensor.cs
					double Vobj2 = BitConverter.ToInt16 (sensorData, 0);
					Vobj2 *= 0.00000015625;

					double S0 = 5.593E-14;
					double a1 = 1.75E-3;
					double a2 = -1.678E-5;
					double b0 = -2.94E-5;
					double b1 = -5.7E-7;
					double b2 = 4.63E-9;
					double c2 = 13.4;
					double Tref = 298.15;
					double S = S0 * (1 + a1 * (Tdie - Tref) + a2 * Math.Pow ((Tdie - Tref), 2));
					double Vos = b0 + b1 * (Tdie - Tref) + b2 * Math.Pow ((Tdie - Tref), 2);
					double fObj = (Vobj2 - Vos) + c2 * Math.Pow ((Vobj2 - Vos), 2);
					double tObj = Math.Pow (Math.Pow (Tdie, 4) + (fObj / S), .25);

					tObj -= 273.15;

					this._stringValueText.Text = "ambient: " + Math.Round(ambientTemperature,1) + "\nIR: " + Math.Round(tObj,1) + " C";

				} else if (_characteristic.ID.PartialFromUuid () == "0xaa11") {
					// Accelerometer sensorKXTJ9
					int x = sensorData [0];
					int y = sensorData [1];
					int z = sensorData [2];

//					x = (byte)((x * 0x0202020202 & 0x010884422010) % 1023); 
//					y = (byte)((y * 0x0202020202 & 0x010884422010) % 1023); 
//					z = (byte)((z * 0x0202020202 & 0x010884422010) % 1023); 

					const double KXTJ9_RANGE = 4.0;

					double scaledX = (x * 1.0) / (256.0 / KXTJ9_RANGE);
					double scaledY = (y * 1.0) / (256.0 / KXTJ9_RANGE) * -1; // Orientation of sensor on board means we need to swap Y (multiplying with -1)
					double scaledZ = (z * 1.0) / (256.0 / KXTJ9_RANGE);

					this._stringValueText.Text = String.Format ("scaled: {0}, {1}, {2} xyz", Math.Round(scaledX,2), Math.Round(scaledY,2), Math.Round(scaledZ,2));

				} else if (_characteristic.ID.PartialFromUuid () == "0xaa21") {
					// Humidity sensorSHT21 - works
					int a = BitConverter.ToUInt16 (sensorData, 2);
					a = a - (a % 4);
					double humidity = (-6f) + 125f * (a / 65535f);

//					int t = BitConverter.ToInt16 (sensorData, 0);
					var t = (sensorData[0] & 0xff) | ((sensorData[1] << 8) & 0xff00); // iono what this sensor is returning :-(

					this._stringValueText.Text = "humidity: " + Math.Round(humidity,1) + "%rH\ntemp: " + Math.Round(t / 1000.0, 1) + "C"; // HACK /1000

				} else if (_characteristic.ID.PartialFromUuid () == "0xaa31") {
					// Magnometer sensorMAG3110

					int x1 = BitConverter.ToInt16 (sensorData, 0);
					int y1 = BitConverter.ToInt16 (sensorData, 2);
					int z1 = BitConverter.ToInt16 (sensorData, 4);

					const double MAG3110_RANGE = 2000.0;
					// calculate acceleration, unit G, range -2, +2
					double x = x1 * (MAG3110_RANGE / 65536f) * -1; //Orientation of sensor on board means we need to swap X (multiplying with -1)
					double y = y1 * (MAG3110_RANGE / 65536f) * -1; //Orientation of sensor on board means we need to swap Y (multiplying with -1)
					double z = z1 * (MAG3110_RANGE / 65536f);

					if (!magno_calibrated) {
						magno_calX = x;
						magno_calY = y;
						magno_calZ = z;
						magno_calibrated = true;
					}

					this._stringValueText.Text = String.Format ("heading: {0}, {1}, {2} /nmag uT", 
						Math.Round(x - magno_calX,1),
						Math.Round(y - magno_calY,1),
						Math.Round(z - magno_calZ,1));
					// TODO: http://cache.freescale.com/files/sensors/doc/app_note/AN4248.pdf?fpsp=1

				} else if (_characteristic.ID.PartialFromUuid () == "0xaa41") {
					// Barometer

				} else if (_characteristic.ID.PartialFromUuid () == "0xaa51") {
					// Gyroscope
					int x1 = BitConverter.ToInt16 (sensorData, 0);
					int y1 = BitConverter.ToInt16 (sensorData, 2);
					int z1 = BitConverter.ToInt16 (sensorData, 4);

					const double IMU3000_RANGE = 500.0;

					double x = (x1 * 1.0) / (65536 / IMU3000_RANGE);
					double y = (y1 * 1.0) / (65536 / IMU3000_RANGE);
					double z = (z1 * 1.0) / (65536 / IMU3000_RANGE); 

					if (!gyro_calibrated) {
						gyro_calX = x;
						gyro_calY = y;
						gyro_calZ = z;
						gyro_calibrated = true;
					}

					this._stringValueText.Text = String.Format ("rotation: {0}, {1}, {2} /nxyz degrees/sec", 
						Math.Round(x - gyro_calX,1),
						Math.Round(y - gyro_calY,1),
						Math.Round(z - gyro_calZ,1));

				} else if (_characteristic.ID.PartialFromUuid () == "0xffe1") {
					// Smart Keys: Bit 2 - side key, Bit 1 - right key, Bit 0 – left key - works
					var b = ((int)_characteristic.Value[0]) % 4;
					switch (b) {
					case 1:
						this._stringValueText.Text = "Right button";
						break;
					case 2:
						this._stringValueText.Text = "Left button";
						break;
					case 3:
						this._stringValueText.Text = "Both buttons";
						break;
					default:
						this._stringValueText.Text = "Neither button";
						break;
					}
					this._stringValueText.Text += " " + b;
				}
			} 
			//TODO: this._valueUpdatedDateTime
		}
	}
}

