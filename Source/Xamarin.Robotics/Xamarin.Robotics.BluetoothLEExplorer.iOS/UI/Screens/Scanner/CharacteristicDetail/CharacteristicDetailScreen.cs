using System;
using MonoTouch.UIKit;
using System.Drawing;
using MonoTouch.CoreBluetooth;
using Xamarin.Robotics.Core.Bluetooth.LE;

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
			_valueUdpatedDateTime = new UILabel (new RectangleF (10, 190, 300, 60));


			_rawValueText.Text = "raw";
			_stringValueText.Text = "string";
			_valueUdpatedDateTime.Text = "-";

			// populate our page
			this._characteristicNameText.Text = _characteristic.Name;
			this._characteristicIDText.Text = _characteristic.ID.ToString ();



			Add (_characteristicNameText);
			Add (_characteristicIDText);
			Add (_rawValueText);
			Add (_stringValueText);
			Add (_valueUdpatedDateTime);

			this._valueUpdatedHandler = (s, e) => {
				Console.WriteLine("-- _valueUpdatedHandler: " +  e.Characteristic.Value);
				this.InvokeOnMainThread( () => {
					this.PopulateValueInfo();
				});
			};

			// request the value to be read
			_characteristic.RequestValue();
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
				this._rawValueText.Text = string.Join (",", _characteristic.Value);
				this._stringValueText.Text = _characteristic.StringValue;
			}
			//TODO: this._valueUpdatedDateTime
		}

	}
}

