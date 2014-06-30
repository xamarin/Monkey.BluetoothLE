using System;
using System.Collections.Generic;
using MonoTouch.CoreBluetooth;
using MonoTouch.Foundation;
using MonoTouch.UIKit;
using Xamarin.Robotics.Core.Bluetooth.LE;

namespace Xamarin.Robotics.BluetoothLEExplorer.iOS.UI.Screens.Scanner.DeviceDetails
{
	[Register("ServiceListScreen")]
	public class ServiceListScreen : UITableViewController
	{
		//protected List<IService> _services = new List<IService>();
		protected Dictionary<IService, ICharacteristic> _serviceCharacteristics = new Dictionary<IService, ICharacteristic>();
		protected ServiceDetails.CharacteristicListScreen _characteristicListScreen;

		ServiceTableSource _tableSource;

		public IDevice ConnectedDevice
		{
			get { return this._connectedDevice; }
			set {
				this._connectedDevice = value;
				this.InitializeDevice ();
			}
		}
		protected IDevice _connectedDevice;

		public ServiceListScreen (IntPtr handle) : base(handle)
		{
			this.Initialize();
		}

		public ServiceListScreen ()
		{
			this.Initialize ();
		}

		protected void Initialize()
		{
			this._tableSource = new ServiceTableSource ();

			this._tableSource.ServiceSelected += (object sender, ServiceTableSource.ServiceSelectedEventArgs e) => {
				//HACK: this._characteristicListScreen = this.Storyboard.InstantiateViewController("CharacteristicListScreen") as ServiceDetails.CharacteristicListScreen;
				this._characteristicListScreen = new ServiceDetails.CharacteristicListScreen();

				this._characteristicListScreen.SetDeviceAndService ( this._connectedDevice, e.Service );
				this.NavigationController.PushViewController(this._characteristicListScreen, true);
			};
		}

		public override void ViewDidLoad ()
		{
			base.ViewDidLoad ();

			this._tableSource.Services = this._connectedDevice.Services;
			TableView.Source = this._tableSource;

			this.NavigationItem.SetLeftBarButtonItem( new UIBarButtonItem ("Disconnect", UIBarButtonItemStyle.Plain, (s,e) => {
				Adapter.Current.DisconnectDevice (this._connectedDevice);
			}), false );
		}

		protected void InitializeDevice()
		{
			// update all our shit

			// > peripheral
			//   > service[s]
			//		> characteristic
			//			> value
			//			> descriptor[s]

			this.Title = this._connectedDevice.Name==null?"<null device name>":this._connectedDevice.Name;

			// when a device disconnects, show an alert and unload this screen
			Adapter.Current.DeviceDisconnected += (object sender, DeviceConnectionEventArgs e) => {
				var alert = new UIAlertView("Peripheral Disconnected", e.Device.Name + " disconnected", 
					null, "ok", null);
				alert.Clicked += (object s, UIButtonEventArgs e2) => {
					Console.WriteLine ("Alert.Clicked");
					this.NavigationController.PopToRootViewController(true); //.PopViewControllerAnimated(true);
				};
				alert.Show();
			};

			//
			this._connectedDevice.DiscoverServices ();
			this._connectedDevice.ServicesDiscovered += (object sender, EventArgs e) => {
				Console.WriteLine("ServiceListScreen.ServicesDiscovered");
				TableView.ReloadData();
			};
		}

		protected class ServiceTableSource : UITableViewSource
		{
			protected const string cellID = "BleServiceCell";
			public event EventHandler<ServiceSelectedEventArgs> ServiceSelected = delegate {};

			public IList<IService> Services
			{
				get { return this._services; }
				set { this._services = value; }
			}
			protected IList<IService> _services = new List<IService>();

//			public override int NumberOfSections (UITableView tableView)
//			{
//				return 1;
//			}

			public override int RowsInSection (UITableView tableview, int section)
			{
				return this._services.Count;
			}

			public override UITableViewCell GetCell (UITableView tableView, NSIndexPath indexPath)
			{
				UITableViewCell cell = tableView.DequeueReusableCell (cellID);
				if (cell == null) {
					cell = new UITableViewCell (UITableViewCellStyle.Subtitle, cellID);
				}

				IService service = this._services [indexPath.Row];
				cell.TextLabel.Text = service.Name;
				cell.DetailTextLabel.Text = service.ID.ToString ();

				return cell;
			}

			public override void RowSelected (UITableView tableView, NSIndexPath indexPath)
			{
				IService service = this._services [indexPath.Row];

				this.ServiceSelected (this, new ServiceSelectedEventArgs (service));

				tableView.DeselectRow (indexPath, true);
			}

			public class ServiceSelectedEventArgs : EventArgs
			{
				public IService Service
				{
					get { return this._service; }
					set { this._service = value; }
				}
				protected IService _service;

				public ServiceSelectedEventArgs (IService service)
				{
					this._service = service;
				}
			}
		}
	}
}

