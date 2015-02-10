using Robotics.Mobile.Core.Bluetooth.LE.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Robotics.Mobile.Core.Bluetooth.LE.LeTypeIds {

    /// <summary>Guids for the ReadBearLab Bluetooth board</summary>
    public class RedBearLabLE_Ids : IBluetoothLE_TypeSet {

        #region Data

        // From original LEStream
        private Guid serviceId = new Guid("713D0000-503E-4C75-BA94-3148F18D941E");
        private Guid transmitCharacteristicId = new Guid("713D0003-503E-4C75-BA94-3148F18D941E");
        private Guid receiveCharacteristicId = new Guid("713D0002-503E-4C75-BA94-3148F18D941E");
        private Guid modeCharacteristicId = new Guid("713D0004-503E-4C75-BA94-3148F18D941E");
        private List<BluetoothLE_Id> ids = new List<BluetoothLE_Id>();

        #endregion

        #region Constructors

        public RedBearLabLE_Ids() {
            this.ids.Add(new BluetoothLE_Id(BluetoothLE_IdType.Service, this.serviceId));
            this.ids.Add(new BluetoothLE_Id(BluetoothLE_IdType.ModeCharacteristic, this.modeCharacteristicId));
            this.ids.Add(new BluetoothLE_Id(BluetoothLE_IdType.ReceiveCharacteristic, this.receiveCharacteristicId));
            this.ids.Add(new BluetoothLE_Id(BluetoothLE_IdType.TransmitCharacteristic, this.transmitCharacteristicId));
        }

        #endregion

        #region Methods

        /// <summary>Get the LE Guid by type</summary>
        /// <param name="type">The guid type for service or characteristic</param>
        /// <returns>The id</returns>
        public Guid GetId(BluetoothLE_IdType type) {
            if (!this.HasId(type)) {
                throw new Exception(string.Format("Type id {0} does not exist for BlueRadio", type));
            }
            return this.ids.FirstOrDefault(x => x.IdType == type).Id;
        }


        /// <summary>Query if the id exists</summary>
        /// <param name="type">The guid type for service or characteristic</param>
        /// <returns>true if found, otherwise false</returns>
        public bool HasId(BluetoothLE_IdType type) {
            return this.ids.Exists(x => x.IdType == type);
        }

        #endregion

    }


}
