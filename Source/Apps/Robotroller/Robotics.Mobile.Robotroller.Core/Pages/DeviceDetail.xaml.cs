using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xamarin.Forms;
using Robotics.Messaging;
using Robotics.Mobile.Core.Bluetooth.LE;

namespace Robotics.Mobile.Robotroller
{	
	public partial class DeviceDetail : TabbedPage
	{	
		readonly IAdapter adapter;
		readonly Guid deviceId;

		readonly TaskScheduler scheduler;

		public DeviceDetail (IAdapter adapter, Guid deviceId)
		{
			scheduler = TaskScheduler.FromCurrentSynchronizationContext ();

			this.adapter = adapter;
			this.deviceId = deviceId;
			InitializeComponent ();

			this.Appearing += async (sender, e) => {
				await RunControlAsync ();
			};

			adapter.DeviceDisconnected += (sender, e) => {
				// if device disconnects, return to main list screen
				Navigation.PopToRootAsync();
			};
		}

		public void OnVariableSelected (object sender, SelectedItemChangedEventArgs e)
		{
			((ListView)sender).SelectedItem = null;
		}

		public async void OnCommandSelected (object sender, SelectedItemChangedEventArgs e)
		{
			var command = e.SelectedItem as Robotics.Messaging.Command;

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


		#region Joystick
		// use device gyroscope for speed and direction
		// gets wired/unwired when 
		IGyro boundGyro;
		DateTime lastGyroUpdateTime = DateTime.Now;
		static readonly TimeSpan GyroUpdateInterval = TimeSpan.FromSeconds (1.0/2);

		void UnbindGyro ()
		{
			if (boundGyro == null)
				return;
			boundGyro.GyroUpdated -= HandleGyroUpdated;
			boundGyro = null;
		}

		void HandleGyroUpdated (object sender, EventArgs e)
		{
			var g = (IGyro)sender;
			var now = DateTime.Now;
			if (now - lastGyroUpdateTime > GyroUpdateInterval) {
				lastGyroUpdateTime = now;
				Task.Factory.StartNew (
					() => {
						if (client == null)
							return;

						var speed = Math.Cos (Math.Max (0, Math.Min (Math.PI / 2, g.Pitch)));
//						Debug.WriteLine ("Gyro.Pitch = " + g.Pitch + ", Speed = " + speed);

						var turn = Math.Sin (Math.Max (-Math.PI / 2, Math.Min (Math.PI / 2, g.Roll)));
//						Debug.WriteLine ("Gyro.Roll = " + g.Roll + ", Turn = " + turn);

						var speedVariable = client.Variables.FirstOrDefault (x => x.Name == "Speed");
						if (speedVariable != null) {
							speedVariable.Value = speed;
						}

						var turnVariable = client.Variables.FirstOrDefault (x => x.Name == "Turn");
						if (turnVariable != null) {
							turnVariable.Value = turn;
						}

						// let's show the values we're sending to the robot
						Device.BeginInvokeOnMainThread(() => {
							JoystickOutputSpeed.Text = String.Format("Speed: {0}", Math.Round(speed,2));
							JoystickOutputTurn.Text = String.Format("Turn: {0}", Math.Round(turn,2));
						});
					},
					CancellationToken.None,
					TaskCreationOptions.None,
					scheduler);
			}
		}

		void BindGyro ()
		{
			UnbindGyro ();
			boundGyro = App.Shared.Gyro;
			if (boundGyro == null)
				return;
			boundGyro.GyroUpdated += HandleGyroUpdated;
		}

		public void OnJoystickAppearing (object sender, EventArgs e)
		{
			BindGyro ();
		}

		public void OnJoystickDisappearing (object sender, EventArgs e)
		{
			UnbindGyro ();
		}
		#endregion



		#region Sliders
		// hacky attempt to use sliders for speed and direction
		public void OnSlidersChanged (object sender, EventArgs e)
		{
			// re-use the gyro update interval, but for slider value changes
			var now = DateTime.Now;
			if (now - lastGyroUpdateTime > GyroUpdateInterval) {
				lastGyroUpdateTime = now;
				Task.Factory.StartNew (
					() => {
						if (client == null)
							return;

						var speed = SpeedSlider.Value;

						var speedVariable = client.Variables.FirstOrDefault (x => x.Name == "Speed");
						if (speedVariable != null) {
							speedVariable.Value = speed;
						}

						var turn = TurnSlider.Value - 1;

						var turnVariable = client.Variables.FirstOrDefault (x => x.Name == "Turn");
						if (turnVariable != null) {
							turnVariable.Value = turn;
						}
					},
					CancellationToken.None,
					TaskCreationOptions.None,
					scheduler);
			}
		}

		public void OnCenterClicked (object sender, EventArgs e) {
			TurnSlider.Value = 1;
		}

		public void OnResetClicked (object sender, EventArgs e) {
			SpeedSlider.Value = 0;
			TurnSlider.Value = 1;
		}

		#endregion
	}
}
