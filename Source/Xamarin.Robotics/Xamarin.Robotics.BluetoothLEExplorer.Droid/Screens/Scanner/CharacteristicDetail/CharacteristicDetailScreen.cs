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
using Xamarin.Robotics.Core.Bluetooth.LE;
using Xamarin.Robotics.BluetoothLEExplorer.Droid.UI;

namespace Xamarin.Robotics.BluetoothLEExplorer.Droid.Screens.Scanner.CharacteristicDetail
{
	[Activity]			
	public class CharacteristicDetailScreen : NoTitleActivityBase
	{
		protected TextView _characteristicNameText;
		protected TextView _characteristicIDText;
		protected TextView _rawValueText;
		protected TextView _stringValueText;
		protected TextView _valueUdpatedDateTime;

		protected EventHandler<CharacteristicReadEventArgs> _valueUpdatedHandler;

		protected override void OnCreate (Bundle bundle)
		{
			base.OnCreate (bundle);

			// load our layout
			SetContentView (Resource.Layout.CharacteristicDetailScreen);

			// find our controls
			this._characteristicNameText = FindViewById<TextView> (Resource.Id.CharacteristicNameText);
			this._characteristicIDText = FindViewById<TextView> (Resource.Id.CharacteristicIDText);
			this._rawValueText = FindViewById<TextView> (Resource.Id.RawValueText);
			this._stringValueText = FindViewById<TextView> (Resource.Id.StringValueText);
			this._valueUdpatedDateTime = FindViewById<TextView> (Resource.Id.ValueUdpatedDateTime);

			// populate our page
			this._characteristicNameText.Text = App.Current.State.SelectedCharacteristic.Name;
			this._characteristicIDText.Text = App.Current.State.SelectedCharacteristic.ID.ToString ();

			// attempt at a value read, in case it is already present (likely never will happen)
			this.PopulateValueInfo ();

			// request the value to be read
			App.Current.State.SelectedCharacteristic.RequestValue ();
		}

		protected void PopulateValueInfo()
		{
			if (App.Current.State.SelectedCharacteristic.Value == null) {
				this._rawValueText.Text = this._stringValueText.Text = "Waiting for update...";

			} else {
				this._rawValueText.Text = string.Join (",", App.Current.State.SelectedCharacteristic.Value);
				this._stringValueText.Text = App.Current.State.SelectedCharacteristic.StringValue;
			}
			//this._valueUdpatedDateTime
		}

		protected override void OnResume ()
		{
			base.OnResume ();

			this.WireupLocalHandlers ();
			this.WireupExternalHandlers ();
		}

		protected override void OnPause ()
		{
			base.OnPause ();

			// unwire external event handlers (memory leaks)
			this.RemoveExternalHandlers ();
		}

		protected void WireupLocalHandlers ()
		{
		}

		protected void WireupExternalHandlers ()
		{
			this._valueUpdatedHandler = (s, e) => {
				this.RunOnUiThread( () => {
					this.PopulateValueInfo();
				});
			};
			App.Current.State.SelectedCharacteristic.ValueUpdated += this._valueUpdatedHandler;
		}

		protected void RemoveExternalHandlers()
		{
			App.Current.State.SelectedCharacteristic.ValueUpdated -= this._valueUpdatedHandler;
		}

	}
}

