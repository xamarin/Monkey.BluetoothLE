using System;
using System.Collections.Generic;
using Xamarin.Forms;
using Xamarin.Robotics.Mobile.Core.Bluetooth.LE;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Collections.ObjectModel;

namespace Xamarin.Robotics.Mobile.Robotroller
{	
	public partial class DeviceList : ContentPage
	{	
		readonly IAdapter adapter;
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
//				adapter.StopScanningForDevices(); // not sure why it doesn't stop already, if the timeout elapses... or is this a fake timeout we made?
//				Device.BeginInvokeOnMainThread ( () => {
//					DisplayAlert("Timeout", "Bluetooth scan timeout elapsed", "OK", "");
//				});
				Debug.WriteLine ("Scan timeout");
			};

			Appearing += (sender, e) => {
				StartScanning();
			};
		}

		public async void OnItemSelected (object sender, SelectedItemChangedEventArgs e)
		{
			if (((ListView)sender).SelectedItem == null) {
				return;
			}
			StopScanning ();

			var device = e.SelectedItem as IDevice;

			try {
				Debug.WriteLine ("Connecting to "+device.Name+"...");
				await adapter.ConnectAsync (device);
				Debug.WriteLine ("Trying to read...");
				using (var s = new LEStream (device)) {
					var buffer = new byte [4];
					var n = await s.ReadAsync (buffer, 0, 4);
					Debug.WriteLine ("READ: " + System.Text.Encoding.UTF8.GetString (buffer, 0, n));
				}
			} catch (Exception ex) {
				Debug.WriteLine ("Stream failed");
				Debug.WriteLine (ex);
			}
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
