using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MBProgressHUD;
using MonoTouch.CoreBluetooth;
using MonoTouch.Foundation;
using MonoTouch.UIKit;
using Xamarin.Robotics.Core.Bluetooth.LE;
using Xamarin.Robotics.BluetoothLEExplorer.iOS.UI.Controls;

namespace Xamarin.Robotics.BluetoothLEExplorer.iOS.UI.Screens.Scanner.Home
{
	[Register("ScannerHome")]
	public partial class ScannerHome : UITableViewController
	{
		ScanButton _scanButton;
		BleDeviceTableSource _tableSource;
		MTMBProgressHUD _connectingDialog;
		DeviceDetails.ServiceListScreen _ServiceListScreen;

		public ScannerHome (IntPtr handle) : base (handle) 
		{
			this.Initialize ();
		}

		public ScannerHome ()
		{
			this.Initialize ();
		}

		protected void Initialize()
		{
			this.Title = "Scanner";

			// configure our scan button
			this._scanButton = new ScanButton ();
			this._scanButton.TouchUpInside += (s,e) => {
				if ( !Adapter.Current.IsScanning ) {
					Adapter.Current.StartScanningForDevices ();
				} else {
					Adapter.Current.StopScanningForDevices ();
				}
			};			 
			this.NavigationItem.SetRightBarButtonItem (new UIBarButtonItem (this._scanButton), false);

			// setup the table
			this._tableSource = new BleDeviceTableSource ();
			this._tableSource.DeviceSelected += (object sender, BleDeviceTableSource.DeviceSelectedEventArgs e) => {

				// stop scanning
				new Task( () => {
					if(Adapter.Current.IsScanning) {
						Console.WriteLine ("Still scanning, stopping the scan and reseting the right button");
						Adapter.Current.StopScanningForDevices();
						this._scanButton.SetState (ScanButton.ScanButtonState.Normal);
					}
				}).Start();

				// show our connecting... overlay
				this._connectingDialog.LabelText = "Connecting to " + e.SelectedDevice.Name;
				this._connectingDialog.Show(true);

				// when the peripheral connects, load our details screen
				Adapter.Current.DeviceConnected += (object s, DeviceConnectionEventArgs connectArgs) => {
					this._connectingDialog.Hide(false);

					//HACK: this._ServiceListScreen = this.Storyboard.InstantiateViewController("ServiceListScreen") as DeviceDetails.ServiceListScreen;
					this._ServiceListScreen = new DeviceDetails.ServiceListScreen();

					this._ServiceListScreen.ConnectedDevice = connectArgs.Device;
					this.NavigationController.PushViewController ( this._ServiceListScreen, true);

				};

				Adapter.Current.DeviceFailedToConnect += (object s, DeviceConnectionEventArgs connectArgs) => {
					this._connectingDialog.Hide(false);

					new UIAlertView ("Device Failed to Connect", connectArgs.ErrorMessage, null, "ok. :(", null).Show();
				};

				// try and connect to the peripheral
				Adapter.Current.ConnectToDevice (e.SelectedDevice);
			};


		}

		public override void ViewDidLoad ()
		{
			base.ViewDidLoad ();

			// iOS7 idiocy
			this.TableView.ContentInset = new UIEdgeInsets (this.TopLayoutGuide.Length, 0, 0, 0);

			TableView.Source = this._tableSource;

			// wire up the DiscoveredPeripheral event to update the table
			Adapter.Current.DeviceDiscovered += (object sender, DeviceDiscoveredEventArgs e) => {
				this._tableSource.Peripherals = Adapter.Current.DiscoveredDevices;
				this.TableView.ReloadData();
			};

			Adapter.Current.ScanTimeoutElapsed += (sender, e) => {
				this._scanButton.SetState ( ScanButton.ScanButtonState.Normal );
			};

			// add our 'connecting' overlay
			this._connectingDialog = new MTMBProgressHUD (View) {
				LabelText = "Connecting to device...",
				RemoveFromSuperViewOnHide = false
			};
			this.View.AddSubview (this._connectingDialog);		
		}

		protected class BleDeviceTableSource : UITableViewSource
		{
			protected const string cellID = "BleDeviceCell";

			public event EventHandler<DeviceSelectedEventArgs> DeviceSelected = delegate {};

			public IList<IDevice> Peripherals
			{
				get { return this._devices; }
				set { this._devices = value; }
			}
			protected IList<IDevice> _devices = new List<IDevice> ();

			public BleDeviceTableSource () {}

			public BleDeviceTableSource (List<IDevice> devices)
			{
				_devices = devices;
			}

			public override int NumberOfSections (UITableView tableView)
			{
				return 1;
			}

			public override int RowsInSection (UITableView tableview, int section)
			{
				return this._devices.Count;
			}

			public override UITableViewCell GetCell (UITableView tableView, NSIndexPath indexPath)
			{
				UITableViewCell cell = tableView.DequeueReusableCell (cellID);
				if (cell == null) {
					cell = new UITableViewCell (UITableViewCellStyle.Subtitle, cellID);
				}

				IDevice device = this._devices [indexPath.Row];
				//TODO: convert to async and update?
				//device.ReadRSSI ();
				cell.TextLabel.Text = device.Name + " RSSI: " + device.Rssi;
				cell.DetailTextLabel.Text = "ID: " + device.ID.ToString ();

				return cell;
			}

			public override void RowSelected (UITableView tableView, NSIndexPath indexPath)
			{
				IDevice device = this._devices [indexPath.Row];
				tableView.DeselectRow (indexPath, false);
				this.DeviceSelected (this, new DeviceSelectedEventArgs (device));
			}

			public class DeviceSelectedEventArgs : EventArgs
			{
				public IDevice SelectedDevice
				{
					get { return this._device; }
				} protected IDevice _device;

				public DeviceSelectedEventArgs (IDevice device)
				{
					this._device = device;
				}
			}
		}
	}

}

