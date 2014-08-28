using System;
using System.Linq;
using System.Threading.Tasks;

namespace Xamarin.Robotics.Mobile.Core.Bluetooth.LE
{
	public static class DeviceEx
	{
		/// <summary>
		/// Asynchronously gets the requested service
		/// </summary>
		public static Task<IService> GetServiceAsync (this IDevice device, Guid id)
		{
			var existing = device.Services.FirstOrDefault (x => x.ID == id);
			if (existing != null)
				return Task.FromResult (existing);

			var tcs = new TaskCompletionSource<IService> ();
			EventHandler h = null;
			h = (sender, e) => {
				var s = device.Services.FirstOrDefault (x => x.ID == id);
				if (s != null) {
					device.ServicesDiscovered -= h;
					tcs.SetResult (s);
				}
			};
			device.ServicesDiscovered += h;
			device.DiscoverServices ();
			return tcs.Task;
		}

		/// <summary>
		/// Asynchronously gets the requested service
		/// </summary>
		public static Task<ICharacteristic> GetCharacteristicAsync (this IService service, Guid id)
		{
			var existing = service.Characteristics.FirstOrDefault (x => x.ID == id);
			if (existing != null)
				return Task.FromResult (existing);

			var tcs = new TaskCompletionSource<ICharacteristic> ();
			EventHandler h = null;
			h = (sender, e) => {
				var s = service.Characteristics.FirstOrDefault (x => x.ID == id);
				if (s != null) {
					service.CharacteristicsDiscovered -= h;
					tcs.SetResult (s);
				}
			};
			service.CharacteristicsDiscovered += h;
			service.DiscoverCharacteristics ();
			return tcs.Task;
		}
	}
}

