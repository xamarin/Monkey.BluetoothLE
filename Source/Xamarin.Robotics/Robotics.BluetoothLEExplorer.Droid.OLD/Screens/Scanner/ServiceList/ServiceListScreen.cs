using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Java.Util;
using Xamarin.Robotics.Core.Bluetooth.LE;
using Xamarin.Robotics.BluetoothLEExplorer.Droid.UI;

namespace Xamarin.Robotics.BluetoothLEExplorer.Droid.Screens.Scanner.ServiceList
{
	[Activity]			
	public class ServiceListScreen : NoTitleActivityBase
	{
		// members
		protected ListView _listView;
		protected TextView _deviceNameText;
		protected TextView _deviceAddressText;
		protected IList<IService> _services = new List<IService>();
		protected ServicesAdapter _listAdapter;

		// external handlers
		EventHandler serviceDiscoveredHandler;

		protected override void OnCreate (Bundle bundle)
		{
			base.OnCreate (bundle);

			// load our layout
			SetContentView (Resource.Layout.ServiceListScreen);

			// find our controls
			this._listView = FindViewById<ListView> (Resource.Id.ServiceListView);
			this._deviceNameText = FindViewById<TextView> (Resource.Id.DeviceNameText);
			this._deviceAddressText = FindViewById<TextView> (Resource.Id.DeviceAddressText);

			// set the device info
			this._deviceNameText.Text = App.Current.State.SelectedDevice.Name;
			this._deviceAddressText.Text = App.Current.State.SelectedDevice.ID.ToString();

			// create our adapter
			this._listAdapter = new ServicesAdapter (this, this._services);
			this._listView.Adapter = this._listAdapter;

			App.Current.State.SelectedDevice.DiscoverServices ();

			this.WireUpLocalHandlers ();
		}

		protected override void OnResume ()
		{
			base.OnResume ();
			this.WireUpExternalHandlers ();
		}

		protected override void OnPause ()
		{
			base.OnPause ();
			this.RemoveExternalHandlers ();
		}

		protected void WireUpLocalHandlers ()
		{
			this._listView.ItemClick += (object sender, AdapterView.ItemClickEventArgs e) => {
				Console.WriteLine ("Clicked on service item " + e.Position.ToString() + ", launching characteristic list screen.");
				// set the selection
				this._listView.SetSelection(e.Position);

				// persist the selected service service
				App.Current.State.SelectedService = this._services[e.Position];

				// launch the service details screen
				StartActivity (typeof(Xamarin.Robotics.BluetoothLEExplorer.Droid.Screens.Scanner.CharacteristicList.CharacteristicListScreen));
			};
		}

		protected void WireUpExternalHandlers ()
		{
			serviceDiscoveredHandler = (object sender, EventArgs e) => {
				if (App.Current.State.SelectedDevice != null) {

					this._services = App.Current.State.SelectedDevice.Services;

					//TODO: why doens't update work? is it because i'm replacing the reference?
					this.RunOnUiThread( () => {
						this._listAdapter = new ServicesAdapter (this, this._services);
						this._listView.Adapter = this._listAdapter;
					});
				}
			};
			App.Current.State.SelectedDevice.ServicesDiscovered += serviceDiscoveredHandler;
		}

		protected void RemoveExternalHandlers()
		{
			App.Current.State.SelectedDevice.ServicesDiscovered -= serviceDiscoveredHandler;
		}

		public override void OnBackPressed ()
		{
			base.OnBackPressed ();

			// disconnect from device
			if (App.Current.State.SelectedDevice != null)
				App.Current.BleAdapter.DisconnectDevice (App.Current.State.SelectedDevice);
		}

	}
}

