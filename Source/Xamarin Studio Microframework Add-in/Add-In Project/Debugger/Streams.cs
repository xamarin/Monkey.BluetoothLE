////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Copyright (c) Microsoft Corporation.  All rights reserved.
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Threading;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using System.Text;
using System.Management;
using Microsoft.Win32;
using Microsoft.Win32.SafeHandles;
using System.Net;
using System.Net.Sockets;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;

namespace Microsoft.SPOT.Debugger
{
	// This is an internal object implementing IAsyncResult with fields
	// for all of the relevant data necessary to complete the IO operation.
	// This is used by AsyncFSCallback and all of the async methods.
	unsafe internal class AsyncFileStream_AsyncResult : IAsyncResult
	{
		private unsafe static readonly IOCompletionCallback s_callback = new IOCompletionCallback(DoneCallback);
		internal AsyncCallback m_userCallback;
		internal Object m_userStateObject;
		internal ManualResetEvent m_waitHandle;
		internal GCHandle m_bufferHandle;
		// GCHandle to pin byte[].
		internal bool m_bufferIsPinned;
		// Whether our m_bufferHandle is valid.
		internal bool m_isWrite;
		// Whether this is a read or a write
		internal bool m_isComplete;
		internal bool m_EndXxxCalled;
		// Whether we've called EndXxx already.
		internal int m_numBytes;
		// number of bytes read OR written
		internal int m_errorCode;
		internal NativeOverlapped* m_overlapped;

		internal AsyncFileStream_AsyncResult(AsyncCallback userCallback, Object stateObject, bool isWrite)
		{
			m_userCallback = userCallback;
			m_userStateObject = stateObject;
			m_waitHandle = new ManualResetEvent(false);

			m_isWrite = isWrite;

			Overlapped overlapped = new Overlapped(0, 0, IntPtr.Zero, this);

			m_overlapped = overlapped.Pack(s_callback, null);            
		}

		public virtual Object AsyncState
		{
			get { return m_userStateObject; }
		}

		public bool IsCompleted
		{
			get { return m_isComplete; }
			set { m_isComplete = value; }
		}

		public WaitHandle AsyncWaitHandle
		{
			get { return m_waitHandle; }
		}

		public bool CompletedSynchronously
		{
			get { return false; }
		}

		internal void SignalCompleted()
		{
			AsyncCallback userCallback = null;

			lock(this)
			{
				if(m_isComplete == false)
				{
					userCallback = m_userCallback;

					ManualResetEvent wh = m_waitHandle;
					if(wh != null && wh.Set() == false)
					{
						Native.ThrowIOException(string.Empty);
					}

					// Set IsCompleted to true AFTER we've signalled the WaitHandle!
					// Necessary since we close the WaitHandle after checking IsCompleted,
					// so we could cause the SetEvent call to fail.
					m_isComplete = true;

					ReleaseMemory();
				}
			}

			if(userCallback != null)
			{
				userCallback(this);
			}
		}

		internal void WaitCompleted()
		{
			ManualResetEvent wh = m_waitHandle;
			if(wh != null)
			{
				if(m_isComplete == false)
				{
					wh.WaitOne();
					// There's a subtle race condition here.  In AsyncFSCallback,
					// I must signal the WaitHandle then set _isComplete to be true,
					// to avoid closing the WaitHandle before AsyncFSCallback has
					// signalled it.  But with that behavior and the optimization
					// to call WaitOne only when IsCompleted is false, it's possible
					// to return from this method before IsCompleted is set to true.
					// This is currently completely harmless, so the most efficient
					// solution of just setting the field seems like the right thing
					// to do.     -- BrianGru, 6/19/2000
					m_isComplete = true;
				}
				wh.Close();
			}
		}

		internal NativeOverlapped* OverlappedPtr
		{
			get { return m_overlapped; }
		}

		internal unsafe void ReleaseMemory()
		{
			if(m_overlapped != null)
			{
				Overlapped.Free(m_overlapped);
				m_overlapped = null;
			}

			UnpinBuffer();
		}

		internal void PinBuffer(byte[] buffer)
		{
			m_bufferHandle = GCHandle.Alloc(buffer, GCHandleType.Pinned);
			m_bufferIsPinned = true;
		}

		internal void UnpinBuffer()
		{
			if(m_bufferIsPinned)
			{
				m_bufferHandle.Free();
				m_bufferIsPinned = false;
			}
		}
		// this callback is called by a free thread in the threadpool when the IO operation completes.
		unsafe private static void DoneCallback(uint errorCode, uint numBytes, NativeOverlapped* pOverlapped)
		{
			if(errorCode == Native.ERROR_OPERATION_ABORTED)
			{
				numBytes = 0;
				errorCode = 0;

				return;
			}

			// Unpack overlapped
			Overlapped overlapped = Overlapped.Unpack(pOverlapped);
			// Free the overlapped struct in EndRead/EndWrite.

			// Extract async result from overlapped
			AsyncFileStream_AsyncResult asyncResult = (AsyncFileStream_AsyncResult)overlapped.AsyncResult;


			asyncResult.m_numBytes = (int)numBytes;
			asyncResult.m_errorCode = (int)errorCode;

			asyncResult.SignalCompleted();
		}
	}

	public class GenericAsyncStream : System.IO.Stream, IDisposable, WireProtocol.IStreamAvailableCharacters
	{
		protected SafeHandle m_handle;
		protected ArrayList m_outstandingRequests;

		protected GenericAsyncStream(SafeHandle handle)
		{
			System.Diagnostics.Debug.Assert(handle != null);

			m_handle = handle;

			if(ThreadPool.BindHandle(m_handle) == false)
			{
				throw new IOException("BindHandle Failed");
			}

			m_outstandingRequests = ArrayList.Synchronized(new ArrayList());
		}

		~GenericAsyncStream()
		{
			Dispose(false);
		}

		public void CancelPendingIO()
		{
			lock(m_outstandingRequests.SyncRoot)
			{
				for(int i = m_outstandingRequests.Count - 1; i >= 0; i--)
				{
					AsyncFileStream_AsyncResult asfar = (AsyncFileStream_AsyncResult)m_outstandingRequests[i];
					asfar.SignalCompleted();
				}

				m_outstandingRequests.Clear();
			}
		}

		protected override void Dispose(bool disposing)
		{            
			// Nothing will be done differently based on whether we are disposing vs. finalizing.
			lock(this)
			{
				if(m_handle != null && !m_handle.IsInvalid)
				{
					if(disposing)
					{
						CancelPendingIO();
					}

					m_handle.Close();
					m_handle.SetHandleAsInvalid();
				}                
			}

			base.Dispose(disposing);
		}

		public override bool CanRead
		{
			get { return true; }
		}

		public override bool CanSeek
		{
			get { return false; }
		}

		public override bool CanWrite
		{
			get { return true; }
		}

		public override long Length
		{
			get { throw NotImplemented(); }
		}

		public override long Position
		{
			get { throw NotImplemented(); }
			set { throw NotImplemented(); }
		}

		public override IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback callback, object state)
		{
			return BeginReadCore(buffer, offset, count, callback, state);
		}

		public override IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback callback, object state)
		{
			return BeginWriteCore(buffer, offset, count, callback, state);
		}

		public override void Close()
		{
			Dispose(true);
		}

		public override int EndRead(IAsyncResult asyncResult)
		{
			AsyncFileStream_AsyncResult afsar = CheckParameterForEnd(asyncResult, false);

			afsar.WaitCompleted();

			m_outstandingRequests.Remove(afsar);

			// Now check for any error during the read.
			if(afsar.m_errorCode != 0)
				throw new IOException("Async Read failed", afsar.m_errorCode);

			return afsar.m_numBytes;
		}

		public override void EndWrite(IAsyncResult asyncResult)
		{
			AsyncFileStream_AsyncResult afsar = CheckParameterForEnd(asyncResult, true);

			//afsar.WaitCompleted();

			afsar.m_waitHandle.WaitOne(afsar.m_numBytes);

			m_outstandingRequests.Remove(afsar);

			// Now check for any error during the write.
			if(afsar.m_errorCode != 0)
				throw new IOException("Async Write failed", afsar.m_errorCode);
		}

		public override void Flush()
		{
		}

		public override int Read(byte[] buffer, int offset, int count)
		{
			IAsyncResult result = BeginRead(buffer, offset, count, null, null);
			return EndRead(result);
		}

		public override long Seek(long offset, SeekOrigin origin)
		{
			throw NotImplemented();
		}

		public override void SetLength(long value)
		{
			throw NotImplemented();
		}

		public override void Write(byte[] buffer, int offset, int count)
		{
			IAsyncResult result = BeginWrite(buffer, offset, count, null, null);
			EndWrite(result);
		}

		public SafeHandle Handle
		{
			get
			{
				return m_handle;
			}
		}

		public virtual int AvailableCharacters
		{
			get
			{
				return 0;
			}
		}

		private Exception NotImplemented()
		{
			return new NotSupportedException("Not Supported");
		}

		internal void CheckParametersForBegin(byte[] array, int offset, int count)
		{
			if(array == null)
				throw new ArgumentNullException("array");

			if(offset < 0)
				throw new ArgumentOutOfRangeException("offset");

			if(count < 0 || array.Length - offset < count)
				throw new ArgumentOutOfRangeException("count");

			if(m_handle.IsInvalid)
			{
				throw new ObjectDisposedException(null);
			}
		}

		internal AsyncFileStream_AsyncResult CheckParameterForEnd(IAsyncResult asyncResult, bool isWrite)
		{
			if(asyncResult == null)
				throw new ArgumentNullException("asyncResult");

			AsyncFileStream_AsyncResult afsar = asyncResult as AsyncFileStream_AsyncResult;
			if(afsar == null || afsar.m_isWrite != isWrite)
				throw new ArgumentException("asyncResult");
			if(afsar.m_EndXxxCalled)
				throw new InvalidOperationException("EndRead called twice");
			afsar.m_EndXxxCalled = true;

			return afsar;
		}

		private unsafe IAsyncResult BeginReadCore(byte[] array, int offset, int count, AsyncCallback userCallback, Object stateObject)
		{
			CheckParametersForBegin(array, offset, count);

			AsyncFileStream_AsyncResult asyncResult = new AsyncFileStream_AsyncResult(userCallback, stateObject, false);

			if(count == 0)
			{
				asyncResult.SignalCompleted();
			}
			else
			{
				// Keep the array in one location in memory until the OS writes the
				// relevant data into the array.  Free GCHandle later.
				asyncResult.PinBuffer(array);

				fixed(byte* p = array)
				{
					int numBytesRead = 0;
					bool res;

					res = Native.ReadFile(m_handle.DangerousGetHandle(), p + offset, count, out numBytesRead, asyncResult.OverlappedPtr);
					if(res == false)
					{
						if(HandleErrorSituation("BeginRead", false))
						{
							asyncResult.SignalCompleted();
						}
						else
						{
							m_outstandingRequests.Add(asyncResult);
						}
					}                    
				}
			}

			return asyncResult;
		}

		private unsafe IAsyncResult BeginWriteCore(byte[] array, int offset, int count, AsyncCallback userCallback, Object stateObject)
		{
			CheckParametersForBegin(array, offset, count);

			AsyncFileStream_AsyncResult asyncResult = new AsyncFileStream_AsyncResult(userCallback, stateObject, true);

			if(count == 0)
			{
				asyncResult.SignalCompleted();
			}
			else
			{
				asyncResult.m_numBytes = count;

				// Keep the array in one location in memory until the OS writes the
				// relevant data into the array.  Free GCHandle later.
				asyncResult.PinBuffer(array);

				fixed(byte* p = array)
				{
					int numBytesWritten = 0;
					bool res;

					res = Native.WriteFile(m_handle.DangerousGetHandle(), p + offset, count, out numBytesWritten, asyncResult.OverlappedPtr);
					if(res == false)
					{
						if(HandleErrorSituation("BeginWrite", true))
						{
							asyncResult.SignalCompleted();
						}
						else
						{
							m_outstandingRequests.Add(asyncResult);
						}
					}
				}
			}

			return asyncResult;
		}

		protected virtual bool HandleErrorSituation(string msg, bool isWrite)
		{
			int hr = Marshal.GetLastWin32Error();

			// For invalid handles, detect the error and close ourselves
			// to prevent a malicious app from stealing someone else's file
			// handle when the OS recycles the handle number.
			if(hr == Native.ERROR_INVALID_HANDLE)
			{
				m_handle.Close();
			}

			if(hr != Native.ERROR_IO_PENDING)
			{
				if(isWrite == false && hr == Native.ERROR_HANDLE_EOF)
				{
					throw new EndOfStreamException(msg);
				}

				throw new IOException(msg, hr);
			}

			return false;
		}

		#region IDisposable Members

		void IDisposable.Dispose()
		{
			base.Dispose(true);

			Dispose(true);

			GC.SuppressFinalize(this);
		}

		#endregion

	}

	public class AsyncFileStream : GenericAsyncStream
	{
		private string m_fileName = null;

		public AsyncFileStream(string file, System.IO.FileShare share)
            : base(OpenHandle(file, share))
		{
			m_fileName = file;
		}

		static private SafeFileHandle OpenHandle(string file, System.IO.FileShare share)
		{
			if(file == null || file.Length == 0)
			{
				throw new ArgumentNullException("file");
			}

			SafeFileHandle handle = Native.CreateFile(file, Native.GENERIC_READ | Native.GENERIC_WRITE, share, Native.NULL, System.IO.FileMode.Open, Native.FILE_FLAG_OVERLAPPED, Native.NULL);
            
			if(handle.IsInvalid)
			{
				throw new InvalidOperationException(String.Format("Cannot open {0}", file));
			}

			return handle;
		}

		public String Name
		{
			get
			{
				return m_fileName;
			}
		}

		public unsafe override int AvailableCharacters
		{
			get
			{
				int bytesRead;
				int totalBytesAvail;
				int bytesLeftThisMessage;

				if(Native.PeekNamedPipe(m_handle.DangerousGetHandle(), (byte*)Native.NULL, 0, out bytesRead, out totalBytesAvail, out bytesLeftThisMessage) == false)
				{
					totalBytesAvail = 1;
				}

				return totalBytesAvail;
			}
		}
	}

	public class AsyncSerialStream : AsyncFileStream
	{
		public AsyncSerialStream(string port, uint baudrate) : base(port, System.IO.FileShare.None)
		{
			Native.COMMTIMEOUTS cto = new Native.COMMTIMEOUTS();
			cto.Initialize();
			Native.DCB dcb = new Native.DCB();
			dcb.Initialize();

			Native.GetCommState(m_handle.DangerousGetHandle(), ref dcb);

			dcb.BaudRate = baudrate;
			dcb.ByteSize = 8;
			dcb.StopBits = 0;

			dcb.__BitField = 0;
			dcb.__BitField &= ~Native.DCB.mask_fDtrControl;
			dcb.__BitField &= ~Native.DCB.mask_fRtsControl;
			dcb.__BitField |= Native.DCB.mask_fBinary;
			dcb.__BitField &= ~Native.DCB.mask_fParity;
			dcb.__BitField &= ~Native.DCB.mask_fOutX;
			dcb.__BitField &= ~Native.DCB.mask_fInX;
			dcb.__BitField &= ~Native.DCB.mask_fErrorChar;
			dcb.__BitField &= ~Native.DCB.mask_fNull;
			dcb.__BitField |= Native.DCB.mask_fAbortOnError;

			Native.SetCommState(m_handle.DangerousGetHandle(), ref dcb);

			Native.SetCommTimeouts(m_handle.DangerousGetHandle(), ref cto);
		}

		public override int AvailableCharacters
		{
			get
			{
				Native.COMSTAT cs = new Native.COMSTAT();
				cs.Initialize();
				uint errors;

				Native.ClearCommError(m_handle.DangerousGetHandle(), out errors, ref cs);

				return (int)cs.cbInQue;
			}
		}

		protected override bool HandleErrorSituation(string msg, bool isWrite)
		{
			if(Marshal.GetLastWin32Error() == Native.ERROR_OPERATION_ABORTED)
			{
				Native.COMSTAT cs = new Native.COMSTAT();
				cs.Initialize();
				uint errors;

				Native.ClearCommError(m_handle.DangerousGetHandle(), out errors, ref cs);

				return true;
			}

			return base.HandleErrorSituation(msg, isWrite);
		}

		public void ConfigureXonXoff(bool fEnable)
		{
			Native.DCB dcb = new Native.DCB();
			dcb.Initialize();

			Native.GetCommState(m_handle.DangerousGetHandle(), ref dcb);

			if(fEnable)
			{
				dcb.__BitField |= Native.DCB.mask_fOutX;
			}
			else
			{
				dcb.__BitField &= ~Native.DCB.mask_fOutX;
			}

			Native.SetCommState(m_handle.DangerousGetHandle(), ref dcb);
		}

		static public PortDefinition[] EnumeratePorts()
		{
			SortedList lst = new SortedList();

			try
			{
				RegistryKey key = Registry.LocalMachine.OpenSubKey(@"HARDWARE\DEVICEMAP\SERIALCOMM");

				foreach(string name in key.GetValueNames())
				{
					string val = (string)key.GetValue(name);
					PortDefinition pd = PortDefinition.CreateInstanceForSerial(val, @"\\.\" + val, 115200);

					lst.Add(val, pd);
				}
			}
			catch
			{
			}

			ICollection col = lst.Values;
			PortDefinition[] res = new PortDefinition[col.Count];

			col.CopyTo(res, 0);

			return res;
		}
	}

	public class AsyncNetworkStream : /*NetworkStream,*/Stream,  WireProtocol.IStreamAvailableCharacters //, IDisposable
	{
		Socket m_socket = null;
		NetworkStream m_ns = null;
		SslStream m_ssl = null;

		public AsyncNetworkStream(Socket socket, bool ownsSocket)
            //: base(socket, ownsSocket)
		{
			m_socket = socket;
			m_ns = new NetworkStream(socket);
		}

		public bool IsUsingSsl { get { return m_ssl != null; } }

		public override bool CanRead { get { return true; } }

		public override bool CanSeek { get { return false; } }

		public override bool CanWrite { get { return true; } }

		public override long Length { get { throw new NotSupportedException(); } }

		public override long Position { get { throw new NotSupportedException(); } set { throw new NotSupportedException(); } }

		public override void Flush()
		{
			if(m_ssl != null)
			{
				m_ssl.Flush();
			}
			else if(m_ns != null)
			{
				m_ns.Flush();
			}
			else
			{
				throw new InvalidOperationException();
			}
		}

		public override int Read(byte[] buffer, int offset, int count)
		{
			if(m_ssl != null)
			{
				return m_ssl.Read(buffer, offset, count);
			}
			else if(m_ns != null)
			{
				return m_ns.Read(buffer, offset, count);
			}

			throw new InvalidOperationException();
		}

		public override long Seek(long offset, SeekOrigin origin)
		{
			throw new NotSupportedException();
		}

		public override void SetLength(long value)
		{
			throw new NotSupportedException();
		}

		public override void Write(byte[] buffer, int offset, int count)
		{
			if(m_ssl != null)
			{
				m_ssl.Write(buffer, offset, count);
			}
			else if(m_ns != null)
			{
				m_ns.Write(buffer, offset, count);
			}
			else
			{
				throw new InvalidOperationException();
			}
		}

		protected override void Dispose(bool disposing)
		{
			if(disposing)
			{
				if(m_ssl != null)
					m_ssl.Dispose();
				if(m_ns != null)
					m_ns.Dispose();
				if(m_socket != null)
					m_socket.Close();
			}

			base.Dispose(disposing);
		}

		internal IAsyncResult BeginUpgradeToSSL(X509Certificate2 cert, bool requiresClientCert)
		{
			m_ssl = new SslStream(m_ns, true);

			return m_ssl.BeginAuthenticateAsServer(cert, requiresClientCert, System.Security.Authentication.SslProtocols.Tls, true, null, null);
		}

		internal bool EndUpgradeToSSL(IAsyncResult iar)
		{
			m_ssl.EndAuthenticateAsServer(iar);

			return iar.IsCompleted;
		}

		#region IStreamAvailableCharacters

		int WireProtocol.IStreamAvailableCharacters.AvailableCharacters
		{
			get
			{
				return m_socket.Available;
			}
		}

		#endregion

	}
}
