using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Xamarin.Robotics.Core.Bluetooth.LE;
using Xamarin.Robotics.BluetoothLEExplorer.Droid.UI.Adapters;
using Xamarin.Robotics.BluetoothLEExplorer.Droid.UI.Controls;
using Xamarin.Robotics.BluetoothLEExplorer.Droid.UI;

namespace Xamarin.Robotics.BluetoothLEExplorer.Droid.Screens.Scanner.Home
{
	[Activity (MainLauncher = true)]			
	public class ScannerHome : NoTitleActivityBase
	{
		protected ListView _listView;
		protected ScanButton _scanButton;
		protected DevicesAdapter _listAdapter;
		protected ProgressDialog _progress;
		protected IDevice _deviceToConnect; //not using State.SelectedDevice because it may not be connected yet

		// external handlers
		EventHandler<DeviceDiscoveredEventArgs> deviceDiscoveredHandler;
		EventHandler<DeviceConnectionEventArgs> deviceConnectedHandler;

		protected override void OnCreate (Bundle bundle)
		{
			base.OnCreate (bundle);

			// load our layout
			SetContentView (Resource.Layout.ScannerHome);

			// find our controls
			this._listView = FindViewById<ListView> (Resource.Id.DevicesListView);
			this._scanButton = FindViewById<ScanButton> (Resource.Id.ScanButton);

			// create our list adapter
			this._listAdapter = new DevicesAdapter(this, App.Current.BleAdapter.DiscoveredDevices);
			this._listView.Adapter = this._listAdapter;

			this.WireupLocalHandlers ();
		}
		
		protected override void OnResume ()
		{
			base.OnResume ();
			
			this.WireupExternalHandlers ();
		}
		
		protected override void OnPause ()
		{
			base.OnPause ();

			// stop our scanning (does a check, and also runs async)
			this.StopScanning ();
			
			// unwire external event handlers (memory leaks)
			this.RemoveExternalHandlers ();
		}

		protected void WireupLocalHandlers ()
		{
			this._scanButton.Click += (object sender, EventArgs e) => {
				if ( !App.Current.BleAdapter.IsScanning ) {
					App.Current.BleAdapter.StartScanningForDevices ();
				} else {
					App.Current.BleAdapter.StopScanningForDevices ();
				}
			};

			this._listView.ItemClick += (object sender, AdapterView.ItemClickEventArgs e) => {
				Console.Write ("ItemClick: " + this._listAdapter.Items[e.Position]);

				// stop scanning
				this.StopScanning();

				// select the item
				this._listView.ClearFocus();
				this._listView.Post( () => {
					this._listView.SetSelection (e.Position);
				});
				//this._listView.SetItemChecked (e.Position, true);
				// todo: for some reason, we're losing the selection, so i have to cache it
				// think i know the issue, see the note in the GenericObjectAdapter class
				this._deviceToConnect = this._listAdapter.Items[e.Position];

				// show a connecting overlay
				// TODO: make this conform to lifecycle, see: https://github.com/xamarin/private-samples/blob/master/EvolveCurriculum/Advanced/10%20-%20Advanced%20Android%20Application%20Lifecycle/ActivityLifecycle/MainActivity.cs
				this.RunOnUiThread( () => {	
					//TODO: we need to save a ref to the device when click
					this._progress = ProgressDialog.Show(this, "Connecting", "Connecting to " + this._deviceToConnect.Name, true);
				});

				// try and connect
				App.Current.BleAdapter.ConnectToDevice ( this._listAdapter[e.Position] );

			};
		}

		protected void WireupExternalHandlers ()
		{
			this.deviceDiscoveredHandler = (object sender, DeviceDiscoveredEventArgs e) => {
				Console.WriteLine ("Discovered device: " + e.Device.Name);

				// reload the list view
				//TODO: why doens't NotifyDataSetChanged work? is it because i'm replacing the reference?
				this.RunOnUiThread( () => {
					this._listAdapter = new DevicesAdapter(this, App.Current.BleAdapter.DiscoveredDevices);
					this._listView.Adapter = this._listAdapter;
				});
			};
			App.Current.BleAdapter.DeviceDiscovered += this.deviceDiscoveredHandler;

			this.deviceConnectedHandler = (object sender, DeviceConnectionEventArgs e) => {
				this.RunOnUiThread( () => {
					this._progress.Hide();
				});
				// now that we're connected, save it
				App.Current.State.SelectedDevice = e.Device;

				// launch the details screen
				this.StartActivity (typeof(Xamarin.Robotics.BluetoothLEExplorer.Droid.Screens.Scanner.ServiceList.ServiceListScreen));
			};
			App.Current.BleAdapter.DeviceConnected += this.deviceConnectedHandler;
		}

		protected void RemoveExternalHandlers()
		{
			App.Current.BleAdapter.DeviceDiscovered -= this.deviceDiscoveredHandler;
			App.Current.BleAdapter.DeviceConnected -= this.deviceConnectedHandler;
		}

		protected void StopScanning()
		{
			// stop scanning
			new Task( () => {
				if(App.Current.BleAdapter.IsScanning) {
					Console.WriteLine ("Still scanning, stopping the scan and reseting the right button");
					App.Current.BleAdapter.StopScanningForDevices();
					this._scanButton.SetState (ScanButton.ScanButtonState.Normal);
				}
			}).Start();
		}
	}
}

