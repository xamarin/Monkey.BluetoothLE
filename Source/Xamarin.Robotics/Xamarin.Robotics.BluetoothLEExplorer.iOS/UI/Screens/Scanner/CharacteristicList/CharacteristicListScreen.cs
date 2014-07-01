using System;
using System.Collections.Generic;
using MonoTouch.CoreBluetooth;
using MonoTouch.Foundation;
using MonoTouch.UIKit;
using Xamarin.Robotics.Core.Bluetooth.LE;
using System.Text;

namespace Xamarin.Robotics.BluetoothLEExplorer.iOS.UI.Screens.Scanner.ServiceDetails
{
	[Register("CharacteristicListScreen")]
	public class CharacteristicListScreen : UITableViewController
	{
		CharacteristicTableSource _tableSource;
		//protected List<ICharacteristic> _characteristics = new List<ICharacteristic>();

		protected IDevice _connectedDevice;
		protected IService _currentService;

		public CharacteristicListScreen (IntPtr handle) : base(handle)
		{
			this.Initialize();
		}

		public CharacteristicListScreen ()
		{
			this.Initialize ();
		}

		protected void Initialize()
		{
			this._tableSource = new CharacteristicTableSource ();

//			// when the characteristic is selected in the table, make a request to disover the descriptors for it.
			this._tableSource.CharacteristicSelected += (object sender, CharacteristicTableSource.CharacteristicSelectedEventArgs e) => {

				Console.WriteLine("Characteristic: " + e.Characteristic.Name);


				var _characteristicDetailScreen = new CharacteristicDetailScreen();
//				var _characteristicDetailScreen = new CharacteristicDetailScreen_Hrm();

				_characteristicDetailScreen.SetDeviceServiceAndCharacteristic ( this._connectedDevice, this._currentService, e.Characteristic );

				this.NavigationController.PushViewController(_characteristicDetailScreen, true);
			};
		}

		public override void ViewDidLoad ()
		{
			base.ViewDidLoad ();

			TableView.Source = this._tableSource;

		}

		public void SetDeviceAndService (IDevice device, IService service)
		{
			this._connectedDevice = device;
			this._currentService = service;

			this.Title = this._currentService.Name;

			this._tableSource.Characteristics = this._currentService.Characteristics;

			// wire up our handler for when characteristics are discovered
			(this._currentService as Service).CharacteristicsDiscovered += (object sender, EventArgs e) => {
				Console.WriteLine("CharacteristicsDiscovered");
				TableView.ReloadData();
			};

			// discover the charactersistics
			(this._currentService as Service).DiscoverCharacteristics ();


//			// when a descriptor is dicovered, reload the table.
//			this._connectedDevice.DiscoveredDescriptor += (object sender, CBCharacteristicEventArgs e) => {
//				foreach (var descriptor in e.Characteristic.Descriptors) {
//					Console.WriteLine ("Characteristic: " + e.Characteristic.Value + " Discovered Descriptor: " + descriptor);	
//				}
//				// reload the table
//				this.CharacteristicsTable.ReloadData();
//			};



		}

		protected class DisconnectAlertViewDelegate : UIAlertViewDelegate
		{
			protected UIViewController _parent;

			public DisconnectAlertViewDelegate(UIViewController parent)
			{
				this._parent = parent;
			}

			public override void Clicked (UIAlertView alertview, int buttonIndex)
			{
				_parent.NavigationController.PopViewControllerAnimated(true);
			}

			public override void Canceled (UIAlertView alertView)
			{
				_parent.NavigationController.PopViewControllerAnimated(true);
			}
		}

		protected class CharacteristicTableSource : UITableViewSource
		{
			protected const string cellID = "BleCharacteristicCell";
			public event EventHandler<CharacteristicSelectedEventArgs> CharacteristicSelected = delegate {};

			public IList<ICharacteristic> Characteristics
			{
				get { return this._characteristics; }
				set { this._characteristics = value; }
			}
			protected IList<ICharacteristic> _characteristics = new List<ICharacteristic>();

//			public override int NumberOfSections (UITableView tableView)
//			{
//				return 1;
//			}

			public override int RowsInSection (UITableView tableview, int section)
			{
				return this._characteristics.Count;
			}

			public override UITableViewCell GetCell (UITableView tableView, NSIndexPath indexPath)
			{
				UITableViewCell cell = tableView.DequeueReusableCell (cellID);
				if (cell == null) {
					cell = new UITableViewCell (UITableViewCellStyle.Subtitle, cellID);
				}

				ICharacteristic characteristic = this._characteristics [indexPath.Row];
				cell.TextLabel.Text = characteristic.Name;
				StringBuilder descriptors = new StringBuilder ();
				if (characteristic.Descriptors != null) {
					foreach (var descriptor in characteristic.Descriptors) {
						descriptors.Append (descriptor.ID + " ");
					}
					cell.DetailTextLabel.Text = descriptors.ToString ();
				} else {
					cell.DetailTextLabel.Text = "Select to discover characteristic descriptors.";
				}

				return cell;
			}


			public override void RowSelected (UITableView tableView, NSIndexPath indexPath)
			{
				ICharacteristic characteristic = this._characteristics [indexPath.Row];
				Console.WriteLine ("Selected: " + characteristic.Name);

				this.CharacteristicSelected (this, new CharacteristicSelectedEventArgs (characteristic));

				tableView.DeselectRow (indexPath, true);
			}

			public class CharacteristicSelectedEventArgs : EventArgs
			{
				public ICharacteristic Characteristic
				{
					get { return this._characteristic; }
					set { this._characteristic = value; }
				}
				protected ICharacteristic _characteristic;

				public CharacteristicSelectedEventArgs (ICharacteristic characteristic)
				{
					this._characteristic = characteristic;
				}
			}
		}
	}
}

