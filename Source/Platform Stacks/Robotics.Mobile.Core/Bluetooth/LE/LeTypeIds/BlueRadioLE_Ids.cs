using Robotics.Mobile.Core.Bluetooth.LE.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Robotics.Mobile.Core.Bluetooth.LE.LeTypeIds {

    /// <summary>Guids for the BlueRadio Bluetooth board</summary>
    public class BlueRadioLE_Ids : IBluetoothLE_TypeSet {

        #region Data

        //flipped the transmit and receive from this reference
        //https://github.com/RHWorkshop/IR-Blue-iPhone/blob/master/IR-Blue%20Version%201.0.11/IR-Blue/IR-Blue/RHViewController.m
        private Guid serviceId = new Guid("DA2B84F1-6279-48DE-BDC0-AFBEA0226079");
        private Guid transmitCharacteristicId = new Guid("BF03260C-7205-4C25-AF43-93B1C299D159");
        private Guid receiveCharacteristicId = new Guid("18CDA784-4BD3-4370-85BB-BFED91EC86AF");
        private Guid modeCharacteristicId = new Guid("A87988B9-694C-479C-900E-95DFA6C00A24");
        private List<BluetoothLE_Id> ids = new List<BluetoothLE_Id>();

        #endregion

        #region Constructors

        public BlueRadioLE_Ids() {
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
