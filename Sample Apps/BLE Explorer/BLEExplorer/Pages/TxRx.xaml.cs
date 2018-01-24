using Robotics.Mobile.Core.Bluetooth.LE;
using Xamarin.Forms;

namespace BLEExplorer.Pages
{
    public partial class TxRx : ContentPage
    {
        ICharacteristic characteristic;
        public TxRx(ICharacteristic characteristic)
        {
            InitializeComponent();

            this.characteristic = characteristic;

            var result = characteristic.Properties & CharacteristicPropertyType.Notify;

            if(characteristic.CanRead)
            {
                characteristic.StartUpdates();
                characteristic.ValueUpdated += CharacteristicValueUpdated;
            }

            if ((characteristic.Properties & CharacteristicPropertyType.Notify) != 0)
            {
                
            }

            btnSend.Clicked += BtnSendClicked;
        }

        void BtnSendClicked(object sender, System.EventArgs e)
        {
            var data = System.Text.Encoding.UTF8.GetBytes(entryMessage.Text);

            if (characteristic.CanWrite)
                characteristic.Write(data);
        }

        private void CharacteristicValueUpdated(object sender, CharacteristicReadEventArgs e)
        {
            string msg = string.Empty;

            if(switchShowAsText.IsToggled == true)
            {
                msg = new string(System.Text.Encoding.UTF8.GetChars(e.Characteristic.Value));
            }
            else
            {
                var count = e.Characteristic.Value.Length;

                for (var i = 0; i < count; i++)
                {
                    msg += e.Characteristic.Value[i] + " ";
                }
            }

            Device.BeginInvokeOnMainThread(()=> entryReceived.Text = msg);
        }
    }
}