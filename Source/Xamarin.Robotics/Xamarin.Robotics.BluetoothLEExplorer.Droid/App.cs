using System;
using Xamarin.Robotics.Core.Bluetooth.LE;

namespace Xamarin.Robotics.BluetoothLEExplorer.Droid
{
	public class App
	{
		public static App Current {
			get { return _current; }
		} private static App _current;

		public State State {
			get { return this._state; }
		}
		protected State _state;

		public Adapter BleAdapter {
			get { return this._bleAdapter; }
		} protected Adapter _bleAdapter;

		protected App ()
		{
			this._state = new State ();

			//TODO: this should be asynchronous, and when it's initialized, need to 
			// call State.WireUpBleEvents
			// also, do the whole app initialization thing
			this._bleAdapter = new Adapter ();
		}

		static App ()
		{
			_current = new App ();
		}


	}
}

