using System;
using System.Collections.Generic;
using Xamarin.Forms;
using Xamarin.Robotics.Core.Bluetooth.LE;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Collections.ObjectModel;

namespace Xamarin.Robotics.BluetoothLEExplorerForms
{	
	public partial class DeviceList : ContentPage
	{	
		IAdapter adapter;
		ObservableCollection<IDevice> devices;

		public DeviceList (IAdapter adapter)
		{
			InitializeComponent ();
			this.adapter = adapter;
			this.devices = new ObservableCollection<IDevice> ();
			listView.ItemsSource = devices;

			adapter.DeviceDiscovered += (object sender, DeviceDiscoveredEventArgs e) => {
				Device.BeginInvokeOnMainThread(() => {
					devices.Add (e.Device);
				});
			};

			adapter.ScanTimeoutElapsed += (sender, e) => {
				DisplayAlert("Timeout", "Bluetooth scan timeout elapsed", "OK", null);
			};

			ScanButton.Activated += (sender, e) => {
				if (adapter.IsScanning) {
					adapter.StopScanningForDevices();
					Debug.WriteLine ("adapter.StopScanningForDevices()");
				} else {
					devices.Clear();
					adapter.StartScanningForDevices();
					Debug.WriteLine ("adapter.StartScanningForDevices()");
				}
			};
		}

		public void OnItemSelected (object sender, SelectedItemChangedEventArgs e) {
			if (((ListView)sender).SelectedItem == null) {
				return;
			}
			StopScanning ();

			var device = e.SelectedItem as IDevice;
			var servicePage = new ServiceList(adapter, device);
			// load services on the next page
			Navigation.PushAsync(servicePage);

			((ListView)sender).SelectedItem = null; // clear selection
		}

		void StopScanning () {
			// stop scanning
			new Task( () => {
				if(adapter.IsScanning) {
					Debug.WriteLine ("Still scanning, stopping the scan");
					adapter.StopScanningForDevices();
				}
			}).Start();
		}
	}
}
