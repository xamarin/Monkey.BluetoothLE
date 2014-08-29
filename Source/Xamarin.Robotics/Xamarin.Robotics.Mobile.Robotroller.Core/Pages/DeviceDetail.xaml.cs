using System;
using System.Collections.Generic;
using Xamarin.Forms;
using System.Diagnostics;
using System.Threading.Tasks;
using Xamarin.Robotics.Mobile.Core.Bluetooth.LE;
using System.Linq;
using Xamarin.Robotics.Messaging;
using System.Threading;

namespace Xamarin.Robotics.Mobile.Robotroller
{	
	public partial class DeviceDetail : TabbedPage
	{	
		readonly IAdapter adapter;
		readonly Guid deviceId;

		public DeviceDetail (IAdapter adapter, Guid deviceId)
		{
			this.adapter = adapter;
			this.deviceId = deviceId;
			InitializeComponent ();

			this.Appearing += async (sender, e) => {
				await RunControlAsync ();
			};
		}

		public void OnVariableSelected (object sender, SelectedItemChangedEventArgs e)
		{
			((ListView)sender).SelectedItem = null;
		}

		public async void OnCommandSelected (object sender, SelectedItemChangedEventArgs e)
		{
			var command = e.SelectedItem as Xamarin.Robotics.Messaging.Command;

			if (client != null && command != null) {
				await client.ExecuteCommandAsync (command);
			}
			((ListView)sender).SelectedItem = null;
		}

		async Task<IDevice> ConnectAsync ()
		{
			var device = adapter.DiscoveredDevices.First (x => x.ID == deviceId);
			Debug.WriteLine ("Connecting to " + device.Name + "...");
			await adapter.ConnectAsync (device);
			Debug.WriteLine ("Trying to read...");
			return device;
		}

		ControlClient client;
			
		async Task RunControlAsync ()
		{
			var cts = new CancellationTokenSource ();
			try {
				var device = await ConnectAsync ();
				using (var s = new LEStream (device)) {
					client = new ControlClient (s);
					variablesList.ItemsSource = client.Variables;
					commandsList.ItemsSource = client.Commands;
					await client.RunAsync (cts.Token);
				}
			} catch (Exception ex) {
				Debug.WriteLine ("Stream failed");
				Debug.WriteLine (ex);
			} finally {
				client = null;
			}
		}
	}
}

