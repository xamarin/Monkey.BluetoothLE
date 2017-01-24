using System;
using Xunit;
using System.Threading.Tasks;
using Robotics.Mobile.Core.Bluetooth.LE;
using System.Collections.Generic;

namespace Robotics.Mobile.Core.Tests
{
    [Collection("IntegrationTest")]
    public class CharacteristicTest
    {
        DeviceFixture fixture;

        public CharacteristicTest(DeviceFixture fixture)
        {
            this.fixture = fixture;
        }

        [Fact]
        public async Task WriteAsyncTest()
        {
            IDevice device = await this.fixture.GetDeviceAsync();
            device = await Extensions.ConnectAsync(DeviceFixture.Adapter, device);
            IService heartRateService = await device.GetServiceAsync(Constants.TEST_SERVICE_UUID);
            ICharacteristic characteristic = await heartRateService.GetCharacteristicAsync(Constants.TEST_CHARACTERISTIC_UUID);
            var test_collection = new List<byte[]>();
            test_collection.Add(new byte[1] { 0xA });
            test_collection.Add(new byte[1] { 0xB });
            foreach (var test_data in test_collection)
            {
                await characteristic.WriteAsync(test_data);
                characteristic = await characteristic.ReadAsync();
                var old_data = characteristic.Value;
                Assert.Equal(test_data, old_data);
            }
        }
    }
}