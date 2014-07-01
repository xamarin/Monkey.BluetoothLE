using System;
using System.Collections.Generic;
using System.Text;
using Android.Bluetooth;
using System.Linq;
using Java.Util;
using Android.Media;
using System.Threading.Tasks;

namespace Xamarin.Robotics.Core.Bluetooth.LE
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
					if(e.Characteristic.ID == this.ID) {
						// update our underlying characteristic (this one will have a value)
						//TODO: is this necessary? probably the underlying reference is the same.
						//this._nativeCharacteristic = e.Characteristic;

						this.ValueUpdated (this, e);
					}
				};
			}
		}

		public string Uuid {
			get { return this._nativeCharacteristic.Uuid.ToString (); }
		}

		public Guid ID {
			get { return Guid.Parse( this._nativeCharacteristic.Uuid.ToString() ); }
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
				if (this._descriptors != null) {
					this._descriptors = new List<IDescriptor> ();
					// convert the internal list of them to the xplat ones
					foreach (var item in this._nativeCharacteristic.Descriptors) {
						this._descriptors.Add (new Descriptor (item));
					}
				}
				return this._descriptors;
			}
		} protected IList<IDescriptor> _descriptors;

		public object NativeCharacteristic {
			get {
				return this._nativeCharacteristic;
			}
		}

		public Task<ICharacteristic> ReadAsync()
		{
			//TODO: implement async read for Android
			throw new NotImplementedException ("TODO");
		}

		public void RequestValue ()
		{
			// TODO: should be bool RequestValue? compare iOS API for commonality
			bool successful = false;
			if((this.Properties & CharacteristicPropertyType.Read) != 0) {
				Console.WriteLine ("Characteristic.RequestValue, PropertyType = Read, requesting updates");
				successful = this._gatt.ReadCharacteristic (this._nativeCharacteristic);
			}
			if ((this.Properties & CharacteristicPropertyType.Notify) != 0) {
				Console.WriteLine ("Characteristic.RequestValue, PropertyType = Notify, requesting updates");
				
				successful = this._gatt.SetCharacteristicNotification (this._nativeCharacteristic, true);

				// [TO20131211@1634] It seems that setting the notification above isn't enough. You have to set the NOTIFY
				// descriptor as well, otherwise the receiver will never get the updates. I just grabbed the first (and only)
				// descriptor that is associated with the characteristic, which is the NOTIFY descriptor. This seems like a really
				// odd way to do things to me, but I'm a Bluetooth newbie. Google has a example here (but ono real explaination as
				// to what is going on):
				// http://developer.android.com/guide/topics/connectivity/bluetooth-le.html#notification
				BluetoothGattDescriptor descriptor = _nativeCharacteristic.Descriptors[0];
				descriptor.SetValue(BluetoothGattDescriptor.EnableNotificationValue.ToArray());
				_gatt.WriteDescriptor(descriptor);
			}

			Console.WriteLine ("RequestValue, Succesful: " + successful.ToString());
		}
	}
}

