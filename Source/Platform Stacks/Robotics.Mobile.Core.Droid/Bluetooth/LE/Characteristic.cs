using System;
using System.Collections.Generic;
using System.Text;
using Android.Bluetooth;
using System.Linq;
using Java.Util;
using Android.Media;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using System.Threading;

namespace Robotics.Mobile.Core.Bluetooth.LE
{
	public class Characteristic : ICharacteristic
	{

		public event EventHandler<CharacteristicReadEventArgs> ValueUpdated = delegate {};


		protected BluetoothGattCharacteristic _nativeCharacteristic;
		/// <summary>
		/// we have to keep a reference to this because Android's api is weird and requires
		/// the GattServer in order to do nearly anything, including enumerating services
		/// </summary>
		protected BluetoothGatt _gatt;
		/// <summary>
		/// we also track this because of gogole's weird API. the gatt callback is where
		/// we'll get notified when services are enumerated
		/// </summary>
		protected GattCallback _gattCallback;


		public Characteristic (BluetoothGattCharacteristic nativeCharacteristic, BluetoothGatt gatt, GattCallback gattCallback)
		{
			this._nativeCharacteristic = nativeCharacteristic;
			this._gatt = gatt;
			this._gattCallback = gattCallback;

			if (this._gattCallback != null) {
				// wire up the characteristic value updating on the gattcallback
				this._gattCallback.CharacteristicValueUpdated += (object sender, CharacteristicReadEventArgs e) => {
					// it may be other characteristics, so we need to test

					if (e.Characteristic.ID == this.ID) {
						// update our underlying characteristic (this one will have a value)
						try {
							this.ValueUpdated (this, e);
						} catch (Exception error) {
							Console.WriteLine ("Characteristic.ValueUpdated on droid exception" + error);
						}
					}
					Console.WriteLine ("Characteristic.droid read" + e.Characteristic.Value);
				};
			}
		}

		public string Uuid {
			get { return this._nativeCharacteristic.Uuid.ToString (); }
		}

		public Guid ID {
			get { return Guid.Parse (this._nativeCharacteristic.Uuid.ToString ()); }
		}

		public byte[] Value {
			get { return this._nativeCharacteristic.GetValue (); }
		}

		public string StringValue {
			get {
				if (this.Value == null)
					return String.Empty;
				else
					return System.Text.Encoding.UTF8.GetString (this.Value);
			}
		}

		public string Name {
			get { return KnownCharacteristics.Lookup (this.ID).Name; }
		}

		public CharacteristicPropertyType Properties {
			get {
				return (CharacteristicPropertyType)(int)this._nativeCharacteristic.Properties;
			}
		}

		public IList<IDescriptor> Descriptors {
			get {
				// if we haven't converted them to our xplat objects
				if (this._descriptors == null) {
					this._descriptors = new List<IDescriptor> ();
					// convert the internal list of them to the xplat ones
					foreach (var item in this._nativeCharacteristic.Descriptors) {
						this._descriptors.Add (new Descriptor (item));
					}
				}
				return this._descriptors;
			}
		}

		protected IList<IDescriptor> _descriptors;

		public object NativeCharacteristic {
			get {
				return this._nativeCharacteristic;
			}
		}

		public bool CanRead { get { return (this.Properties & CharacteristicPropertyType.Read) != 0; } }

		public bool CanUpdate { get { return (this.Properties & CharacteristicPropertyType.Notify) != 0; } }

		public bool CanWrite { get { return (this.Properties & CharacteristicPropertyType.WriteWithoutResponse | CharacteristicPropertyType.AppleWriteWithoutResponse) != 0; } }

		public void Write (byte[] data)
		{
			if (!CanWrite) {
				throw new InvalidOperationException ("Characteristic does not support WRITE");
			}

			var c = _nativeCharacteristic;
			c.SetValue (data);
			this._gatt.WriteCharacteristic (c);
			Console.WriteLine (".....Write Message: " + BitConverter.ToString (c.GetValue ()));
		}

		public Task<ICharacteristic> ReadAsync ()
		{
			var tcs = new TaskCompletionSource<ICharacteristic> ();

			if (!CanRead) {
				throw new InvalidOperationException ("Characteristic does not support READ");
			}
			EventHandler<CharacteristicReadEventArgs> updated = null;
			updated = (object sender, CharacteristicReadEventArgs e) => {
				// it may be other characteristics, so we need to test
				var c = e.Characteristic;
				tcs.SetResult (c);
				if (this._gattCallback != null) {
					// wire up the characteristic value updating on the gattcallback
					this._gattCallback.CharacteristicValueUpdated -= updated;
				}

				Console.WriteLine (".....CharacteristicValueUpdated.droid.ReadAsync: " + BitConverter.ToString (c.Value));

			};

			if (this._gattCallback != null) {
				// wire up the characteristic value updating on the gattcallback
				this._gattCallback.CharacteristicValueUpdated += updated;
			}

			Console.WriteLine (".....ReadAsync ");

			this._gatt.ReadCharacteristic (this._nativeCharacteristic);

			return tcs.Task;
		}

		public void StartUpdates ()
		{

			bool successful = false;
			if (CanRead) {
				Console.WriteLine ("Characteristic.RequestValue, PropertyType = Read, requesting updates");
				successful = this._gatt.ReadCharacteristic (this._nativeCharacteristic);
			}

			if (CanUpdate || CanWrite) {
				Console.WriteLine ("Characteristic.RequestValue, PropertyType = Notify, requesting updates");

				successful = this._gatt.SetCharacteristicNotification (this._nativeCharacteristic, true);

				// [TO20131211@1634] It seems that setting the notification above isn't enough. You have to set the NOTIFY
				// descriptor as well, otherwise the receiver will never get the updates. I just grabbed the first (and only)
				// descriptor that is associated with the characteristic, which is the NOTIFY descriptor. This seems like a really
				// odd way to do things to me, but I'm a Bluetooth newbie. Google has a example here (but ono real explaination as
				// to what is going on):
				// http://developer.android.com/guide/topics/connectivity/bluetooth-le.html#notification
				//

				if (successful == false) {
					Console.WriteLine ("Characteristic.SetCharacteristicNotification failed!");
				}

				if (_nativeCharacteristic.Descriptors.Count > 0) {

					// Loop through descriptors of the characteristic
					foreach (BluetoothGattDescriptor _descriptor in _nativeCharacteristic.Descriptors) {

						BluetoothGattDescriptor descriptor = _descriptor;
						descriptor.SetValue (BluetoothGattDescriptor.EnableNotificationValue.ToArray ());

						// Make ure the discriptor has bytes.
						if (descriptor.GetValue ().Length > 0) {

							/*
								setup the discriptor after a wait to make sure SetCharacteristicNotification has time to initialize
								Otherwise a Read- notify in forms will fail here
								http://developer.android.com/guide/topics/connectivity/bluetooth-le.html#notification
							*/
							Task.Delay (2000).Wait (); // wait after a valid discriptor is found. 1500 miliseconds fails, 2000 passes in forms.
							_gatt.WriteDescriptor (_descriptor);
							break;
						}

					}

				} else {
					Console.WriteLine ("RequestValue, FAILED: _nativeCharacteristic.Descriptors was empty, not sure why");
				}
			}

		}

		public void StopUpdates ()
		{
			bool successful = false;
			if (CanUpdate || CanRead) {
				successful = this._gatt.SetCharacteristicNotification (this._nativeCharacteristic, false);
				Console.WriteLine ("Characteristic.RequestValue, PropertyType = Notify, STOP updates");
			}
		}
	}
}