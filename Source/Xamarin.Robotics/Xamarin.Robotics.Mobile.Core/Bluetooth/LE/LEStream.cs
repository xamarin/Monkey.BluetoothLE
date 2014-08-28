using System;
using System.IO;
using System.Threading.Tasks;
using System.Linq;

namespace Xamarin.Robotics.Mobile.Core.Bluetooth.LE
{
	public class LEStream : Stream
	{
		readonly Task initTask;

		readonly IDevice device;
		IService service;

		static readonly Guid SerialServiceId = new Guid ("713D0000-503E-4C75-BA94-3148F18D941E");

		public LEStream (IDevice device)
		{
			this.device = device;
			initTask = InitializeAsync ();
		}

		async Task InitializeAsync ()
		{
			service = await device.GetServiceAsync (SerialServiceId);
		}



		#region implemented abstract members of Stream

		public override int Read (byte[] buffer, int offset, int count)
		{
			initTask.Wait ();
			throw new NotImplementedException ();
		}

		public override void Write (byte[] buffer, int offset, int count)
		{
			initTask.Wait ();
			throw new NotImplementedException ();
		}

		public override void Flush ()
		{
			throw new NotImplementedException ();
		}

		public override long Seek (long offset, SeekOrigin origin)
		{
			throw new NotSupportedException ();
		}
		public override void SetLength (long value)
		{
			throw new NotSupportedException ();
		}
		public override bool CanRead {
			get {
				return true;
			}
		}
		public override bool CanSeek {
			get {
				return false;
			}
		}
		public override bool CanWrite {
			get {
				return true;
			}
		}
		public override long Length {
			get {
				return 0;
			}
		}
		public override long Position {
			get {
				return 0;
			}
			set {
			}
		}
		#endregion
	}
}

