////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Copyright (c) Microsoft Corporation.  All rights reserved.
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

using System;
using System.Collections;
using System.IO;
using System.Text;
using System.Reflection;
using System.Threading;
using System.Runtime.Serialization;
using System.Runtime.CompilerServices;
using DEBUG = System.Diagnostics.Debug;

namespace Microsoft.SPOT.Debugger.WireProtocol
{
	public interface IControllerHost
	{
		void SpuriousCharacters(byte[] buf, int offset, int count);

		void ProcessExited();
	}

	public interface IController
	{
		DateTime LastActivity { get; }

		bool IsPortConnected { get; }

		Packet NewPacket();

		bool QueueOutput(MessageRaw raw);

		void SendRawBuffer(byte[] buf);

		void ClosePort();

		void Start();

		void StopProcessing();

		void ResumeProcessing();

		void Stop();

		uint GetUniqueEndpointId();

		CLRCapabilities Capabilities { get; set; }
	}

	public interface IControllerHostLocal : IControllerHost
	{
		Stream OpenConnection();

		bool ProcessMessage(IncomingMessage msg, bool fReply);
	}

	public interface IControllerLocal : IController
	{
		Stream OpenPort();
	}

	public interface IStreamAvailableCharacters
	{
		int AvailableCharacters { get; }
	}

	public interface IControllerHostRemote : IControllerHost
	{
		bool ProcessMessage(byte[] header, byte[] payload, bool fReply);
	}

	public interface IControllerRemote : IController
	{
		bool RegisterEndpoint(uint epType, uint epId);

		void DeregisterEndpoint(uint epType, uint epId);
	}

	internal class FifoBuffer
	{
		byte[] m_buffer;
		int m_offset;
		int m_count;
		ManualResetEvent m_ready;

		public FifoBuffer()
		{
			m_buffer = new byte[1024];
			m_offset = 0;
			m_count = 0;
			m_ready = new ManualResetEvent(false);
		}

		public WaitHandle WaitHandle
		{
			get { return m_ready; }
		}

		[MethodImplAttribute(MethodImplOptions.Synchronized)]
		public int Read(byte[] buf, int offset, int count)
		{
			int countRequested = count;

			int len = m_buffer.Length;

			while(m_count > 0 && count > 0)
			{
				int avail = m_count;
				if(avail + m_offset > len)
					avail = len - m_offset;

				if(avail > count)
					avail = count;

				Array.Copy(m_buffer, m_offset, buf, offset, avail);

				m_offset += avail;
				if(m_offset == len)
					m_offset = 0;
				offset += avail;

				m_count -= avail;
				count -= avail;
			}

			if(m_count == 0)
			{
				//
				// No pending data, resync to the beginning of the buffer.
				//
				m_offset = 0;

				m_ready.Reset();
			}

			return countRequested - count;
		}

		[MethodImplAttribute(MethodImplOptions.Synchronized)]
		public void Write(byte[] buf, int offset, int count)
		{
			while(count > 0)
			{
				int len = m_buffer.Length;
				int avail = len - m_count;

				if(avail == 0) // Buffer full. Expand it.
				{
					byte[] buffer = new byte[len * 2];

					//
					// Double the buffer and copy all the data to the left side.
					//
					Array.Copy(m_buffer, m_offset, buffer, 0, len - m_offset);
					Array.Copy(m_buffer, 0, buffer, len - m_offset, m_offset);

					m_buffer = buffer;
					m_offset = 0;
					len *= 2;
					avail = len;
				}

				int offsetWrite = m_offset + m_count;
				if(offsetWrite >= len)
					offsetWrite -= len;

				if(avail + offsetWrite > len)
					avail = len - offsetWrite;

				if(avail > count)
					avail = count;

				Array.Copy(buf, offset, m_buffer, offsetWrite, avail);

				offset += avail;
				m_count += avail;
				count -= avail;
			}

			m_ready.Set();
		}

		public int Available
		{
			[MethodImplAttribute( MethodImplOptions.Synchronized )]
            get
			{
				return m_count;
			}
		}
	}

	public class Controller : IControllerLocal
	{
		internal class MessageReassembler
		{
			enum ReceiveState
			{
				Idle = 0,
				Initialize = 1,
				WaitingForHeader = 2,
				ReadingHeader = 3,
				CompleteHeader = 4,
				ReadingPayload = 5,
				CompletePayload = 6,
			}

			Controller m_parent;
			ReceiveState m_state;
			MessageRaw m_raw;
			int m_rawPos;
			MessageBase m_base;

			internal MessageReassembler(Controller parent)
			{
				m_parent = parent;
				m_state = ReceiveState.Initialize;
			}

			internal IncomingMessage GetCompleteMessage()
			{
				return new IncomingMessage(m_parent, m_raw, m_base);
			}

			/// <summary>
			/// Essential Rx method. Drives state machine by reading data and processing it. This works in
			/// conjunction with NotificationThreadWorker [Tx].
			/// </summary>
			internal void Process()
			{
				int count;
				int bytesRead;

				try
				{
					switch(m_state)
					{
						case ReceiveState.Initialize:
							m_rawPos = 0;

							m_base = new MessageBase();
							m_base.m_header = new Packet();

							m_raw = new MessageRaw();
							m_raw.m_header = m_parent.CreateConverter().Serialize(m_base.m_header);

							m_state = ReceiveState.WaitingForHeader;
							goto case ReceiveState.WaitingForHeader;

						case ReceiveState.WaitingForHeader:
							count = m_raw.m_header.Length - m_rawPos;

							bytesRead = m_parent.Read(m_raw.m_header, m_rawPos, count);
							m_rawPos += bytesRead;

							while(m_rawPos > 0)
							{
								int flag_Debugger = ValidSignature(m_parent.marker_Debugger);
								int flag_Packet = ValidSignature(m_parent.marker_Packet);
                                
								if(flag_Debugger == 1 || flag_Packet == 1)
								{
									m_state = ReceiveState.ReadingHeader;
									goto case ReceiveState.ReadingHeader;
								}

								if(flag_Debugger == 0 || flag_Packet == 0)
								{
									break; // Partial match.
								}

								m_parent.App.SpuriousCharacters(m_raw.m_header, 0, 1);

								Array.Copy(m_raw.m_header, 1, m_raw.m_header, 0, --m_rawPos);
							}
							break;

						case ReceiveState.ReadingHeader:
							count = m_raw.m_header.Length - m_rawPos;

							bytesRead = m_parent.Read(m_raw.m_header, m_rawPos, count);

							m_rawPos += bytesRead;

							if(bytesRead != count)
								break;

							m_state = ReceiveState.CompleteHeader;
							goto case ReceiveState.CompleteHeader;

						case ReceiveState.CompleteHeader:
							try
							{
								m_parent.CreateConverter().Deserialize(m_base.m_header, m_raw.m_header);

								if(VerifyHeader() == true)
								{
									bool fReply = (m_base.m_header.m_flags & Flags.c_Reply) != 0;

									m_base.DumpHeader("Receiving");

									if(m_base.m_header.m_size != 0)
									{
										m_raw.m_payload = new byte[m_base.m_header.m_size];
										//reuse m_rawPos for position in header to read.
										m_rawPos = 0;

										m_state = ReceiveState.ReadingPayload;
										goto case ReceiveState.ReadingPayload;
									}
									else
									{
										m_state = ReceiveState.CompletePayload;
										goto case ReceiveState.CompletePayload;
									}
								}
							}
							catch(ThreadAbortException)
							{
								throw;
							}
							catch(Exception e)
							{
								Console.WriteLine("Fault at payload deserialization:\n\n{0}", e.ToString());
							}

							m_state = ReceiveState.Initialize;

							if((m_base.m_header.m_flags & Flags.c_NonCritical) == 0)
							{
								IncomingMessage.ReplyBadPacket(m_parent, Flags.c_BadHeader);
							}

							break;

						case ReceiveState.ReadingPayload:
							count = m_raw.m_payload.Length - m_rawPos;

							bytesRead = m_parent.Read(m_raw.m_payload, m_rawPos, count);

							m_rawPos += bytesRead;

							if(bytesRead != count)
								break;

							m_state = ReceiveState.CompletePayload;
							goto case ReceiveState.CompletePayload;

						case ReceiveState.CompletePayload:
							if(VerifyPayload() == true)
							{
								try
								{
									bool fReply = (m_base.m_header.m_flags & Flags.c_Reply) != 0;

									if((m_base.m_header.m_flags & Flags.c_NACK) != 0)
									{
										m_raw.m_payload = null;
									}

									m_parent.App.ProcessMessage(this.GetCompleteMessage(), fReply);

									m_state = ReceiveState.Initialize;
									return;
								}
								catch(ThreadAbortException)
								{
									throw;
								}
								catch(Exception e)
								{
									Console.WriteLine("Fault at payload deserialization:\n\n{0}", e.ToString());
								}
							}

							m_state = ReceiveState.Initialize;

							if((m_base.m_header.m_flags & Flags.c_NonCritical) == 0)
							{
								IncomingMessage.ReplyBadPacket(m_parent, Flags.c_BadPayload);
							}

							break;             
					}
				}
				catch
				{
					m_state = ReceiveState.Initialize;
					throw;
				}
			}

			private int ValidSignature(byte[] sig)
			{
				System.Diagnostics.Debug.Assert(sig != null && sig.Length == Packet.SIZE_OF_SIGNATURE);
				int markerSize = Packet.SIZE_OF_SIGNATURE;
				int iMax = System.Math.Min(m_rawPos, markerSize);

				for(int i = 0; i < iMax; i++)
				{
					if(m_raw.m_header[i] != sig[i])
						return -1;
				}

				if(m_rawPos < markerSize)
					return 0;

				return 1;
			}

			private bool VerifyHeader()
			{
				uint crc = m_base.m_header.m_crcHeader;
				bool fRes;

				m_base.m_header.m_crcHeader = 0;

				fRes = CRC.ComputeCRC(m_parent.CreateConverter().Serialize(m_base.m_header), 0) == crc;

				m_base.m_header.m_crcHeader = crc;

				return fRes;
			}

			private bool VerifyPayload()
			{
				if(m_raw.m_payload == null)
				{
					return (m_base.m_header.m_size == 0);
				}
				else
				{
					if(m_base.m_header.m_size != m_raw.m_payload.Length)
						return false;

					return CRC.ComputeCRC(m_raw.m_payload, 0) == m_base.m_header.m_crcData;
				}
			}
		}

		internal byte[] marker_Debugger = Encoding.UTF8.GetBytes(Packet.MARKER_DEBUGGER_V1);
		internal byte[] marker_Packet = Encoding.UTF8.GetBytes(Packet.MARKER_PACKET_V1);
		private string m_marker;
		private IControllerHostLocal m_app;
		private Stream m_port;
		private int m_lastOutboundMessage;
		private DateTime m_lastActivity = DateTime.UtcNow;
		private int m_nextEndpointId;
		private FifoBuffer m_inboundData;
		private Thread m_inboundDataThread;
		private Thread m_stateMachineThread;
		private bool m_fProcessExit;
		private ManualResetEvent m_evtShutdown;
		private State m_state;
		private CLRCapabilities m_capabilities;
		private WaitHandle[] m_waitHandlesRead;

		public Controller(string marker, IControllerHostLocal app)
		{
			m_marker = marker;
			m_app = app;

			Random random = new Random();

			m_lastOutboundMessage = random.Next(65536);
			m_nextEndpointId = random.Next(int.MaxValue);

			m_state = new State(this);
            
			//default capabilities
			m_capabilities = new CLRCapabilities();
		}

		private Converter CreateConverter()
		{
			return new Converter(m_capabilities);
		}

		private Thread CreateThread(ThreadStart ts)
		{
			Thread th = new Thread(ts);            
			th.IsBackground = true;
			th.Start();

			return th;
		}

		#region IControllerLocal

		#region IController

		DateTime IController.LastActivity
		{
			get
			{
				return m_lastActivity;
			}
		}

		bool IController.IsPortConnected
		{
			[MethodImplAttribute( MethodImplOptions.Synchronized )]
            get
			{
				return (m_port != null);
			}
		}

		Packet IController.NewPacket()
		{
			if(!m_state.IsRunning)
				throw new ArgumentException("Controller not started, cannot create message");

			Packet bp = new Packet();

			SetSignature(bp, m_marker);

			bp.m_seq = (ushort)Interlocked.Increment(ref m_lastOutboundMessage);

			return bp;
		}

		bool IController.QueueOutput(MessageRaw raw)
		{
			SendRawBuffer(raw.m_header);
			if(raw.m_payload != null)
				SendRawBuffer(raw.m_payload);
			return true;
		}

		[MethodImplAttribute(MethodImplOptions.Synchronized)]
		void IController.ClosePort()
		{
			if(m_port != null)
			{
				try
				{
					m_port.Dispose();
				}
				catch
				{
				}

				m_port = null;
			}
		}

		void IController.Start()
		{
			m_state.SetValue(State.Value.Starting, true);

			m_inboundData = new FifoBuffer();
            
			m_evtShutdown = new ManualResetEvent(false);

			m_waitHandlesRead = new WaitHandle[] { m_evtShutdown, m_inboundData.WaitHandle };

			m_inboundDataThread = CreateThread(new ThreadStart(this.ReceiveInput));
			m_stateMachineThread = CreateThread(new ThreadStart(this.Process));

			m_state.SetValue(State.Value.Started, false);
		}

		void IController.StopProcessing()
		{
			m_state.SetValue(State.Value.Stopping, false);

			m_evtShutdown.Set();

			if(m_inboundDataThread != null)
			{
				m_inboundDataThread.Join();
				m_inboundDataThread = null;
			}
			if(m_stateMachineThread != null)
			{
				m_stateMachineThread.Join();
				m_stateMachineThread = null;
			}
		}

		void IController.ResumeProcessing()
		{
			m_evtShutdown.Reset();
			m_state.SetValue(State.Value.Resume, false);
			if(m_inboundDataThread == null)
			{
				m_inboundDataThread = CreateThread(new ThreadStart(this.ReceiveInput));
			}
			if(m_stateMachineThread == null)
			{
				m_stateMachineThread = CreateThread(new ThreadStart(this.Process));
			}
		}

		void IController.Stop()
		{
			if(m_evtShutdown != null)
			{
				m_evtShutdown.Set();
			}

			if(m_state.SetValue(State.Value.Stopping, false))
			{
				((IController)this).StopProcessing();

				((IController)this).ClosePort();

				m_state.SetValue(State.Value.Stopped, false);
			}
		}

		uint IController.GetUniqueEndpointId()
		{
			int id = Interlocked.Increment(ref m_nextEndpointId);

			return (uint)id;
		}

		CLRCapabilities IController.Capabilities
		{
			get { return m_capabilities; }
			set { m_capabilities = value; }            
		}

		#endregion

		[MethodImplAttribute(MethodImplOptions.Synchronized)]
		Stream IControllerLocal.OpenPort()
		{
			if(m_port == null)
			{
				m_port = App.OpenConnection();
			}

			return m_port;
		}

		#endregion

		internal IControllerHostLocal App
		{
			get
			{
				return m_app;
			}
		}

		internal int Read(byte[] buf, int offset, int count)
		{
			//wait on inbound data, or on exit....
			int countRequested = count;
            
			while(count > 0 && WaitHandle.WaitAny(m_waitHandlesRead) != 0)
			{                
				System.Diagnostics.Debug.Assert(m_inboundData.Available > 0);

				int cBytesRead = m_inboundData.Read(buf, offset, count);

				offset += cBytesRead;
				count -= cBytesRead;
			}

			return countRequested - count;
		}

		internal void SetSignature(Packet bp, string sig)
		{
			byte[] buf = Encoding.UTF8.GetBytes(sig);

			Array.Copy(buf, 0, bp.m_signature, 0, buf.Length);
		}

		private void ProcessExit()
		{
			bool fExit = false;

			lock(this)
			{
				if(!m_fProcessExit)
				{
					m_fProcessExit = true;

					fExit = true;
				}
			}

			if(fExit)
			{
				App.ProcessExited();
			}
		}

		private void Process()
		{
			MessageReassembler msg = new MessageReassembler(this);

			while(m_state.IsRunning)
			{
				try
				{
					msg.Process();
				}
				catch(ThreadAbortException)
				{
					((IController)this).Stop();
					break;
				}
				catch
				{
					((IController)this).ClosePort();

					Thread.Sleep(100);                    
				}
			}
		}

		private void ReceiveInput()
		{
			byte[] buf = new byte[128];

			int invalidOperationRetry = 5;

			while(m_state.IsRunning)
			{
				try
				{
					Stream stream = ((IControllerLocal)this).OpenPort();

					IStreamAvailableCharacters streamAvail = stream as IStreamAvailableCharacters;
					int avail = 0;

					if(streamAvail != null)
					{
						avail = streamAvail.AvailableCharacters;

						if(avail == 0)
						{
							Thread.Sleep(100);
							continue;
						}
					}

					if(avail == 0)
						avail = 1;

					if(avail > buf.Length)
						buf = new byte[avail];

					int read = stream.Read(buf, 0, avail);

					if(read > 0)
					{
						m_lastActivity = DateTime.UtcNow;

						m_inboundData.Write(buf, 0, read);
					}
					else if(read == 0)
					{
						Thread.Sleep(100);
					}
				}
				catch(ProcessExitException)
				{
					ProcessExit();

					((IController)this).ClosePort();

					return;
				}
				catch(InvalidOperationException)
				{
					if(invalidOperationRetry <= 0)
					{
						ProcessExit();

						((IController)this).ClosePort();

						return;
					}
					else
					{
						invalidOperationRetry--;

						((IController)this).ClosePort();

						Thread.Sleep(200);
					}
				}
				catch(IOException)
				{
					((IController)this).ClosePort();

					Thread.Sleep(200);
				}
				catch
				{
					((IController)this).ClosePort();

					Thread.Sleep(200);
				}
			}
		}

		public void SendRawBuffer(byte[] buf)
		{
			try
			{
				Stream stream = ((IControllerLocal)this).OpenPort();
                
				stream.Write(buf, 0, buf.Length);
				stream.Flush();
			}
			catch(ProcessExitException)
			{
				ProcessExit();
				return;
			}
			catch
			{
				((IController)this).ClosePort();
			}
		}
	}

	[Serializable]
	public class MessageRaw
	{
		public byte[] m_header;
		public byte[] m_payload;
	}

	public class MessageBase
	{
		public Packet m_header;
		public object m_payload;

		[System.Diagnostics.Conditional("TRACE_DBG_HEADERS")]
		public void DumpHeader(string txt)
		{
			Console.WriteLine("{0}: {1:X08} {2:X08} {3} {4}", txt, m_header.m_cmd, m_header.m_flags, m_header.m_seq, m_header.m_seqReply);          
		}
	}

	public class IncomingMessage
	{
		IController m_parent;
		MessageRaw m_raw;
		MessageBase m_base;

		public IncomingMessage(IController parent, MessageRaw raw, MessageBase messageBase)
		{
			m_parent = parent;
			m_raw = raw;
			m_base = messageBase;
		}

		public MessageRaw Raw
		{
			get
			{
				return m_raw;
			}
		}

		public MessageBase Base
		{
			get
			{
				return m_base;
			}
		}

		public IController Parent
		{
			get
			{
				return m_parent;
			}
		}

		public Packet Header
		{
			get
			{
				return m_base.m_header;
			}
		}

		public object Payload
		{
			get
			{
				return m_base.m_payload;
			}
			set
			{        
				object payload = null;

				if(m_raw.m_payload != null)
				{
					if(value != null)
					{
						new Converter(m_parent.Capabilities).Deserialize(value, m_raw.m_payload);
						payload = value;
					}
					else
					{
						payload = m_raw.m_payload.Clone();
					}
				}

				m_base.m_payload = payload;
			}
		}

		static public bool IsPositiveAcknowledge(IncomingMessage reply)
		{
			return reply != null && ((reply.Header.m_flags & WireProtocol.Flags.c_ACK) != 0);
		}

		static public bool ReplyBadPacket(IController ctrl, uint flags)
		{
			//What is this for? Nack + Ping?  What can the TinyCLR possibly do with this information?
			OutgoingMessage msg = new OutgoingMessage(ctrl, new WireProtocol.Converter(), Commands.c_Monitor_Ping, Flags.c_NonCritical | Flags.c_NACK | flags, null);

			return msg.Send();
		}

		public bool Reply(Converter converter, uint flags, object payload)
		{
            
			OutgoingMessage msgReply = new OutgoingMessage(this, converter, flags, payload);

			return msgReply.Send();
		}
	}

	public class OutgoingMessage
	{
		IController m_parent;
		MessageRaw m_raw;
		MessageBase m_base;

		public OutgoingMessage(IController parent, Converter converter, uint cmd, uint flags, object payload)
		{
			InitializeForSend(parent, converter, cmd, flags, payload);

			UpdateCRC(converter);
		}

		internal OutgoingMessage(IncomingMessage req, Converter converter, uint flags, object payload)
		{
			InitializeForSend(req.Parent, converter, req.Header.m_cmd, flags, payload);

			m_base.m_header.m_seqReply = req.Header.m_seq;
			m_base.m_header.m_flags |= Flags.c_Reply;

			UpdateCRC(converter);
		}

		public bool Send()
		{
			try
			{
				m_base.DumpHeader("Sending");

				return m_parent.QueueOutput(m_raw);
			}
			catch
			{
				return false;
			}
		}

		public Packet Header
		{
			get
			{
				return m_base.m_header;
			}
		}

		public object Payload
		{
			get
			{
				return m_base.m_payload;
			}
		}

		internal void InitializeForSend(IController parent, Converter converter, uint cmd, uint flags, object payload)
		{
			Packet header = parent.NewPacket();

			header.m_cmd = cmd;
			header.m_flags = flags;

			m_parent = parent;

			m_raw = new MessageRaw();
			m_base = new MessageBase();
			m_base.m_header = header;
			m_base.m_payload = payload;

			if(payload != null)
			{                
				m_raw.m_payload = converter.Serialize(payload);

				m_base.m_header.m_size = (uint)m_raw.m_payload.Length;
				m_base.m_header.m_crcData = CRC.ComputeCRC(m_raw.m_payload, 0);
			}
		}

		private void UpdateCRC(Converter converter)
		{
			Packet header = m_base.m_header;

			//
			// The CRC for the header is computed setting the CRC field to zero and then running the CRC algorithm.
			//
			header.m_crcHeader = 0;
			header.m_crcHeader = CRC.ComputeCRC(converter.Serialize(header), 0);

			m_raw.m_header = converter.Serialize(header);
		}
	}
}
