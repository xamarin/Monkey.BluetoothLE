using System;
using System.Collections.Generic;
using Xamarin.Forms;
using Xamarin.Robotics.Mobile.Core.Bluetooth.LE;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Collections.ObjectModel;
using System.Linq;
using Xamarin.Robotics.Messaging;

namespace Xamarin.Robotics.Mobile.Robotroller
{	
	public partial class DeviceList : ContentPage
	{	
		readonly IAdapter adapter;
		ObservableCollection<IDevice> devices;

		bool autoScan = false;

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
				Debug.WriteLine ("Scan timeout");
				if (autoScan) {
					StartScanning ();
				}
			};

			Appearing += (sender, e) => {
				StartScanning();
			};
		}

		public void OnItemSelected (object sender, SelectedItemChangedEventArgs e)
		{
			if (((ListView)sender).SelectedItem == null) {
				return;
			}
			StopScanning ();

			var device = e.SelectedItem as IDevice;

			var servicePage = new DeviceDetail(adapter, device.ID);
			// load services on the next page
			Navigation.PushAsync(servicePage);

			((ListView)sender).SelectedItem = null; // clear selection
		}

		void StartScanning () {
			if (!adapter.IsScanning) {
				devices.Clear();
				adapter.StartScanningForDevices();
				Debug.WriteLine ("adapter.StartScanningForDevices()");
			}
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
