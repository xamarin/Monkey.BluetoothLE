using System;
using System.Collections.Generic;
using Xamarin.Forms;
using System.Threading.Tasks;
using Robotics.Mobile.Core.Bluetooth.LE;
using System.Diagnostics;
using Robotics.Messaging;
using System.Threading;
using System.Linq;

namespace HeadLights
{	
	public partial class MainPage : ContentPage
	{	
		Task<ControlClient> connectTask;

		public MainPage (IAdapter adapter)
		{
			InitializeComponent ();

			connectTask = ConnectAsync (adapter);
		}

		Task<ControlClient> ConnectAsync (IAdapter adapter)
		{
			var tcs = new TaskCompletionSource<ControlClient> ();

			adapter.DeviceDiscovered += (object sender, DeviceDiscoveredEventArgs e) => {
				Device.BeginInvokeOnMainThread(async () => {

					// Look for a specific device
					if (e.Device.ID.ToString ().StartsWith ("af18", StringComparison.OrdinalIgnoreCase)) {

						// Connect to the device
						await adapter.ConnectAsync (e.Device);

						// Establish the control client
						using (var stream = new LEStream (e.Device)) {
							var client = new ControlClient (stream);
							client.RunAsync (CancellationToken.None); // Don't await to run in background
							tcs.SetResult (client);
						}

						// Update the UI
						connectLabel.Text = "Yay " + e.Device + "!";
					}
				});
			};

			adapter.StartScanningForDevices();

			return tcs.Task;
		}

		async void ToggleLeft (object sender, EventArgs e)
		{
			var c = await connectTask;

			var v = c.Variables.FirstOrDefault (x => x.Name == "LeftLight.Input");

			if (v != null) {
				v.Value = leftSwitch.IsToggled ? 1 : 0;
			}
		}

		async void ToggleRight (object sender, EventArgs e)
		{
			var c = await connectTask;

			var v = c.Variables.FirstOrDefault (x => x.Name == "RightLight.Input");

			if (v != null) {
				v.Value = rightSwitch.IsToggled ? 1 : 0;
			}
		}
	}
}

