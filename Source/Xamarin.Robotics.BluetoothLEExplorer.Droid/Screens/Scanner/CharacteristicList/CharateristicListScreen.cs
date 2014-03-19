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
using Xamarin.Robotics.BluetoothLEExplorer.Droid.UI.Adapters;
using Xamarin.Robotics.BluetoothLEExplorer.Droid.UI;

namespace Xamarin.Robotics.BluetoothLEExplorer.Droid.Screens.Scanner.CharacteristicList
{
	[Activity]			
	public class CharacteristicListScreen : NoTitleActivityBase
	{
		protected ListView _listView;
		protected CharacteristicsAdapter _listAdapter;
		protected TextView _serviceNameText;
		protected TextView _serviceIDText;

		protected override void OnCreate (Bundle bundle)
		{
			base.OnCreate (bundle);

			// load our layout
			SetContentView (Resource.Layout.CharacteristicListScreen);

			// find our controls
			this._listView = FindViewById<ListView> (Resource.Id.CharacteristicListView);
			this._serviceNameText = FindViewById<TextView> (Resource.Id.ServiceNameText);
			this._serviceIDText = FindViewById<TextView> (Resource.Id.ServiceIDText);

			this._serviceNameText.Text = "Service Name Unknown";
			this._serviceIDText.Text = App.Current.State.SelectedService.ID.ToString();

			// create our list adapter
			this._listAdapter = new CharacteristicsAdapter (this, App.Current.State.SelectedService.Characteristics);
			this._listView.Adapter = this._listAdapter;

			this.WireUpLocalHandlers ();
		}

		protected override void OnResume ()
		{
			base.OnResume ();
			//this.WireUpExternalHandlers ();
		}

		protected override void OnPause ()
		{
			base.OnPause ();
			//this.RemoveExternalHandlers ();
		}

		protected void WireUpLocalHandlers ()
		{
			this._listView.ItemClick += (object sender, AdapterView.ItemClickEventArgs e) => {
				// set the selection
				this._listView.SetSelection(e.Position);

				// persist the selected service characteristic
				App.Current.State.SelectedCharacteristic = App.Current.State.SelectedService.Characteristics[e.Position];

				// launch the next
				StartActivity (typeof(Xamarin.Robotics.BluetoothLEExplorer.Droid.Screens.Scanner.CharacteristicDetail.CharacteristicDetailScreen));
			};
		}
	}
}

