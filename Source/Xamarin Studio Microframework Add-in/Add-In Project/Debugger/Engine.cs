////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Copyright (c) Microsoft Corporation.  All rights reserved.
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Collections;
using System.Collections.Specialized;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Messaging;
using System.Runtime.Remoting.Proxies;
using System.Management;
using Microsoft.Win32;
using Microsoft.SPOT.Debugger.WireProtocol;
using System.Security.Cryptography.X509Certificates;
using System.Collections.Generic;
using WinUsb;

namespace Microsoft.SPOT.Debugger
{
	public delegate void NoiseEventHandler(byte[] buf,int offset,int count);
	public delegate void MessageEventHandler(WireProtocol.IncomingMessage msg,string text);
	public delegate void CommandEventHandler(WireProtocol.IncomingMessage msg,bool fReply);
	[Serializable]
	public class ThreadStatus
	{
		public const uint STATUS_Ready = WireProtocol.Commands.Debugging_Thread_Stack.Reply.TH_S_Ready;
		public const uint STATUS_Waiting = WireProtocol.Commands.Debugging_Thread_Stack.Reply.TH_S_Waiting;
		public const uint STATUS_Terminated = WireProtocol.Commands.Debugging_Thread_Stack.Reply.TH_S_Terminated;
		public const uint FLAGS_Suspended = WireProtocol.Commands.Debugging_Thread_Stack.Reply.TH_F_Suspended;
		public uint m_pid;
		public uint m_flags;
		public uint m_status;
		public string[] m_calls;
	}

	public enum PortFilter
	{
		Serial,
		Usb,
		Emulator,
		TcpIp,
	}

	public enum PublicKeyIndex
	{
		FirmwareKey = 0,
		DeploymentKey = 1}

	;

	[Serializable]
	public abstract class PortDefinition
	{
		protected string m_displayName;
		protected string m_port;
		protected ListDictionary m_properties = new ListDictionary();

		protected PortDefinition(string displayName, string port)
		{
			m_displayName = displayName;
			m_port = port;
		}

		public override bool Equals(object obj)
		{
			PortDefinition pd = obj as PortDefinition;
			if(pd == null)
				return false;

			return (pd.UniqueId.Equals(this.UniqueId));
		}

		public override int GetHashCode()
		{
			return base.GetHashCode();
		}

		static public PortDefinition CreateInstanceForSerial(string displayName, string port, uint baudRate)
		{
			return new PortDefinition_Serial(displayName, port, baudRate);
		}

		static public PortDefinition CreateInstanceForUsb(string displayName, string port)
		{
			return new PortDefinition_Usb(displayName, port, new ListDictionary());
		}

		static public PortDefinition CreateInstanceForWinUsb(string displayName, string port)
		{
			return new PortDefinition_WinUsb(displayName, port, new ListDictionary());
		}

		static public PortDefinition CreateInstanceForEmulator(string displayName, string port, int pid)
		{
			return new PortDefinition_Emulator(displayName, port, pid);
		}

		static public PortDefinition CreateInstanceForTcp(IPEndPoint ipEndPoint)
		{
			return new PortDefinition_Tcp(ipEndPoint);
		}

		static public PortDefinition CreateInstanceForTcp(string name)
		{
			PortDefinition portDefinition = null;

			//From CorDebug\DebugPort.cs
			string hostName = name;
			int port = PortDefinition_Tcp.WellKnownPort;
			int portIndex = hostName.IndexOf(':');
			IPAddress address = null;

			if(portIndex > 0)
			{
				hostName = name.Substring(0, portIndex);

				if(portIndex < name.Length - 1)
				{
					string portString = name.Substring(portIndex + 1);

					int portT;

					if(int.TryParse(portString, out portT))
					{
						port = portT;
					}
				}
			}

			if(!IPAddress.TryParse(hostName, out address))
			{
				//Does DNS resolution make sense here?

				IPHostEntry iPHostEntry = Dns.GetHostEntry(hostName);

				if(iPHostEntry.AddressList.Length > 0)
				{
					//choose the first one?
					address = iPHostEntry.AddressList[0];
				}
			}

			if(address != null)
			{
				IPEndPoint ipEndPoint = new IPEndPoint(address, port);

				portDefinition = new PortDefinition_Tcp(ipEndPoint);

				//ping to see if it is alive?
			}

			return portDefinition;
		}

		static public ArrayList Enumerate(params PortFilter[] args)
		{
			ArrayList lst = new ArrayList();

			foreach(PortFilter pf in args)
			{
				PortDefinition[] res;

				switch(pf)
				{
					case PortFilter.Emulator:
						res = Emulator.EnumeratePipes();
						break;
					case PortFilter.Serial:
						res = AsyncSerialStream.EnumeratePorts();
						break;
					case PortFilter.Usb: 
						{
						if (Platform.IsWindows) {
							res = WinUsb_AsyncUsbStream.EnumeratePorts ();
							lst.AddRange (res);
							res = AsyncUsbStream.EnumeratePorts (); 
						} else {
							res = LibUsb_AsyncUsbStream.EnumeratePorts ();
						}
						}
						break;
					case PortFilter.TcpIp:
						res = PortDefinition_Tcp.EnumeratePorts();
						break;
					default:
						res = null;
						break;
				}

				if(res != null)
				{
					lst.AddRange(res);
				}
			}

			return lst;
		}

		public string DisplayName { get { return m_displayName; } }

		public ListDictionary Properties { get { return m_properties; } }

		public bool TryToOpen()
		{
			bool fSuccess = false;

			try
			{
				using(Stream stream = CreateStream())
				{
					fSuccess = true;
					stream.Close();
				}
			}
			catch
			{
			}

			return fSuccess;
		}

		public virtual string Port
		{
			get
			{
				return m_port;
			}
		}

		public virtual object UniqueId
		{
			get
			{
				return m_port;
			}
		}

		public virtual string PersistName
		{
			get
			{
				return this.UniqueId.ToString();
			}
		}

		public virtual Stream Open()
		{
			Stream stream = CreateStream();

			return stream;
		}

		public abstract Stream CreateStream();
	}

	public class EndPoint
	{
		internal class MessageCall
		{
			public readonly string Name;
			public readonly object[] Args;

			public MessageCall(string name, object[] args)
			{
				this.Name = name;
				this.Args = args;
			}

			public static MessageCall CreateFromIMethodMessage(IMethodMessage message)
			{
				return new MessageCall(message.MethodName, message.Args);
			}

			public object CreateMessagePayload()
			{
				return new object[] { this.Name, this.Args };
			}

			public static MessageCall CreateFromMessagePayload(object payload)
			{
				object[] data = (object[])payload;
				string name = (string)data[0];
				object[] args = (object[])data[1];

				return new MessageCall(name, args);
			}
		}

		internal Engine m_eng;
		internal uint m_type;
		internal uint m_id;
		internal int m_seq;
		private object m_server;
		private Type m_serverClassToRemote;

		internal EndPoint(Type type, uint id, Engine engine)
		{
			m_type = BinaryFormatter.LookupHash(type);
			m_id = id;
			m_seq = 0;
			m_eng = engine;
		}

		public EndPoint(Type type, uint id, object server, Type classToRemote, Engine engine)
            : this(type, id, engine)
		{
			m_server = server;
			m_serverClassToRemote = classToRemote;
		}

		public void Register()
		{
			m_eng.RpcRegisterEndPoint(this);
		}

		public void Deregister()
		{
			m_eng.RpcDeregisterEndPoint(this);
		}

		internal bool CheckDestination(EndPoint ep)
		{
			return m_eng.RpcCheck(InitializeAddressForTransmission(ep));
		}

		internal bool IsRpcServer
		{
			get { return m_server != null; }
		}

		private WireProtocol.Commands.Debugging_Messaging_Address InitializeAddressForTransmission(EndPoint epTo)
		{
			WireProtocol.Commands.Debugging_Messaging_Address addr = new WireProtocol.Commands.Debugging_Messaging_Address();

			addr.m_seq = (uint)Interlocked.Increment(ref this.m_seq);

			addr.m_from_Type = this.m_type;
			addr.m_from_Id = this.m_id;

			addr.m_to_Type = epTo.m_type;
			addr.m_to_Id = epTo.m_id;

			return addr;
		}

		internal WireProtocol.Commands.Debugging_Messaging_Address InitializeAddressForReception()
		{
			WireProtocol.Commands.Debugging_Messaging_Address addr = new WireProtocol.Commands.Debugging_Messaging_Address();

			addr.m_seq = 0;

			addr.m_from_Type = 0;
			addr.m_from_Id = 0;

			addr.m_to_Type = this.m_type;
			addr.m_to_Id = this.m_id;

			return addr;
		}

		internal object SendMessage(EndPoint ep, int timeout, MessageCall call)
		{
			object data = call.CreateMessagePayload();

			byte[] payload = m_eng.CreateBinaryFormatter().Serialize(data);

			byte[] res = SendMessageInner(ep, timeout, payload);

			if(res == null)
			{
				throw new RemotingException(string.Format("Remote call '{0}' failed", call.Name));
			}

			object o = m_eng.CreateBinaryFormatter().Deserialize(res);

			Microsoft.SPOT.Messaging.Message.RemotedException ex = o as Microsoft.SPOT.Messaging.Message.RemotedException;

			if(ex != null)
			{
				ex.Raise();
			}

			return o;
		}

		internal void DispatchMessage(Message message)
		{
			object res = null;

			try
			{
				MessageCall call = MessageCall.CreateFromMessagePayload(message.Payload);

				object[] args = call.Args;
				Type[] argTypes = new Type[(args == null) ? 0 : args.Length];

				if(args != null)
				{
					for(int i = args.Length - 1; i >= 0; i--)
					{
						object arg = args[i];

						argTypes[i] = (arg == null) ? typeof(object) : arg.GetType();
					}
				}

				System.Reflection.MethodInfo mi = this.m_serverClassToRemote.GetMethod(call.Name, argTypes);

				if(mi == null)
					throw new Exception(string.Format("Could not find remote method '{0}'", call.Name));

				res = mi.Invoke(this.m_server, call.Args);
			}
			catch(Exception ex)
			{
				if(ex.InnerException != null)
				{
					//If an exception is thrown in the target method, it will be packaged up as the InnerException
					ex = ex.InnerException;
				}

				res = new Microsoft.SPOT.Messaging.Message.RemotedException(ex);
			}

			try
			{
				message.Reply(res);
			}
			catch
			{
			}
		}

		internal byte[] SendMessageInner(EndPoint ep, int timeout, byte[] data)
		{
			return m_eng.RpcSend(InitializeAddressForTransmission(ep), timeout, data);
		}

		internal void ReplyInner(Message msg, byte[] data)
		{
			m_eng.RpcReply(msg.m_addr, data);
		}

		static public object GetObject(Engine eng, Type type, uint id, Type classToRemote)
		{
			return GetObject(eng, new EndPoint(type, id, eng), classToRemote);
		}

		static internal object GetObject(Engine eng, EndPoint ep, Type classToRemote)
		{
			uint id = eng.RpcGetUniqueEndpointId();

			EndPoint epLocal = new EndPoint(typeof(EndPointProxy), id, eng);

			EndPointProxy prx = new EndPointProxy(eng, epLocal, ep, classToRemote);

			return prx.GetTransparentProxy();
		}

		internal class EndPointProxy : RealProxy, IDisposable
		{
			private Engine m_eng;
			private Type m_type;
			private EndPoint m_from;
			private EndPoint m_to;

			internal EndPointProxy(Engine eng, EndPoint from, EndPoint to, Type type)
                : base(type)
			{
				from.Register();

				if(from.CheckDestination(to) == false)
				{
					from.Deregister();

					throw new ArgumentException("Cannot connect to device EndPoint");
				}

				m_eng = eng;
				m_from = from;
				m_to = to;
				m_type = type;
			}

			~EndPointProxy()
			{
				Dispose();
			}

			public void Dispose()
			{
				try
				{
					if(m_from != null)
					{
						m_from.Deregister();
					}
				}
				catch
				{
				}
				finally
				{
					m_eng = null;
					m_from = null;
					m_to = null;
					m_type = null;
				}
			}

			public override IMessage Invoke(IMessage message)
			{
				IMethodMessage myMethodMessage = (IMethodMessage)message;

				if(myMethodMessage.MethodSignature is System.Array)
				{
					foreach(Type t in (System.Array)myMethodMessage.MethodSignature)
					{
						if(t.IsByRef)
						{
							throw new NotSupportedException("ByRef parameters are not supported");
						}
					}
				}

				MethodInfo mi = myMethodMessage.MethodBase as MethodInfo;

				if(mi != null)
				{
					BinaryFormatter.PopulateFromType(mi.ReturnType);
				}

				EndPoint.MessageCall call = EndPoint.MessageCall.CreateFromIMethodMessage(myMethodMessage);

				object returnValue = m_from.SendMessage(m_to, 60 * 1000, call);

				// Build the return message to pass back to the transparent proxy.
				return new ReturnMessage(returnValue, null, 0, null, (IMethodCallMessage)message);
			}
		}
	}

	internal class Message
	{
		internal readonly EndPoint m_source;
		internal readonly WireProtocol.Commands.Debugging_Messaging_Address m_addr;
		internal readonly byte[] m_payload;

		internal Message(EndPoint source, WireProtocol.Commands.Debugging_Messaging_Address addr, byte[] payload)
		{
			m_source = source;
			m_addr = addr;
			m_payload = payload;
		}

		public object Payload
		{
			get
			{
				return m_source.m_eng.CreateBinaryFormatter().Deserialize(m_payload);
			}
		}

		public void Reply(object data)
		{
			byte[] payload = m_source.m_eng.CreateBinaryFormatter().Serialize(data);
			m_source.ReplyInner(this, payload);
		}
	}

	public class ProcessExitException : Exception
	{
	}

	internal class State
	{
		public enum Value
		{
			NotStarted,
			Starting,
			Started,
			Stopping,
			Resume,
			Stopped,
			Disposing,
			Disposed
		}

		private Value m_value;
		private object m_syncObject;

		public State(object syncObject)
		{
			m_value = Value.NotStarted;
			m_syncObject = syncObject;
		}

		public Value GetValue()
		{
			return m_value;
		}

		public bool SetValue(Value value)
		{
			return SetValue(value, false);
		}

		public bool SetValue(Value value, bool fThrow)
		{
			lock(m_syncObject)
			{
				if(m_value == Value.Stopping && value == Value.Resume)
				{
					m_value = Value.Started;
					return true;
				}
				else if(m_value < value)
				{
					m_value = value;
					return true;
				}
				else
				{
					if(fThrow)
					{
						throw new ApplicationException(string.Format("Cannot set State to {0}", value));
					}

					return false;
				}
			}
		}

		public bool IsRunning
		{
			get
			{
				Value val = m_value;

				return val == Value.Starting || val == Value.Started;
			}
		}

		public object SyncObject
		{
			get { return m_syncObject; }
		}
	}

	public enum ConnectionSource
	{
		Unknown,
		TinyBooter,
		TinyCLR,
		MicroBooter,
	};

	public class Engine : WireProtocol.IControllerHostLocal, IDisposable
	{
		internal class Request
		{
			internal Engine m_parent;
			internal WireProtocol.OutgoingMessage m_req;
			internal WireProtocol.IncomingMessage m_res;
			internal int m_retries;
			internal TimeSpan m_timeoutRetry;
			internal TimeSpan m_timeoutWait;
			internal CommandEventHandler m_callback;
			internal ManualResetEvent m_event;
			internal Timer m_timer;

			internal Request(Engine parent, WireProtocol.OutgoingMessage req, int retries, int timeout, CommandEventHandler callback)
			{
				if(retries < 0)
				{
					throw new ArgumentException("Value cannot be negative", "retries");
				}

				if(timeout < 1 || timeout > 60 * 60 * 1000)
				{
					throw new ArgumentException(String.Format("Value out of bounds: {0}", timeout), "timeout");
				}

				m_parent = parent;
				m_req = req;
				m_retries = retries;
				m_timeoutRetry = new TimeSpan(timeout * TimeSpan.TicksPerMillisecond);
				m_timeoutWait = new TimeSpan((retries == 0 ? 1 : 2 * retries) * timeout * TimeSpan.TicksPerMillisecond);
				m_callback = callback;

				if(callback == null)
				{
					m_event = new ManualResetEvent(false);
				}
			}

			internal void SendAsync()
			{
				m_req.Send();

			}

			internal bool MatchesReply(WireProtocol.IncomingMessage res)
			{
				WireProtocol.Packet headerReq = m_req.Header;
				WireProtocol.Packet headerRes = res.Header;

				if(headerReq.m_cmd == headerRes.m_cmd &&
				                headerReq.m_seq == headerRes.m_seqReply)
				{
					return true;
				}

				return false;
			}

			internal WireProtocol.IncomingMessage Wait()
			{
				WireProtocol.IncomingMessage res = m_res;

				if(m_event != null)
				{
					DateTime waitStartTime = DateTime.UtcNow;
					bool requestTimedOut = false;

					/// Wait for m_timeoutRetry milliseconds, if we did not get a signal by then
					/// attempt sending the request again, and then wait more.
					while((requestTimedOut = !m_event.WaitOne(m_timeoutRetry, false)))
					{
						TimeSpan diff = DateTime.UtcNow - waitStartTime;
						if(diff >= m_timeoutWait)
							break;

						if(m_retries > 0)
						{
							if(m_req.Send())
							{
								m_retries--;
							}
						}
                        /// m_retries and m_timeoutWait are competing entities. Here I am settling down for m_retries
                        /// in the event m_retries * m_timeoutRetry < m_timeoutWait.
                        else
						{
							break;
						}
					}

					if(requestTimedOut)
					{
						m_parent.CancelRequest(this);
					}

					res = m_res;

					if(res == null && this.m_parent.m_fThrowOnCommunicationFailure)
					{
						//do we want a separate exception for aborted requests?
						throw new System.IO.IOException("Request failed");
					}
				}

				return res;
			}

			internal void Signal(WireProtocol.IncomingMessage res)
			{
				lock(this)
				{
					if(m_timer != null)
					{
						m_timer.Dispose();

						m_timer = null;
					}

					m_res = res;
				}

				Signal();
			}

			internal void Signal()
			{
				CommandEventHandler callback;
				WireProtocol.IncomingMessage res;

				lock(this)
				{
					callback = m_callback;
					res = m_res;

					if(m_timer != null)
					{
						m_timer.Dispose();

						m_timer = null;
					}

					if(m_event != null)
					{
						m_event.Set();
					}
				}

				if(callback != null)
				{
					callback(res, true);
				}
			}

			internal void Retry(object state)
			{
				bool fCancel = false;
				TimeSpan ts = TimeSpan.MinValue;

				lock(this)
				{
					if(m_res != null || m_timer == null)
						return;

					try
					{
						while(true)
						{
							DateTime now = DateTime.UtcNow;

							ts = now - m_parent.LastActivity;

							if(ts < m_timeoutRetry)
							{
								//
								// There was some activity going on, compensate for that.
								//
								ts = m_timeoutRetry - ts;
								break;
							}

							if(m_retries > 0)
							{
								if(m_req.Send())
								{
									m_retries--;

									ts = m_timeoutRetry;
								}
								else
								{
									//
									// Too many pending requests, retry in a bit.
									//
									ts = new TimeSpan(10 * TimeSpan.TicksPerMillisecond);
								}

								break;
							}

							fCancel = true;
							break;
						}
					}
					catch
					{
						fCancel = true;
					}

					if(!fCancel)
					{
						m_timer.Change((int)ts.TotalMilliseconds, Timeout.Infinite);
					}
				}

				//
				// Call can go out-of-proc, you need to release locks before the call to avoid deadlocks.
				//
				if(fCancel)
				{
					m_parent.CancelRequest(this);
				}
			}
		}

		public enum RebootOption
		{
			EnterBootloader,
			RebootClrOnly,
			NormalReboot,
			NoReconnect,
			RebootClrWaitForDebugger,
		};

		private class RebootTime
		{
			public const int c_RECONNECT_RETRIES_DEFAULT = 5;
			public const int c_RECONNECT_HARD_TIMEOUT_DEFAULT_MS = 1000;
			// one second
			public const int c_RECONNECT_SOFT_TIMEOUT_DEFAULT_MS = 500;
			// 500 milliseconds
			public const int c_MIN_RECONNECT_RETRIES = 1;
			public const int c_MAX_RECONNECT_RETRIES = 1000;
			public const int c_MIN_TIMEOUT_MS = 1 * 50;
			// fifty milliseconds
			public const int c_MAX_TIMEOUT_MS = 60 * 1000;
			// sixty seconds
			int m_retriesCount;
			int m_waitHardMs;
			int m_waitSoftMs;

			public RebootTime()
			{
				m_waitSoftMs = c_RECONNECT_SOFT_TIMEOUT_DEFAULT_MS;
				m_waitHardMs = c_RECONNECT_HARD_TIMEOUT_DEFAULT_MS;

				bool fOverride = false;
				string timingKey = @"\NonVersionSpecific\Timing\AnyDevice";

				RegistryAccess.GetBoolValue(timingKey, "override", out fOverride, false);

				if(RegistryAccess.GetIntValue(timingKey, "retries", out m_retriesCount, c_RECONNECT_RETRIES_DEFAULT))
				{
					if(!fOverride)
					{
						if(m_retriesCount < c_MIN_RECONNECT_RETRIES)
							m_retriesCount = c_MIN_RECONNECT_RETRIES;
						if(m_retriesCount > c_MAX_RECONNECT_RETRIES)
							m_retriesCount = c_MAX_RECONNECT_RETRIES;
					}
				}

				if(RegistryAccess.GetIntValue(timingKey, "timeout", out m_waitHardMs, c_RECONNECT_HARD_TIMEOUT_DEFAULT_MS))
				{
					if(!fOverride)
					{
						if(m_waitHardMs < c_MIN_TIMEOUT_MS)
							m_waitHardMs = c_MIN_TIMEOUT_MS;
						if(m_waitHardMs > c_MAX_TIMEOUT_MS)
							m_waitHardMs = c_MAX_TIMEOUT_MS;
					}
					m_waitSoftMs = m_waitHardMs;
				}
			}

			public int Retries
			{
				get
				{
					return m_retriesCount;
				}
			}

			public int WaitMs(bool fSoftReboot)
			{
				return (fSoftReboot ? m_waitSoftMs : m_waitHardMs);
			}
		}

		private const int RETRIES_DEFAULT = 4;
		private const int TIMEOUT_DEFAULT = 500;
		PortDefinition m_portDefinition;
		WireProtocol.IController m_ctrl;
		bool m_silent;
		bool m_stopDebuggerOnConnect;
		bool m_connected;
		ConnectionSource m_connectionSource;
		bool m_targetIsBigEndian;
		DateTime m_lastNoise = DateTime.Now;

		event NoiseEventHandler m_eventNoise;
		event MessageEventHandler m_eventMessage;
		event CommandEventHandler m_eventCommand;
		event EventHandler m_eventProcessExit;

		/// <summary>
		/// Notification thread is essentially the Tx thread. Other threads pump outgoing data into it, which after potential
		/// processing is sent out to destination synchronously.
		/// </summary>
		Thread m_notificationThread;
		AutoResetEvent m_notifyEvent;
		ArrayList m_notifyQueue;
		WireProtocol.FifoBuffer m_notifyNoise;
		AutoResetEvent m_rpcEvent;
		ArrayList m_rpcQueue;
		ArrayList m_rpcEndPoints;
		ManualResetEvent m_evtShutdown;
		ManualResetEvent m_evtPing;
		ArrayList m_requests;
		TypeSysLookup m_typeSysLookup;
		State m_state;
		bool m_fProcessExited;
		CLRCapabilities m_capabilities;
		bool m_fThrowOnCommunicationFailure;
		RebootTime m_RebootTime;

		private class TypeSysLookup
		{
			public enum Type : uint
			{
				Type,
				Method,
				Field
			}

			private Hashtable m_lookup;

			private void EnsureHashtable()
			{
				lock(this)
				{
					if(m_lookup == null)
					{
						m_lookup = Hashtable.Synchronized(new Hashtable());
					}
				}
			}

			private ulong KeyFromTypeToken(TypeSysLookup.Type type, uint token)
			{
				return ((ulong)type) << 32 | (ulong)token;
			}

			public object Lookup(TypeSysLookup.Type type, uint token)
			{
				EnsureHashtable();

				ulong key = KeyFromTypeToken(type, token);

				return m_lookup[key];
			}

			public void Add(TypeSysLookup.Type type, uint token, object val)
			{
				EnsureHashtable();

				ulong key = KeyFromTypeToken(type, token);

				m_lookup[key] = val;
			}
		}

		public Engine(PortDefinition pd)
		{
			InitializeLocal(pd);
		}

		public Engine()
		{
			Initialize();
		}

		public bool ThrowOnCommunicationFailure
		{
			get { return m_fThrowOnCommunicationFailure; }
			set { m_fThrowOnCommunicationFailure = value; }
		}

		private void InitializeLocal(PortDefinition pd)
		{
			m_portDefinition = pd;
			m_ctrl = new WireProtocol.Controller(WireProtocol.Packet.MARKER_PACKET_V1, this);

			Initialize();
		}

		private void Initialize()
		{
			m_notifyEvent = new AutoResetEvent(false);
			m_rpcEvent = new AutoResetEvent(false);
			m_evtShutdown = new ManualResetEvent(false);
			m_evtPing = new ManualResetEvent(false);

			m_rpcQueue = ArrayList.Synchronized(new ArrayList());
			m_rpcEndPoints = ArrayList.Synchronized(new ArrayList());
			m_requests = ArrayList.Synchronized(new ArrayList());
			m_notifyQueue = ArrayList.Synchronized(new ArrayList());

			m_notifyNoise = new WireProtocol.FifoBuffer();
			m_typeSysLookup = new TypeSysLookup();
			m_state = new State(this);
			m_fProcessExited = false;

			//default capabilities, used until clr can be queried.
			m_capabilities = new CLRCapabilities();

			m_RebootTime = new RebootTime();
		}

		private Thread CreateThread(ThreadStart ts)
		{
			Thread th = new Thread(ts);

			th.IsBackground = true;
			th.Priority = ThreadPriority.BelowNormal;

			th.Start();

			return th;
		}

		public CLRCapabilities Capabilities
		{
			get { return m_capabilities; }
		}

		public BinaryFormatter CreateBinaryFormatter()
		{
			return new BinaryFormatter(this.Capabilities);
		}

		public WireProtocol.Converter CreateConverter()
		{
			return new WireProtocol.Converter(this.Capabilities);
		}

		public void SetController(WireProtocol.IController ctrl)
		{
			if(m_ctrl != null)
			{
				throw new ArgumentException("Controller already initialized");
			}

			if(ctrl == null)
			{
				throw new ArgumentNullException("ctrl");
			}

			m_ctrl = ctrl;
		}

		public void Start()
		{
			if(m_ctrl == null)
			{
				throw new ApplicationException("Controller not initialized");
			}

			m_state.SetValue(State.Value.Starting, true);

			try
			{
				m_notificationThread = CreateThread(new ThreadStart(this.NotificationThreadWorker));

				m_ctrl.Start();
			}
			catch(Exception)
			{
				Stop();

				throw;
			}

			m_state.SetValue(State.Value.Started, false);
		}

		public void Stop()
		{
			if(m_state.SetValue(State.Value.Stopping))
			{
				m_evtShutdown.Set();

				CancelAllRequests();

				m_notificationThread = null;

				if(m_ctrl != null)
				{
					m_ctrl.Stop();
					m_ctrl = null;
				}

				m_state.SetValue(State.Value.Stopped);
			}
		}

		private bool IsRunning
		{
			get
			{
				return !m_fProcessExited && m_state.IsRunning;
			}
		}

		private void CancelAllRequests()
		{
			ArrayList requests;
			ArrayList endPoints;

			requests = (ArrayList)m_requests.Clone();

			foreach(Request req in requests)
			{
				CancelRequest(req);
			}

			endPoints = (ArrayList)m_rpcEndPoints.Clone();

			foreach(EndPointRegistration eep in endPoints)
			{
				try
				{
					RpcDeregisterEndPoint(eep.m_ep);
				}
				catch
				{
				}
			}
		}

		public DateTime LastActivity
		{
			get
			{
				return m_ctrl.LastActivity;
			}
		}

		public DateTime LastNoise
		{
			get
			{
				return m_lastNoise;
			}
		}

		public PortDefinition PortDefinition
		{
			get
			{
				return m_portDefinition;
			}
		}

		public bool Silent
		{
			get
			{
				return m_silent;
			}

			set
			{
				m_silent = value;
			}
		}

		public bool StopDebuggerOnConnect
		{
			get
			{
				return m_stopDebuggerOnConnect;
			}
			set
			{
				m_stopDebuggerOnConnect = value;
			}
		}

		public event NoiseEventHandler OnNoise
		{
			add
			{
				m_eventNoise += value;
			}

			remove
			{
				m_eventNoise -= value;
			}
		}

		public event MessageEventHandler OnMessage
		{
			add
			{
				m_eventMessage += value;
			}

			remove
			{
				m_eventMessage -= value;
			}
		}

		public event CommandEventHandler OnCommand
		{
			add
			{
				m_eventCommand += value;
			}

			remove
			{
				m_eventCommand -= value;
			}
		}

		public event EventHandler OnProcessExit
		{
			add
			{
				m_eventProcessExit += value;
			}

			remove
			{
				m_eventProcessExit -= value;
			}
		}

		public void WaitForPort()
		{
			if(m_ctrl.IsPortConnected == false)
			{
				InjectMessage("Port is not connected, waiting...\r\n");

				while(m_ctrl.IsPortConnected == false)
				{
					Thread.Sleep(200);
				}

				InjectMessage("Port connected, continuing...\r\n");
			}
		}

		public void ConfigureXonXoff(bool fEnable)
		{
			WireProtocol.IControllerLocal local = m_ctrl as WireProtocol.IControllerLocal;
			if(local != null && m_portDefinition is PortDefinition_Serial)
			{
				try
				{
					AsyncSerialStream port = local.OpenPort() as AsyncSerialStream;

					if(port != null)
					{
						port.ConfigureXonXoff(fEnable);
					}
				}
				catch
				{
				}
			}
		}

		#region IControllerHostLocal

		#region IControllerHost

		void WireProtocol.IControllerHost.SpuriousCharacters(byte[] buf, int offset, int count)
		{
			m_lastNoise = DateTime.Now;

			m_notifyNoise.Write(buf, offset, count);
		}

		void WireProtocol.IControllerHost.ProcessExited()
		{
			m_fProcessExited = true;

			Stop();

			EventHandler eventProcessExit = m_eventProcessExit;
			if(eventProcessExit != null)
			{
				eventProcessExit(this, null);
			}
		}

		#endregion

		bool WireProtocol.IControllerHostLocal.ProcessMessage(WireProtocol.IncomingMessage msg, bool fReply)
		{
			msg.Payload = WireProtocol.Commands.ResolveCommandToPayload(msg.Header.m_cmd, fReply, m_capabilities);

			if(fReply == true)
			{
				Request reply = null;

				lock(m_requests.SyncRoot)
				{
					foreach(Request req in m_requests)
					{
						if(req.MatchesReply(msg))
						{
							m_requests.Remove(req);

							reply = req;
							break;
						}
					}
				}

				if(reply != null)
				{
					reply.Signal(msg);
					return true;
				}
			}
			else
			{
				WireProtocol.Packet bp = msg.Header;

				switch(bp.m_cmd)
				{
					case WireProtocol.Commands.c_Monitor_Ping:
						{
							WireProtocol.Commands.Monitor_Ping.Reply cmdReply = new Microsoft.SPOT.Debugger.WireProtocol.Commands.Monitor_Ping.Reply();

							cmdReply.m_source = WireProtocol.Commands.Monitor_Ping.c_Ping_Source_Host;
							cmdReply.m_dbg_flags = (m_stopDebuggerOnConnect ? WireProtocol.Commands.Monitor_Ping.c_Ping_DbgFlag_Stop : 0);

							msg.Reply(CreateConverter(), WireProtocol.Flags.c_NonCritical, cmdReply);

							m_evtPing.Set();

							return true;
						}

					case WireProtocol.Commands.c_Monitor_Message:
						{
							WireProtocol.Commands.Monitor_Message payload = msg.Payload as WireProtocol.Commands.Monitor_Message;

							Debug.Assert(payload != null);

							if(payload != null)
							{
								QueueNotify(m_eventMessage, msg, payload.ToString());
							}

							return true;
						}

					case WireProtocol.Commands.c_Debugging_Messaging_Query:
					case WireProtocol.Commands.c_Debugging_Messaging_Reply:
					case WireProtocol.Commands.c_Debugging_Messaging_Send:
						{
							Debug.Assert(msg.Payload != null);

							if(msg.Payload != null)
							{
								QueueRpc(msg);
							}

							return true;
						}
				}
			}

			if(m_eventCommand != null)
			{
				QueueNotify(m_eventCommand, msg, fReply);
				return true;
			}

			return false;
		}

		Stream WireProtocol.IControllerHostLocal.OpenConnection()
		{
			return m_portDefinition.Open();
		}

		#endregion

		/// <summary>
		/// Notification thread is essentially the Tx thread. Other threads pump outgoing data into it, which after potential
		/// processing is sent out to destination synchronously.
		/// </summary>
		internal void NotificationThreadWorker()
		{
			byte[] buf = new byte[256];
			WaitHandle[] wh = new WaitHandle[] { m_evtShutdown, m_notifyEvent, m_notifyNoise.WaitHandle, m_rpcEvent };

			while(WaitHandle.WaitAny(wh) > 0)
			{
				int read = 0;
				while((read = m_notifyNoise.Available) > 0)
				{
					if(read > buf.Length)
						read = buf.Length;

					m_notifyNoise.Read(buf, 0, read);

					try
					{
						NoiseEventHandler ev = m_eventNoise;
						if(ev != null)
							ev(buf, 0, read);
					}
					catch
					{
					}
				}

				while(m_notifyQueue.Count > 0)
				{
					object[] arr = (object[])m_notifyQueue[0];
					m_notifyQueue.RemoveAt(0);

					try
					{
						CommandEventHandler cev = arr[0] as CommandEventHandler;
						if(cev != null)
							cev((WireProtocol.IncomingMessage)arr[1], (bool)arr[2]);
						MessageEventHandler mev = arr[0] as MessageEventHandler;
						if(mev != null)
							mev((WireProtocol.IncomingMessage)arr[1], (string)arr[2]);
					}
					catch
					{
					}
				}

				while(m_rpcQueue.Count > 0)
				{
					WireProtocol.IncomingMessage msg = (WireProtocol.IncomingMessage)m_rpcQueue[0];
					m_rpcQueue.RemoveAt(0);

					try
					{
						object payload = msg.Payload;

						switch(msg.Header.m_cmd)
						{
							case WireProtocol.Commands.c_Debugging_Messaging_Query:
								RpcReceiveQuery(msg, (WireProtocol.Commands.Debugging_Messaging_Query)payload);
								break;
							case WireProtocol.Commands.c_Debugging_Messaging_Send:
								RpcReceiveSend(msg, (WireProtocol.Commands.Debugging_Messaging_Send)payload);
								break;
							case WireProtocol.Commands.c_Debugging_Messaging_Reply:
								RpcReceiveReply(msg, (WireProtocol.Commands.Debugging_Messaging_Reply)payload);
								break;
							default:
								WireProtocol.IncomingMessage.ReplyBadPacket(msg.Parent, 0);
								break;
						}
					}
					catch
					{
					}
				}
			}
		}

		internal void QueueNotify(params object[] arr)
		{
			m_notifyQueue.Add(arr);
			m_notifyEvent.Set();
		}

		internal void InjectMessage(string format, params object[] args)
		{
			QueueNotify(m_eventMessage, null, String.Format(format, args));
		}

		internal class EndPointRegistration
		{
			internal class Request
			{
				public readonly EndPointRegistration Owner;

				public Request(EndPointRegistration owner)
				{
					this.Owner = owner;
				}
			}

			internal class OutboundRequest : Request
			{
				private byte[] m_data;
				private readonly AutoResetEvent m_wait;
				public readonly uint Seq;
				public readonly uint Type;
				public readonly uint Id;

				public OutboundRequest(EndPointRegistration owner, uint seq, uint type, uint id)
                    : base(owner)
				{
					this.Seq = seq;
					this.Type = type;
					this.Id = id;
					this.m_wait = new AutoResetEvent(false);
				}

				public byte[] Reply
				{
					get { return m_data; }

					set
					{
						m_data = value;
						m_wait.Set();
					}
				}

				public WaitHandle WaitHandle
				{
					get { return m_wait; }
				}
			}

			internal class InboundRequest : Request
			{
				public readonly Message m_msg;

				public InboundRequest(EndPointRegistration owner, Message msg)
                    : base(owner)
				{
					m_msg = msg;
				}
			}

			internal EndPoint m_ep;
			internal ArrayList m_req_Outbound;

			internal EndPointRegistration(EndPoint ep)
			{
				m_ep = ep;
				m_req_Outbound = ArrayList.Synchronized(new ArrayList());
			}

			internal void Destroy()
			{
				lock(m_req_Outbound.SyncRoot)
				{
					foreach(OutboundRequest or in m_req_Outbound)
					{
						or.Reply = null;
					}
				}

				m_req_Outbound.Clear();
			}
		}

		internal void QueueRpc(WireProtocol.IncomingMessage msg)
		{
			m_rpcQueue.Add(msg);
			m_rpcEvent.Set();
		}

		internal void RpcRegisterEndPoint(EndPoint ep)
		{
			EndPointRegistration eep = RpcFind(ep);
			bool fSuccess = false;

			if(eep == null)
			{
				WireProtocol.IControllerRemote remote = m_ctrl as WireProtocol.IControllerRemote;

				if(remote != null)
				{
					fSuccess = remote.RegisterEndpoint(ep.m_type, ep.m_id);
				}
				else
				{
					fSuccess = true;
				}

				if(fSuccess)
				{
					lock(m_rpcEndPoints.SyncRoot)
					{
						eep = RpcFind(ep);

						if(eep == null)
						{
							m_rpcEndPoints.Add(new EndPointRegistration(ep));
						}
						else
						{
							fSuccess = false;
						}
					}
				}
			}

			if(!fSuccess)
			{
				throw new ApplicationException("Endpoint already registered.");
			}
		}

		internal void RpcDeregisterEndPoint(EndPoint ep)
		{
			EndPointRegistration eep = RpcFind(ep);

			if(eep != null)
			{
				m_rpcEndPoints.Remove(eep);

				eep.Destroy();

				WireProtocol.IControllerRemote remote = m_ctrl as WireProtocol.IControllerRemote;
				if(remote != null)
				{
					remote.DeregisterEndpoint(ep.m_type, ep.m_id);
				}
			}
		}

		private EndPointRegistration RpcFind(EndPoint ep)
		{
			return RpcFind(ep.m_type, ep.m_id, false);
		}

		private EndPointRegistration RpcFind(uint type, uint id, bool fOnlyServer)
		{
			lock(m_rpcEndPoints.SyncRoot)
			{
				foreach(EndPointRegistration eep in m_rpcEndPoints)
				{
					EndPoint ep = eep.m_ep;

					if(ep.m_type == type && ep.m_id == id)
					{
						if(!fOnlyServer || ep.IsRpcServer)
						{
							return eep;
						}
					}
				}
			}
			return null;
		}

		private void RpcReceiveQuery(WireProtocol.IncomingMessage msg, WireProtocol.Commands.Debugging_Messaging_Query query)
		{
			WireProtocol.Commands.Debugging_Messaging_Address addr = query.m_addr;
			EndPointRegistration eep = RpcFind(addr.m_to_Type, addr.m_to_Id, true);

			WireProtocol.Commands.Debugging_Messaging_Query.Reply res = new WireProtocol.Commands.Debugging_Messaging_Query.Reply();

			res.m_found = (eep != null) ? 1u : 0u;
			res.m_addr = addr;

			msg.Reply(CreateConverter(), WireProtocol.Flags.c_NonCritical, res);
		}

		internal bool RpcCheck(WireProtocol.Commands.Debugging_Messaging_Address addr)
		{
			WireProtocol.Commands.Debugging_Messaging_Query cmd = new WireProtocol.Commands.Debugging_Messaging_Query();

			cmd.m_addr = addr;

			WireProtocol.IncomingMessage reply = SyncMessage(WireProtocol.Commands.c_Debugging_Messaging_Query, 0, cmd);
			if(reply != null)
			{
				WireProtocol.Commands.Debugging_Messaging_Query.Reply res = reply.Payload as WireProtocol.Commands.Debugging_Messaging_Query.Reply;

				if(res != null && res.m_found != 0)
				{
					return true;
				}
			}

			return false;
		}

		internal byte[] RpcSend(WireProtocol.Commands.Debugging_Messaging_Address addr, int timeout, byte[] data)
		{
			EndPointRegistration.OutboundRequest or = null;
			byte[] res = null;

			try
			{
				or = RpcSend_Setup(addr, data);
				if(or != null)
				{
					or.WaitHandle.WaitOne(timeout, false);

					res = or.Reply;
				}
			}
			finally
			{
				if(or != null)
				{
					or.Owner.m_req_Outbound.Remove(or);
				}
			}

			return res;
		}

		private EndPointRegistration.OutboundRequest RpcSend_Setup(WireProtocol.Commands.Debugging_Messaging_Address addr, byte[] data)
		{
			EndPointRegistration eep = RpcFind(addr.m_from_Type, addr.m_from_Id, false);
			EndPointRegistration.OutboundRequest or = null;

			if(eep != null)
			{
				bool fSuccess = false;

				or = new EndPointRegistration.OutboundRequest(eep, addr.m_seq, addr.m_to_Type, addr.m_to_Id);

				try
				{
					eep.m_req_Outbound.Add(or);

					WireProtocol.Commands.Debugging_Messaging_Send cmd = new WireProtocol.Commands.Debugging_Messaging_Send();

					cmd.m_addr = addr;
					cmd.m_data = data;

					WireProtocol.IncomingMessage reply = SyncMessage(WireProtocol.Commands.c_Debugging_Messaging_Send, 0, cmd);
					if(reply != null)
					{
						WireProtocol.Commands.Debugging_Messaging_Send.Reply res = reply.Payload as WireProtocol.Commands.Debugging_Messaging_Send.Reply;

						if(res != null && res.m_found != 0)
						{
							fSuccess = true;
						}
					}
				}
				catch
				{
				}

				if(!this.IsRunning)
				{
					fSuccess = false;
				}

				if(!fSuccess)
				{
					eep.m_req_Outbound.Remove(or);

					or = null;
				}
			}

			return or;
		}

		private void RpcReceiveSend(WireProtocol.IncomingMessage msg, WireProtocol.Commands.Debugging_Messaging_Send send)
		{
			WireProtocol.Commands.Debugging_Messaging_Address addr = send.m_addr;
			EndPointRegistration eep;

			eep = RpcFind(addr.m_to_Type, addr.m_to_Id, true);

			WireProtocol.Commands.Debugging_Messaging_Send.Reply res = new WireProtocol.Commands.Debugging_Messaging_Send.Reply();

			res.m_found = (eep != null) ? 1u : 0u;
			res.m_addr = addr;

			msg.Reply(CreateConverter(), WireProtocol.Flags.c_NonCritical, res);

			if(eep != null)
			{
				Message msgNew = new Message(eep.m_ep, addr, send.m_data);

				EndPointRegistration.InboundRequest ir = new EndPointRegistration.InboundRequest(eep, msgNew);

				ThreadPool.QueueUserWorkItem(new WaitCallback(RpcReceiveSendDispatch), ir);
			}
		}

		private void RpcReceiveSendDispatch(object obj)
		{
			EndPointRegistration.InboundRequest ir = (EndPointRegistration.InboundRequest)obj;

			if(this.IsRunning)
			{
				ir.Owner.m_ep.DispatchMessage(ir.m_msg);
			}
		}

		internal bool RpcReply(WireProtocol.Commands.Debugging_Messaging_Address addr, byte[] data)
		{
			WireProtocol.Commands.Debugging_Messaging_Reply cmd = new WireProtocol.Commands.Debugging_Messaging_Reply();

			cmd.m_addr = addr;
			cmd.m_data = data;

			WireProtocol.IncomingMessage reply = SyncMessage(WireProtocol.Commands.c_Debugging_Messaging_Reply, 0, cmd);
			if(reply != null)
			{
				WireProtocol.Commands.Debugging_Messaging_Reply.Reply res = new WireProtocol.Commands.Debugging_Messaging_Reply.Reply();

				if(res != null && res.m_found != 0)
				{
					return true;
				}
			}

			return false;
		}

		private void RpcReceiveReply(WireProtocol.IncomingMessage msg, WireProtocol.Commands.Debugging_Messaging_Reply reply)
		{
			WireProtocol.Commands.Debugging_Messaging_Address addr = reply.m_addr;
			EndPointRegistration eep;

			eep = RpcFind(addr.m_from_Type, addr.m_from_Id, false);

			WireProtocol.Commands.Debugging_Messaging_Reply.Reply res = new WireProtocol.Commands.Debugging_Messaging_Reply.Reply();

			res.m_found = (eep != null) ? 1u : 0u;
			res.m_addr = addr;

			msg.Reply(CreateConverter(), WireProtocol.Flags.c_NonCritical, res);

			if(eep != null)
			{
				lock(eep.m_req_Outbound.SyncRoot)
				{
					foreach(EndPointRegistration.OutboundRequest or in eep.m_req_Outbound)
					{
						if(or.Seq == addr.m_seq && or.Type == addr.m_to_Type && or.Id == addr.m_to_Id)
						{
							or.Reply = reply.m_data;

							break;
						}
					}
				}
			}
		}

		internal uint RpcGetUniqueEndpointId()
		{
			return m_ctrl.GetUniqueEndpointId();
		}

		internal Request AsyncRequest(WireProtocol.OutgoingMessage msg, int retries, int timeout)
		{
			try
			{
				Request req = new Request(this, msg, retries, timeout, null);

				lock(m_state.SyncObject)
				{

					//Checking whether IsRunning and adding the request to m_requests
					//needs to be atomic to avoid adding a request after the Engine
					//has been stopped.

					if(!this.IsRunning)
					{
						throw new ApplicationException("Engine is not running or process has exited.");
					}

					m_requests.Add(req);

					req.SendAsync();
				}

				return req;
			}
			catch
			{
				return null;
			}
		}

		/// <summary>
		/// Global lock object for synchornizing message request. This ensures there is only one
		/// outstanding request at any point of time. 
		/// </summary>
		internal object m_ReqSyncLock = new object();

		internal WireProtocol.IncomingMessage SyncRequest(WireProtocol.OutgoingMessage msg, int retries, int timeout)
		{
			/// Lock on m_ReqSyncLock object, so only one thread is active inside the block.
			lock(m_ReqSyncLock)
			{
				Request req = AsyncRequest(msg, retries, timeout);

				return req != null ? req.Wait() : null;
			}
		}

		internal void CancelRequest(Request req)
		{
			m_requests.Remove(req);

			req.Signal();
		}

		private WireProtocol.OutgoingMessage CreateMessage(uint cmd, uint flags, object payload)
		{
			return new WireProtocol.OutgoingMessage(m_ctrl, CreateConverter(), cmd, flags, payload);
		}

		private Request AsyncMessage(uint cmd, uint flags, object payload, int retries, int timeout)
		{
			WireProtocol.OutgoingMessage msg = CreateMessage(cmd, flags, payload);

			return AsyncRequest(msg, retries, timeout);
		}

		private WireProtocol.IncomingMessage SyncMessage(uint cmd, uint flags, object payload, int retries, int timeout)
		{
			/// Lock on m_ReqSyncLock object, so only one thread is active inside the block.
			lock(m_ReqSyncLock)
			{
				try
				{
					Request req = AsyncMessage(cmd, flags, payload, retries, timeout);

					return req.Wait();
				}
				catch
				{
				}

				return null;
			}
		}

		private WireProtocol.IncomingMessage SyncMessage(uint cmd, uint flags, object payload)
		{
			return SyncMessage(cmd, flags, payload, RETRIES_DEFAULT, TIMEOUT_DEFAULT);
		}

		private WireProtocol.IncomingMessage[] SyncMessages(WireProtocol.OutgoingMessage[] messages, int retries, int timeout)
		{
			int cMessage = messages.Length;
			WireProtocol.IncomingMessage[] replies = new WireProtocol.IncomingMessage[cMessage];
			Request[] requests = new Request[cMessage];

			for(int iMessage = 0; iMessage < cMessage; iMessage++)
			{
				replies[iMessage] = SyncRequest(messages[iMessage], retries, timeout);
			}

			return replies;
		}

		private WireProtocol.IncomingMessage[] SyncMessages(WireProtocol.OutgoingMessage[] messages)
		{
			return SyncMessages(messages, 2, 1000);
		}

		public bool IsConnected
		{
			get
			{
				return m_connected;
			}
		}

		public ConnectionSource ConnectionSource
		{
			get
			{
				if(!m_connected)
				{
					TryToConnect(0, 500, true, ConnectionSource.Unknown);
				}

				return m_connected ? m_connectionSource : ConnectionSource.Unknown;
			}
		}

		public bool IsConnectedToTinyCLR
		{
			get { return this.ConnectionSource == ConnectionSource.TinyCLR; }
		}

		public bool IsTargetBigEndian
		{
			get { return this.m_targetIsBigEndian; }
		}

		public bool TryToConnect(int retries, int wait)
		{
			return TryToConnect(retries, wait, false, ConnectionSource.Unknown);
		}

		public bool TryToConnect(int retries, int wait, bool force, ConnectionSource connectionSource)
		{
			if(force || m_connected == false)
			{
				WireProtocol.Commands.Monitor_Ping cmd = new Microsoft.SPOT.Debugger.WireProtocol.Commands.Monitor_Ping();

				cmd.m_source = WireProtocol.Commands.Monitor_Ping.c_Ping_Source_Host;
				cmd.m_dbg_flags = (m_stopDebuggerOnConnect ? WireProtocol.Commands.Monitor_Ping.c_Ping_DbgFlag_Stop : 0);

				WireProtocol.IncomingMessage msg = SyncMessage(WireProtocol.Commands.c_Monitor_Ping, WireProtocol.Flags.c_NoCaching, cmd, retries, wait);

				if(msg == null)
				{
					m_connected = false;
					return false;
				}

				WireProtocol.Commands.Monitor_Ping.Reply reply = msg.Payload as WireProtocol.Commands.Monitor_Ping.Reply;

				if(reply != null)
				{
					this.m_targetIsBigEndian = (reply.m_dbg_flags & WireProtocol.Commands.Monitor_Ping.c_Ping_DbgFlag_BigEndian).Equals(WireProtocol.Commands.Monitor_Ping.c_Ping_DbgFlag_BigEndian);
				}
				m_connected = true;

				m_connectionSource = (reply == null || reply.m_source == WireProtocol.Commands.Monitor_Ping.c_Ping_Source_TinyCLR) ? ConnectionSource.TinyCLR : ConnectionSource.TinyBooter;
 
				if(m_silent)
				{
					SetExecutionMode(WireProtocol.Commands.Debugging_Execution_ChangeConditions.c_fDebugger_Quiet, 0);
				}

				// resume execution for older clients, since server tools no longer do this.
				if(!m_stopDebuggerOnConnect && (msg != null && msg.Payload == null))
				{
					ResumeExecution();
				}
			}

			if((force || m_capabilities.IsUnknown) && m_connectionSource == ConnectionSource.TinyCLR)
			{
				m_capabilities = DiscoverCLRCapabilities();
				m_ctrl.Capabilities = m_capabilities;
			}

			if(connectionSource != ConnectionSource.Unknown && connectionSource != m_connectionSource)
			{
				m_connected = false;
				return false;
			}

			return true;
		}

		public WireProtocol.Commands.Monitor_Ping.Reply GetConnectionSource()
		{
			WireProtocol.IncomingMessage reply = SyncMessage(WireProtocol.Commands.c_Monitor_Ping, 0, null, 2, 1000);
			if(reply != null)
			{
				return reply.Payload as WireProtocol.Commands.Monitor_Ping.Reply;
			}
			return null;
		}

		public WireProtocol.Commands.Monitor_OemInfo.Reply GetMonitorOemInfo()
		{
			WireProtocol.IncomingMessage reply = SyncMessage(WireProtocol.Commands.c_Monitor_OemInfo, 0, null, 2, 1000);
			if(reply != null)
			{
				return reply.Payload as WireProtocol.Commands.Monitor_OemInfo.Reply;
			}
			return null;
		}

		public WireProtocol.Commands.Monitor_FlashSectorMap.Reply GetFlashSectorMap()
		{
			WireProtocol.IncomingMessage reply = SyncMessage(WireProtocol.Commands.c_Monitor_FlashSectorMap, 0, null, 1, 4000);
			if(reply != null)
			{
				return reply.Payload as WireProtocol.Commands.Monitor_FlashSectorMap.Reply;
			}
			return null;
		}

		public bool UpdateSignatureKey(PublicKeyIndex keyIndex, byte[] oldPublicKeySignature, byte[] newPublicKey, byte[] reserveData)
		{
			WireProtocol.Commands.Monitor_SignatureKeyUpdate keyUpdate = new WireProtocol.Commands.Monitor_SignatureKeyUpdate();

			// key must be 260 bytes
			if(keyUpdate.m_newPublicKey.Length != newPublicKey.Length)
				return false;

			if(!keyUpdate.PrepareForSend((uint)keyIndex, oldPublicKeySignature, newPublicKey, reserveData))
				return false;

			WireProtocol.IncomingMessage reply = SyncMessage(WireProtocol.Commands.c_Monitor_SignatureKeyUpdate, 0, keyUpdate);

			return WireProtocol.IncomingMessage.IsPositiveAcknowledge(reply);
		}

		public void SendRawBuffer(byte[] buf)
		{
			m_ctrl.SendRawBuffer(buf);
		}

		private bool ReadMemory(uint address, uint length, byte[] buf, uint offset)
		{
			while(length > 0)
			{
				WireProtocol.Commands.Monitor_ReadMemory cmd = new WireProtocol.Commands.Monitor_ReadMemory();

				cmd.m_address = address;
				cmd.m_length = length;

				WireProtocol.IncomingMessage reply = SyncMessage(WireProtocol.Commands.c_Monitor_ReadMemory, 0, cmd);
				if(reply == null)
					return false;

				WireProtocol.Commands.Monitor_ReadMemory.Reply cmdReply = reply.Payload as WireProtocol.Commands.Monitor_ReadMemory.Reply;
				if(cmdReply == null || cmdReply.m_data == null)
					return false;

				uint actualLength = System.Math.Min((uint)cmdReply.m_data.Length, length);

				Array.Copy(cmdReply.m_data, 0, buf, (int)offset, (int)actualLength);

				address += actualLength;
				length -= actualLength;
				offset += actualLength;
			}
			return true;
		}

		public bool ReadMemory(uint address, uint length, out byte[] buf)
		{
			buf = new byte[length];

			return ReadMemory(address, length, buf, 0);
		}

		public bool WriteMemory(uint address, byte[] buf, int offset, int length)
		{
			int count = length;
			int pos = offset;

			while(count > 0)
			{
				WireProtocol.Commands.Monitor_WriteMemory cmd = new WireProtocol.Commands.Monitor_WriteMemory();
				int len = System.Math.Min(1024, count);

				cmd.PrepareForSend(address, buf, pos, len);

				WireProtocol.IncomingMessage reply = SyncMessage(WireProtocol.Commands.c_Monitor_WriteMemory, 0, cmd);

				if(!WireProtocol.IncomingMessage.IsPositiveAcknowledge(reply))
					return false;

				address += (uint)len;
				count -= len;
				pos += len;
			}

			return true;
		}

		public bool WriteMemory(uint address, byte[] buf)
		{
			return WriteMemory(address, buf, 0, buf.Length);
		}

		public bool CheckSignature(byte[] signature, uint keyIndex)
		{
			WireProtocol.Commands.Monitor_Signature cmd = new WireProtocol.Commands.Monitor_Signature();

			cmd.PrepareForSend(signature, keyIndex);

			WireProtocol.IncomingMessage reply = SyncMessage(WireProtocol.Commands.c_Monitor_CheckSignature, 0, cmd, 0, 600000);

			return WireProtocol.IncomingMessage.IsPositiveAcknowledge(reply);
		}

		public bool EraseMemory(uint address, uint length)
		{
			WireProtocol.Commands.Monitor_EraseMemory cmd = new WireProtocol.Commands.Monitor_EraseMemory();

			cmd.m_address = address;
			cmd.m_length = length;

			WireProtocol.IncomingMessage reply = SyncMessage(WireProtocol.Commands.c_Monitor_EraseMemory, 0, cmd, 2, 10000);

			return WireProtocol.IncomingMessage.IsPositiveAcknowledge(reply);
		}

		public bool ExecuteMemory(uint address)
		{
			WireProtocol.Commands.Monitor_Execute cmd = new WireProtocol.Commands.Monitor_Execute();

			cmd.m_address = address;

			WireProtocol.IncomingMessage reply = SyncMessage(WireProtocol.Commands.c_Monitor_Execute, 0, cmd);

			return WireProtocol.IncomingMessage.IsPositiveAcknowledge(reply);
		}

		public void RebootDevice()
		{
			RebootDevice(RebootOption.NormalReboot);
		}

		public void RebootDevice(RebootOption option)
		{
			WireProtocol.Commands.Monitor_Reboot cmd = new WireProtocol.Commands.Monitor_Reboot();

			bool fThrowOnCommunicationFailureSav = this.m_fThrowOnCommunicationFailure;

			this.m_fThrowOnCommunicationFailure = false;

			switch(option)
			{
				case RebootOption.EnterBootloader:
					cmd.m_flags = WireProtocol.Commands.Monitor_Reboot.c_EnterBootloader;
					break;
				case RebootOption.RebootClrOnly:
					cmd.m_flags = this.Capabilities.SoftReboot ? WireProtocol.Commands.Monitor_Reboot.c_ClrRebootOnly : WireProtocol.Commands.Monitor_Reboot.c_NormalReboot;
					break;
				case RebootOption.RebootClrWaitForDebugger:
					cmd.m_flags = this.Capabilities.SoftReboot ? WireProtocol.Commands.Monitor_Reboot.c_ClrWaitForDbg : WireProtocol.Commands.Monitor_Reboot.c_NormalReboot;
					break;
				default:
					cmd.m_flags = WireProtocol.Commands.Monitor_Reboot.c_NormalReboot;
					break;
			}

			try
			{
				m_evtPing.Reset();
                
				SyncMessage(WireProtocol.Commands.c_Monitor_Reboot, 0, cmd);

				if(option != RebootOption.NoReconnect)
				{
					int timeout = 1000;

					if(m_portDefinition is PortDefinition_Tcp)
					{
						timeout = 2000;
					}
                    
					Thread.Sleep(timeout);
				}
			}
			finally
			{
				this.m_fThrowOnCommunicationFailure = fThrowOnCommunicationFailureSav;
			}

		}

		public bool TryToReconnect(bool fSoftReboot)
		{
			if(!TryToConnect(m_RebootTime.Retries, m_RebootTime.WaitMs(fSoftReboot), true, ConnectionSource.Unknown))
			{
				if(m_fThrowOnCommunicationFailure)
				{
					throw new ApplicationException("Could not reconnect to TinyCLR");
				}
				else
				{
					return false;
				}
			}

			return true;
		}

		public WireProtocol.Commands.Monitor_MemoryMap.Range[] MemoryMap()
		{
			WireProtocol.Commands.Monitor_MemoryMap cmd = new WireProtocol.Commands.Monitor_MemoryMap();

			WireProtocol.IncomingMessage reply = SyncMessage(WireProtocol.Commands.c_Monitor_MemoryMap, 0, cmd);

			if(reply != null)
			{
				WireProtocol.Commands.Monitor_MemoryMap.Reply cmdReply = reply.Payload as WireProtocol.Commands.Monitor_MemoryMap.Reply;

				if(cmdReply != null)
				{
					return cmdReply.m_map;
				}
			}

			return null;
		}

		public WireProtocol.Commands.Monitor_DeploymentMap.Reply DeploymentMap()
		{
			WireProtocol.Commands.Monitor_DeploymentMap cmd = new WireProtocol.Commands.Monitor_DeploymentMap();

			WireProtocol.IncomingMessage reply = SyncMessage(WireProtocol.Commands.c_Monitor_DeploymentMap, 0, cmd, 2, 10000);

			if(reply != null)
			{
				WireProtocol.Commands.Monitor_DeploymentMap.Reply cmdReply = reply.Payload as WireProtocol.Commands.Monitor_DeploymentMap.Reply;

				return cmdReply;
			}

			return null;
		}

		public bool GetExecutionBasePtr(out uint ee)
		{
			WireProtocol.IncomingMessage reply = SyncMessage(WireProtocol.Commands.c_Debugging_Execution_BasePtr, 0, null);
			if(reply != null)
			{
				WireProtocol.Commands.Debugging_Execution_BasePtr.Reply cmdReply = reply.Payload as WireProtocol.Commands.Debugging_Execution_BasePtr.Reply;

				if(cmdReply != null)
				{
					ee = cmdReply.m_EE;
					return true;
				}
			}

			ee = 0;
			return false;
		}

		public bool SetExecutionMode(uint iSet, uint iReset, out uint iCurrent)
		{
			WireProtocol.Commands.Debugging_Execution_ChangeConditions cmd = new WireProtocol.Commands.Debugging_Execution_ChangeConditions();

			cmd.m_set = iSet;
			cmd.m_reset = iReset;

			WireProtocol.IncomingMessage reply = SyncMessage(WireProtocol.Commands.c_Debugging_Execution_ChangeConditions, WireProtocol.Flags.c_NoCaching, cmd);
			if(reply != null)
			{
				WireProtocol.Commands.Debugging_Execution_ChangeConditions.Reply cmdReply = reply.Payload as WireProtocol.Commands.Debugging_Execution_ChangeConditions.Reply;

				if(cmdReply != null)
				{
					iCurrent = cmdReply.m_current;
				}
				else
				{
					iCurrent = 0;
				}

				return true;
			}

			iCurrent = 0;
			return false;
		}

		public bool SetExecutionMode(uint iSet, uint iReset)
		{
			uint iCurrent;

			return SetExecutionMode(iSet, iReset, out iCurrent);
		}

		public bool PauseExecution()
		{
			return SetExecutionMode(WireProtocol.Commands.Debugging_Execution_ChangeConditions.c_Stopped, 0);
		}

		public bool ResumeExecution()
		{
			return SetExecutionMode(0, WireProtocol.Commands.Debugging_Execution_ChangeConditions.c_Stopped);
		}

		public bool SetCurrentAppDomain(uint id)
		{
			WireProtocol.Commands.Debugging_Execution_SetCurrentAppDomain cmd = new WireProtocol.Commands.Debugging_Execution_SetCurrentAppDomain();

			cmd.m_id = id;

			return WireProtocol.IncomingMessage.IsPositiveAcknowledge(SyncMessage(WireProtocol.Commands.c_Debugging_Execution_SetCurrentAppDomain, 0, cmd));
		}

		public bool SetBreakpoints(WireProtocol.Commands.Debugging_Execution_BreakpointDef[] breakpoints)
		{
			WireProtocol.Commands.Debugging_Execution_Breakpoints cmd = new WireProtocol.Commands.Debugging_Execution_Breakpoints();

			cmd.m_data = breakpoints;

			return WireProtocol.IncomingMessage.IsPositiveAcknowledge(SyncMessage(WireProtocol.Commands.c_Debugging_Execution_Breakpoints, 0, cmd));
		}

		public WireProtocol.Commands.Debugging_Execution_BreakpointDef GetBreakpointStatus()
		{
			WireProtocol.Commands.Debugging_Execution_BreakpointStatus cmd = new WireProtocol.Commands.Debugging_Execution_BreakpointStatus();

			WireProtocol.IncomingMessage reply = SyncMessage(WireProtocol.Commands.c_Debugging_Execution_BreakpointStatus, 0, cmd);
			if(reply != null)
			{
				WireProtocol.Commands.Debugging_Execution_BreakpointStatus.Reply cmdReply = reply.Payload as WireProtocol.Commands.Debugging_Execution_BreakpointStatus.Reply;

				if(cmdReply != null)
					return cmdReply.m_lastHit;
			}

			return null;
		}

		public bool SetSecurityKey(byte[] key)
		{
			WireProtocol.Commands.Debugging_Execution_SecurityKey cmd = new WireProtocol.Commands.Debugging_Execution_SecurityKey();

			cmd.m_key = key;

			return SyncMessage(WireProtocol.Commands.c_Debugging_Execution_SecurityKey, 0, cmd) != null;
		}

		public bool UnlockDevice(byte[] blob)
		{
			WireProtocol.Commands.Debugging_Execution_Unlock cmd = new WireProtocol.Commands.Debugging_Execution_Unlock();

			Array.Copy(blob, 0, cmd.m_command, 0, 128);
			Array.Copy(blob, 128, cmd.m_hash, 0, 128);

			return WireProtocol.IncomingMessage.IsPositiveAcknowledge(SyncMessage(WireProtocol.Commands.c_Debugging_Execution_Unlock, 0, cmd));
		}

		public bool AllocateMemory(uint size, out uint address)
		{
			WireProtocol.Commands.Debugging_Execution_Allocate cmd = new WireProtocol.Commands.Debugging_Execution_Allocate();

			cmd.m_size = size;

			WireProtocol.IncomingMessage reply = SyncMessage(WireProtocol.Commands.c_Debugging_Execution_Allocate, 0, cmd);
			if(reply != null)
			{
				WireProtocol.Commands.Debugging_Execution_Allocate.Reply cmdReply = reply.Payload as WireProtocol.Commands.Debugging_Execution_Allocate.Reply;

				if(cmdReply != null)
				{
					address = cmdReply.m_address;
					return true;
				}
			}

			address = 0;
			return false;
		}

		public IAsyncResult UpgradeConnectionToSsl_Begin(X509Certificate2 cert, bool fRequireClientCert)
		{
			AsyncNetworkStream ans = ((IControllerLocal)m_ctrl).OpenPort() as AsyncNetworkStream;

			if(ans == null)
				return null;

			m_ctrl.StopProcessing();

			IAsyncResult iar = ans.BeginUpgradeToSSL(cert, fRequireClientCert);

			return iar;
		}

		public bool UpgradeConnectionToSSL_End(IAsyncResult iar)
		{
			AsyncNetworkStream ans = ((IControllerLocal)m_ctrl).OpenPort() as AsyncNetworkStream;

			if(ans == null)
				return false;

			bool result = ans.EndUpgradeToSSL(iar);

			m_ctrl.ResumeProcessing();

			return result;
		}

		public bool IsUsingSsl
		{
			get
			{
				if(!IsConnected)
					return false;

				AsyncNetworkStream ans = ((IControllerLocal)m_ctrl).OpenPort() as AsyncNetworkStream;

				if(ans == null)
					return false;

				return ans.IsUsingSsl;
			}
		}

		public bool CanUpgradeToSsl()
		{
			WireProtocol.Commands.Debugging_UpgradeToSsl cmd = new WireProtocol.Commands.Debugging_UpgradeToSsl();

			cmd.m_flags = 0;

			WireProtocol.IncomingMessage reply = SyncMessage(WireProtocol.Commands.c_Debugging_UpgradeToSsl, WireProtocol.Flags.c_NoCaching, cmd, 2, 5000);

			if(reply != null)
			{
				WireProtocol.Commands.Debugging_UpgradeToSsl.Reply cmdReply = reply.Payload as WireProtocol.Commands.Debugging_UpgradeToSsl.Reply;

				if(cmdReply != null)
				{
					return cmdReply.m_success != 0;
				}
			}

			return false;

		}

		Dictionary<int, uint[]> m_updateMissingPktTbl = new Dictionary<int, uint[]>();

		public bool StartUpdate(
			string provider, 
			ushort versionMajor, 
			ushort versionMinor, 
			uint updateId, 
			uint updateType, 
			uint updateSubType, 
			uint updateSize, 
			uint packetSize, 
			uint installAddress, 
			ref int updateHandle)
		{
			WireProtocol.Commands.Debugging_MFUpdate_Start cmd = new WireProtocol.Commands.Debugging_MFUpdate_Start();

			byte[] name = UTF8Encoding.UTF8.GetBytes(provider);

			Array.Copy(name, cmd.m_updateProvider, Math.Min(name.Length, cmd.m_updateProvider.Length));
			cmd.m_updateId = updateId;
			cmd.m_updateVerMajor = versionMajor;
			cmd.m_updateVerMinor = versionMinor;
			cmd.m_updateType = updateType;
			cmd.m_updateSubType = updateSubType;
			cmd.m_updateSize = updateSize;
			cmd.m_updatePacketSize = packetSize;

			WireProtocol.IncomingMessage reply = SyncMessage(WireProtocol.Commands.c_Debugging_MFUpdate_Start, WireProtocol.Flags.c_NoCaching, cmd, 2, 5000);
            
			if(reply != null)
			{
				WireProtocol.Commands.Debugging_MFUpdate_Start.Reply cmdReply = reply.Payload as WireProtocol.Commands.Debugging_MFUpdate_Start.Reply;

				if(cmdReply != null)
				{
					updateHandle = cmdReply.m_updateHandle;
					return (-1 != updateHandle);
				}
			}

			updateHandle = -1;
			return false;
		}

		public bool UpdateAuthCommand(int updateHandle, uint authCommand, byte[] commandArgs, ref byte[] response)
		{
			WireProtocol.Commands.Debugging_MFUpdate_AuthCommand cmd = new WireProtocol.Commands.Debugging_MFUpdate_AuthCommand();

			if(commandArgs == null)
				commandArgs = new byte[0];

			cmd.m_updateHandle = updateHandle;
			cmd.m_authCommand = authCommand;
			cmd.m_authArgs = commandArgs;
			cmd.m_authArgsSize = (uint)commandArgs.Length;

			WireProtocol.IncomingMessage reply = SyncMessage(WireProtocol.Commands.c_Debugging_MFUpdate_AuthCmd, WireProtocol.Flags.c_NoCaching, cmd);
            
			if(reply != null)
			{
				WireProtocol.Commands.Debugging_MFUpdate_AuthCommand.Reply cmdReply = reply.Payload as WireProtocol.Commands.Debugging_MFUpdate_AuthCommand.Reply;
            
				if(cmdReply != null && cmdReply.m_success != 0)
				{
					if(cmdReply.m_responseSize > 0)
					{
						Array.Copy(cmdReply.m_response, response, Math.Min(response.Length, cmdReply.m_responseSize));
					}
					return true;
				}
			}
            
			return false;
		}

		public bool UpdateAuthenticate(int updateHandle, byte[] authenticationData)
		{
			WireProtocol.Commands.Debugging_MFUpdate_Authenticate cmd = new WireProtocol.Commands.Debugging_MFUpdate_Authenticate();

			cmd.m_updateHandle = updateHandle;
			cmd.PrepareForSend(authenticationData);

			WireProtocol.IncomingMessage reply = SyncMessage(WireProtocol.Commands.c_Debugging_MFUpdate_Authenticate, WireProtocol.Flags.c_NoCaching, cmd);
            
			if(reply != null)
			{
				WireProtocol.Commands.Debugging_MFUpdate_Authenticate.Reply cmdReply = reply.Payload as WireProtocol.Commands.Debugging_MFUpdate_Authenticate.Reply;
            
				if(cmdReply != null && cmdReply.m_success != 0)
				{
					return true;
				}
			}
            
			return false;
		}

		private bool UpdateGetMissingPackets(int updateHandle)
		{
			WireProtocol.Commands.Debugging_MFUpdate_GetMissingPkts cmd = new WireProtocol.Commands.Debugging_MFUpdate_GetMissingPkts();

			cmd.m_updateHandle = updateHandle;

			WireProtocol.IncomingMessage reply = SyncMessage(WireProtocol.Commands.c_Debugging_MFUpdate_GetMissingPkts, WireProtocol.Flags.c_NoCaching, cmd);

			if(reply != null)
			{
				WireProtocol.Commands.Debugging_MFUpdate_GetMissingPkts.Reply cmdReply = reply.Payload as WireProtocol.Commands.Debugging_MFUpdate_GetMissingPkts.Reply;

				if(cmdReply != null && cmdReply.m_success != 0)
				{
					if(cmdReply.m_missingPktCount > 0)
					{
						m_updateMissingPktTbl[updateHandle] = cmdReply.m_missingPkts;
					}
					else
					{
						m_updateMissingPktTbl[updateHandle] = new uint[0];
					}
					return true;
				}
			}

			return false;
		}

		public bool AddPacket(int updateHandle, uint packetIndex, byte[] packetData, uint packetValidation)
		{
			if(!m_updateMissingPktTbl.ContainsKey(updateHandle))
			{
				UpdateGetMissingPackets(updateHandle);
			}

			if(m_updateMissingPktTbl.ContainsKey(updateHandle) && m_updateMissingPktTbl[updateHandle].Length > 0)
			{
				uint[] pktBits = m_updateMissingPktTbl[updateHandle];
				uint div = packetIndex >> 5;

				if(pktBits.Length > div)
				{
					if(0 == (pktBits[div] & (1u << (int)(packetIndex % 32))))
					{
						return true;
					}
				}
			}

			WireProtocol.Commands.Debugging_MFUpdate_AddPacket cmd = new WireProtocol.Commands.Debugging_MFUpdate_AddPacket();

			cmd.m_updateHandle = updateHandle;
			cmd.m_packetIndex = packetIndex;
			cmd.m_packetValidation = packetValidation;
			cmd.PrepareForSend(packetData);

			WireProtocol.IncomingMessage reply = SyncMessage(WireProtocol.Commands.c_Debugging_MFUpdate_AddPacket, WireProtocol.Flags.c_NoCaching, cmd);
			if(reply != null)
			{
				WireProtocol.Commands.Debugging_MFUpdate_AddPacket.Reply cmdReply = reply.Payload as WireProtocol.Commands.Debugging_MFUpdate_AddPacket.Reply;

				if(cmdReply != null)
				{
					return cmdReply.m_success != 0;
				}
			}

			return false;
		}

		public bool InstallUpdate(int updateHandle, byte[] validationData)
		{
			if(m_updateMissingPktTbl.ContainsKey(updateHandle))
			{
				m_updateMissingPktTbl.Remove(updateHandle);
			}

			WireProtocol.Commands.Debugging_MFUpdate_Install cmd = new WireProtocol.Commands.Debugging_MFUpdate_Install();

			cmd.m_updateHandle = updateHandle;

			cmd.PrepareForSend(validationData);

			WireProtocol.IncomingMessage reply = SyncMessage(WireProtocol.Commands.c_Debugging_MFUpdate_Install, WireProtocol.Flags.c_NoCaching, cmd);
			if(reply != null)
			{
				WireProtocol.Commands.Debugging_MFUpdate_Install.Reply cmdReply = reply.Payload as WireProtocol.Commands.Debugging_MFUpdate_Install.Reply;

				if(cmdReply != null)
				{
					return cmdReply.m_success != 0;
				}
			}

			return false;
		}

		public uint CreateThread(uint methodIndex, int scratchPadLocation)
		{
			return CreateThread(methodIndex, scratchPadLocation, 0);
		}

		public uint CreateThread(uint methodIndex, int scratchPadLocation, uint pid)
		{
			if(this.Capabilities.ThreadCreateEx)
			{
				WireProtocol.Commands.Debugging_Thread_CreateEx cmd = new WireProtocol.Commands.Debugging_Thread_CreateEx();

				cmd.m_md = methodIndex;
				cmd.m_scratchPad = scratchPadLocation;
				cmd.m_pid = pid;

				WireProtocol.IncomingMessage reply = SyncMessage(WireProtocol.Commands.c_Debugging_Thread_CreateEx, 0, cmd);
				if(reply != null)
				{
					WireProtocol.Commands.Debugging_Thread_CreateEx.Reply cmdReply = reply.Payload as WireProtocol.Commands.Debugging_Thread_CreateEx.Reply;

					return cmdReply.m_pid;
				}
			}

			return 0;
		}

		public uint[] GetThreadList()
		{
			WireProtocol.IncomingMessage reply = SyncMessage(WireProtocol.Commands.c_Debugging_Thread_List, 0, null);
			if(reply != null)
			{
				WireProtocol.Commands.Debugging_Thread_List.Reply cmdReply = reply.Payload as WireProtocol.Commands.Debugging_Thread_List.Reply;

				if(cmdReply != null)
				{
					return cmdReply.m_pids;
				}
			}

			return null;
		}

		public WireProtocol.Commands.Debugging_Thread_Stack.Reply GetThreadStack(uint pid)
		{
			WireProtocol.Commands.Debugging_Thread_Stack cmd = new WireProtocol.Commands.Debugging_Thread_Stack();

			cmd.m_pid = pid;

			WireProtocol.IncomingMessage reply = SyncMessage(WireProtocol.Commands.c_Debugging_Thread_Stack, 0, cmd);
			if(reply != null)
			{
				return reply.Payload as WireProtocol.Commands.Debugging_Thread_Stack.Reply;
			}

			return null;
		}

		public bool KillThread(uint pid)
		{
			WireProtocol.Commands.Debugging_Thread_Kill cmd = new WireProtocol.Commands.Debugging_Thread_Kill();

			cmd.m_pid = pid;

			WireProtocol.IncomingMessage reply = SyncMessage(WireProtocol.Commands.c_Debugging_Thread_Kill, 0, cmd);
			if(reply != null)
			{
				WireProtocol.Commands.Debugging_Thread_Kill.Reply cmdReply = reply.Payload as WireProtocol.Commands.Debugging_Thread_Kill.Reply;

				return cmdReply.m_result != 0;
			}

			return false;
		}

		public bool SuspendThread(uint pid)
		{
			WireProtocol.Commands.Debugging_Thread_Suspend cmd = new WireProtocol.Commands.Debugging_Thread_Suspend();

			cmd.m_pid = pid;

			return WireProtocol.IncomingMessage.IsPositiveAcknowledge(SyncMessage(WireProtocol.Commands.c_Debugging_Thread_Suspend, 0, cmd));
		}

		public bool ResumeThread(uint pid)
		{
			WireProtocol.Commands.Debugging_Thread_Resume cmd = new WireProtocol.Commands.Debugging_Thread_Resume();

			cmd.m_pid = pid;

			return WireProtocol.IncomingMessage.IsPositiveAcknowledge(SyncMessage(WireProtocol.Commands.c_Debugging_Thread_Resume, 0, cmd));
		}

		public RuntimeValue GetThreadException(uint pid)
		{
			WireProtocol.Commands.Debugging_Thread_GetException cmd = new WireProtocol.Commands.Debugging_Thread_GetException();

			cmd.m_pid = pid;

			return GetRuntimeValue(WireProtocol.Commands.c_Debugging_Thread_GetException, cmd);
		}

		public RuntimeValue GetThread(uint pid)
		{
			WireProtocol.Commands.Debugging_Thread_Get cmd = new WireProtocol.Commands.Debugging_Thread_Get();

			cmd.m_pid = pid;

			return GetRuntimeValue(WireProtocol.Commands.c_Debugging_Thread_Get, cmd);            
		}

		public bool UnwindThread(uint pid, uint depth)
		{
			WireProtocol.Commands.Debugging_Thread_Unwind cmd = new Microsoft.SPOT.Debugger.WireProtocol.Commands.Debugging_Thread_Unwind();

			cmd.m_pid = pid;
			cmd.m_depth = depth;

			return WireProtocol.IncomingMessage.IsPositiveAcknowledge(SyncMessage(WireProtocol.Commands.c_Debugging_Thread_Unwind, 0, cmd));
		}

		public bool SetIPOfStackFrame(uint pid, uint depth, uint IP, uint depthOfEvalStack)
		{
			WireProtocol.Commands.Debugging_Stack_SetIP cmd = new WireProtocol.Commands.Debugging_Stack_SetIP();

			cmd.m_pid = pid;
			cmd.m_depth = depth;

			cmd.m_IP = IP;
			cmd.m_depthOfEvalStack = depthOfEvalStack;

			return WireProtocol.IncomingMessage.IsPositiveAcknowledge(SyncMessage(WireProtocol.Commands.c_Debugging_Stack_SetIP, 0, cmd));
		}

		public WireProtocol.Commands.Debugging_Stack_Info.Reply GetStackInfo(uint pid, uint depth)
		{
			WireProtocol.Commands.Debugging_Stack_Info cmd = new WireProtocol.Commands.Debugging_Stack_Info();

			cmd.m_pid = pid;
			cmd.m_depth = depth;

			WireProtocol.IncomingMessage reply = SyncMessage(WireProtocol.Commands.c_Debugging_Stack_Info, 0, cmd);
			if(reply != null)
			{
				return reply.Payload as WireProtocol.Commands.Debugging_Stack_Info.Reply;
			}

			return null;
		}
		//--//
		public WireProtocol.Commands.Debugging_TypeSys_AppDomains.Reply GetAppDomains()
		{
			if(!Capabilities.AppDomains)
				return null;

			WireProtocol.Commands.Debugging_TypeSys_AppDomains cmd = new WireProtocol.Commands.Debugging_TypeSys_AppDomains();

			WireProtocol.IncomingMessage reply = SyncMessage(WireProtocol.Commands.c_Debugging_TypeSys_AppDomains, 0, cmd);
			if(reply != null)
			{
				return reply.Payload as WireProtocol.Commands.Debugging_TypeSys_AppDomains.Reply;
			}

			return null;
		}

		public WireProtocol.Commands.Debugging_TypeSys_Assemblies.Reply GetAssemblies()
		{
			WireProtocol.Commands.Debugging_TypeSys_Assemblies cmd = new WireProtocol.Commands.Debugging_TypeSys_Assemblies();

			WireProtocol.IncomingMessage reply = SyncMessage(WireProtocol.Commands.c_Debugging_TypeSys_Assemblies, 0, cmd);
			if(reply != null)
			{
				return reply.Payload as WireProtocol.Commands.Debugging_TypeSys_Assemblies.Reply;
			}

			return null;
		}

		public WireProtocol.Commands.Debugging_Resolve_Assembly[] ResolveAllAssemblies()
		{
			WireProtocol.Commands.Debugging_TypeSys_Assemblies.Reply assemblies = GetAssemblies();
			WireProtocol.Commands.Debugging_Resolve_Assembly[] resolveAssemblies = null;

			if(assemblies == null || assemblies.m_data == null)
			{
				resolveAssemblies = new WireProtocol.Commands.Debugging_Resolve_Assembly[0];
			}
			else
			{
				int cAssembly = assemblies.m_data.Length;
				WireProtocol.OutgoingMessage[] requests = new WireProtocol.OutgoingMessage[cAssembly];
				int iAssembly;

				for(iAssembly = 0; iAssembly < cAssembly; iAssembly++)
				{
					WireProtocol.Commands.Debugging_Resolve_Assembly cmd = new WireProtocol.Commands.Debugging_Resolve_Assembly();

					cmd.m_idx = assemblies.m_data[iAssembly];

					requests[iAssembly] = CreateMessage(WireProtocol.Commands.c_Debugging_Resolve_Assembly, 0, cmd);
				}

				WireProtocol.IncomingMessage[] replies = SyncMessages(requests);

				resolveAssemblies = new WireProtocol.Commands.Debugging_Resolve_Assembly[cAssembly];

				for(iAssembly = 0; iAssembly < cAssembly; iAssembly++)
				{
					resolveAssemblies[iAssembly] = requests[iAssembly].Payload as WireProtocol.Commands.Debugging_Resolve_Assembly;
					resolveAssemblies[iAssembly].m_reply = replies[iAssembly].Payload as WireProtocol.Commands.Debugging_Resolve_Assembly.Reply;
				}
			}

			return resolveAssemblies;
		}

		public WireProtocol.Commands.Debugging_Resolve_Assembly.Reply ResolveAssembly(uint idx)
		{
			WireProtocol.Commands.Debugging_Resolve_Assembly cmd = new WireProtocol.Commands.Debugging_Resolve_Assembly();

			cmd.m_idx = idx;

			WireProtocol.IncomingMessage reply = SyncMessage(WireProtocol.Commands.c_Debugging_Resolve_Assembly, 0, cmd);
			if(reply != null)
			{
				return reply.Payload as WireProtocol.Commands.Debugging_Resolve_Assembly.Reply;
			}

			return null;
		}

		public enum StackValueKind
		{
			Local = 0,
			Argument = 1,
			EvalStack = 2,
		}

		public bool GetStackFrameInfo(uint pid, uint depth, out uint numOfArguments, out uint numOfLocals, out uint depthOfEvalStack)
		{
			numOfArguments = 0;
			numOfLocals = 0;
			depthOfEvalStack = 0;

			WireProtocol.Commands.Debugging_Stack_Info.Reply reply = GetStackInfo(pid, depth);

			if(reply == null)
				return false;

			numOfArguments = reply.m_numOfArguments;
			numOfLocals = reply.m_numOfLocals;
			depthOfEvalStack = reply.m_depthOfEvalStack;

			return true;
		}

		private RuntimeValue GetRuntimeValue(uint msg, object cmd)
		{
			WireProtocol.IncomingMessage reply = SyncMessage(msg, 0, cmd);
			if(reply != null && reply.Payload != null)
			{
				WireProtocol.Commands.Debugging_Value_Reply cmdReply = reply.Payload as WireProtocol.Commands.Debugging_Value_Reply;

				return RuntimeValue.Convert(this, cmdReply.m_values);
			}

			return null;
		}

		internal RuntimeValue GetFieldValue(RuntimeValue val, uint offset, uint fd)
		{
			WireProtocol.Commands.Debugging_Value_GetField cmd = new WireProtocol.Commands.Debugging_Value_GetField();

			cmd.m_heapblock = (val == null ? 0 : val.m_handle.m_referenceID);
			cmd.m_offset = offset;
			cmd.m_fd = fd;

			return GetRuntimeValue(WireProtocol.Commands.c_Debugging_Value_GetField, cmd);
		}

		public RuntimeValue GetStaticFieldValue(uint fd)
		{
			return GetFieldValue(null, 0, fd);
		}

		internal RuntimeValue AssignRuntimeValue(uint heapblockSrc, uint heapblockDst)
		{
			WireProtocol.Commands.Debugging_Value_Assign cmd = new WireProtocol.Commands.Debugging_Value_Assign();

			cmd.m_heapblockSrc = heapblockSrc;
			cmd.m_heapblockDst = heapblockDst;

			return GetRuntimeValue(WireProtocol.Commands.c_Debugging_Value_Assign, cmd);
		}

		internal bool SetBlock(uint heapblock, uint dt, byte[] data)
		{
			WireProtocol.Commands.Debugging_Value_SetBlock setBlock = new WireProtocol.Commands.Debugging_Value_SetBlock();

			setBlock.m_heapblock = heapblock;
			setBlock.m_dt = dt;

			data.CopyTo(setBlock.m_value, 0);

			return WireProtocol.IncomingMessage.IsPositiveAcknowledge(SyncMessage(WireProtocol.Commands.c_Debugging_Value_SetBlock, 0, setBlock));
		}

		private WireProtocol.OutgoingMessage CreateMessage_GetValue_Stack(uint pid, uint depth, StackValueKind kind, uint index)
		{
			WireProtocol.Commands.Debugging_Value_GetStack cmd = new WireProtocol.Commands.Debugging_Value_GetStack();

			cmd.m_pid = pid;
			cmd.m_depth = depth;
			cmd.m_kind = (uint)kind;
			cmd.m_index = index;

			return CreateMessage(WireProtocol.Commands.c_Debugging_Value_GetStack, 0, cmd);
		}

		public bool ResizeScratchPad(int size)
		{
			WireProtocol.Commands.Debugging_Value_ResizeScratchPad cmd = new WireProtocol.Commands.Debugging_Value_ResizeScratchPad();

			cmd.m_size = size;

			return WireProtocol.IncomingMessage.IsPositiveAcknowledge(SyncMessage(WireProtocol.Commands.c_Debugging_Value_ResizeScratchPad, 0, cmd));
		}

		public RuntimeValue GetStackFrameValue(uint pid, uint depth, StackValueKind kind, uint index)
		{
			WireProtocol.OutgoingMessage cmd = CreateMessage_GetValue_Stack(pid, depth, kind, index);

			WireProtocol.IncomingMessage reply = SyncRequest(cmd, 10, 200);
			if(reply != null)
			{
				WireProtocol.Commands.Debugging_Value_Reply cmdReply = reply.Payload as WireProtocol.Commands.Debugging_Value_Reply;

				return RuntimeValue.Convert(this, cmdReply.m_values);
			}

			return null;
		}

		public RuntimeValue[] GetStackFrameValueAll(uint pid, uint depth, uint cValues, StackValueKind kind)
		{
			WireProtocol.OutgoingMessage[] cmds = new WireProtocol.OutgoingMessage[cValues];
			RuntimeValue[] vals = null;
			uint i;

			for(i = 0; i < cValues; i++)
			{
				cmds[i] = CreateMessage_GetValue_Stack(pid, depth, kind, i);
			}

			WireProtocol.IncomingMessage[] replies = SyncMessages(cmds);
			if(replies != null)
			{
				vals = new RuntimeValue[cValues];

				for(i = 0; i < cValues; i++)
				{
					WireProtocol.Commands.Debugging_Value_Reply reply = replies[i].Payload as WireProtocol.Commands.Debugging_Value_Reply;
					if(reply != null)
					{
						vals[i] = RuntimeValue.Convert(this, reply.m_values);
					}
				}
			}

			return vals;
		}

		public RuntimeValue GetArrayElement(uint arrayReferenceId, uint index)
		{
			WireProtocol.Commands.Debugging_Value_GetArray cmd = new WireProtocol.Commands.Debugging_Value_GetArray();

			cmd.m_heapblock = arrayReferenceId;
			cmd.m_index = index;

			RuntimeValue rtv = GetRuntimeValue(WireProtocol.Commands.c_Debugging_Value_GetArray, cmd);

			if(rtv != null)
			{
				rtv.m_handle.m_arrayref_referenceID = arrayReferenceId;
				rtv.m_handle.m_arrayref_index = index;
			}

			return rtv;
		}

		internal bool SetArrayElement(uint heapblock, uint index, byte[] data)
		{
			WireProtocol.Commands.Debugging_Value_SetArray cmd = new WireProtocol.Commands.Debugging_Value_SetArray();

			cmd.m_heapblock = heapblock;
			cmd.m_index = index;

			data.CopyTo(cmd.m_value, 0);

			return WireProtocol.IncomingMessage.IsPositiveAcknowledge(SyncMessage(WireProtocol.Commands.c_Debugging_Value_SetArray, 0, cmd));
		}

		public RuntimeValue GetScratchPadValue(int index)
		{
			WireProtocol.Commands.Debugging_Value_GetScratchPad cmd = new WireProtocol.Commands.Debugging_Value_GetScratchPad();

			cmd.m_index = index;

			return GetRuntimeValue(WireProtocol.Commands.c_Debugging_Value_GetScratchPad, cmd);
		}

		public RuntimeValue AllocateObject(int scratchPadLocation, uint td)
		{
			WireProtocol.Commands.Debugging_Value_AllocateObject cmd = new WireProtocol.Commands.Debugging_Value_AllocateObject();

			cmd.m_index = scratchPadLocation;
			cmd.m_td = td;

			return GetRuntimeValue(WireProtocol.Commands.c_Debugging_Value_AllocateObject, cmd);
		}

		public RuntimeValue AllocateString(int scratchPadLocation, string val)
		{
			WireProtocol.Commands.Debugging_Value_AllocateString cmd = new WireProtocol.Commands.Debugging_Value_AllocateString();

			cmd.m_index = scratchPadLocation;
			cmd.m_size = (uint)Encoding.UTF8.GetByteCount(val);

			RuntimeValue rtv = GetRuntimeValue(WireProtocol.Commands.c_Debugging_Value_AllocateString, cmd);

			if(rtv != null)
			{
				rtv.SetStringValue(val);
			}

			return rtv;
		}

		public RuntimeValue AllocateArray(int scratchPadLocation, uint td, int depth, int numOfElements)
		{
			WireProtocol.Commands.Debugging_Value_AllocateArray cmd = new WireProtocol.Commands.Debugging_Value_AllocateArray();

			cmd.m_index = scratchPadLocation;
			cmd.m_td = td;
			cmd.m_depth = (uint)depth;
			cmd.m_numOfElements = (uint)numOfElements;

			return GetRuntimeValue(WireProtocol.Commands.c_Debugging_Value_AllocateArray, cmd);
		}

		public WireProtocol.Commands.Debugging_Resolve_Type.Result ResolveType(uint td)
		{
			WireProtocol.Commands.Debugging_Resolve_Type.Result result = (WireProtocol.Commands.Debugging_Resolve_Type.Result)m_typeSysLookup.Lookup(TypeSysLookup.Type.Type, td);

			if(result == null)
			{
				WireProtocol.Commands.Debugging_Resolve_Type cmd = new WireProtocol.Commands.Debugging_Resolve_Type();

				cmd.m_td = td;

				WireProtocol.IncomingMessage reply = SyncMessage(WireProtocol.Commands.c_Debugging_Resolve_Type, 0, cmd);
				if(reply != null)
				{
					WireProtocol.Commands.Debugging_Resolve_Type.Reply cmdReply = reply.Payload as WireProtocol.Commands.Debugging_Resolve_Type.Reply;

					if(cmdReply != null)
					{
						result = new WireProtocol.Commands.Debugging_Resolve_Type.Result();

						result.m_name = WireProtocol.Commands.GetZeroTerminatedString(cmdReply.m_type, false);

						m_typeSysLookup.Add(TypeSysLookup.Type.Type, td, result);
					}
				}
			}

			return result;
		}

		public WireProtocol.Commands.Debugging_Resolve_Method.Result ResolveMethod(uint md)
		{
			WireProtocol.Commands.Debugging_Resolve_Method.Result result = (WireProtocol.Commands.Debugging_Resolve_Method.Result)m_typeSysLookup.Lookup(TypeSysLookup.Type.Method, md);
			;

			if(result == null)
			{
				WireProtocol.Commands.Debugging_Resolve_Method cmd = new WireProtocol.Commands.Debugging_Resolve_Method();

				cmd.m_md = md;

				WireProtocol.IncomingMessage reply = SyncMessage(WireProtocol.Commands.c_Debugging_Resolve_Method, 0, cmd);
				if(reply != null)
				{
					WireProtocol.Commands.Debugging_Resolve_Method.Reply cmdReply = reply.Payload as WireProtocol.Commands.Debugging_Resolve_Method.Reply;

					if(cmdReply != null)
					{
						result = new WireProtocol.Commands.Debugging_Resolve_Method.Result();

						result.m_name = WireProtocol.Commands.GetZeroTerminatedString(cmdReply.m_method, false);
						result.m_td = cmdReply.m_td;

						m_typeSysLookup.Add(TypeSysLookup.Type.Method, md, result);
					}
				}
			}

			return result;
		}

		public WireProtocol.Commands.Debugging_Resolve_Field.Result ResolveField(uint fd)
		{
			WireProtocol.Commands.Debugging_Resolve_Field.Result result = (WireProtocol.Commands.Debugging_Resolve_Field.Result)m_typeSysLookup.Lookup(TypeSysLookup.Type.Field, fd);
			;

			if(result == null)
			{
				WireProtocol.Commands.Debugging_Resolve_Field cmd = new WireProtocol.Commands.Debugging_Resolve_Field();

				cmd.m_fd = fd;

				WireProtocol.IncomingMessage reply = SyncMessage(WireProtocol.Commands.c_Debugging_Resolve_Field, 0, cmd);
				if(reply != null)
				{
					WireProtocol.Commands.Debugging_Resolve_Field.Reply cmdReply = reply.Payload as WireProtocol.Commands.Debugging_Resolve_Field.Reply;

					if(cmdReply != null)
					{
						result = new WireProtocol.Commands.Debugging_Resolve_Field.Result();

						result.m_name = WireProtocol.Commands.GetZeroTerminatedString(cmdReply.m_name, false);
						result.m_offset = cmdReply.m_offset;
						result.m_td = cmdReply.m_td;

						m_typeSysLookup.Add(TypeSysLookup.Type.Field, fd, result);
					}
				}
			}

			return result;
		}

		public WireProtocol.Commands.Debugging_Resolve_AppDomain.Reply ResolveAppDomain(uint appDomainID)
		{
			if(!Capabilities.AppDomains)
				return null;

			WireProtocol.Commands.Debugging_Resolve_AppDomain cmd = new WireProtocol.Commands.Debugging_Resolve_AppDomain();

			cmd.m_id = appDomainID;

			WireProtocol.IncomingMessage reply = SyncMessage(WireProtocol.Commands.c_Debugging_Resolve_AppDomain, 0, cmd);
			if(reply != null)
			{
				return reply.Payload as WireProtocol.Commands.Debugging_Resolve_AppDomain.Reply;
			}

			return null;
		}

		public string GetTypeName(uint td)
		{
			WireProtocol.Commands.Debugging_Resolve_Type.Result resolvedType = ResolveType(td);

			return (resolvedType != null) ? resolvedType.m_name : null;
		}

		public string GetMethodName(uint md, bool fIncludeType)
		{
			WireProtocol.Commands.Debugging_Resolve_Method.Result resolvedMethod = ResolveMethod(md);
			string name = null;

			if(resolvedMethod != null)
			{
				if(fIncludeType)
				{
					name = string.Format("{0}::{1}", GetTypeName(resolvedMethod.m_td), resolvedMethod.m_name);
				}
				else
				{
					name = resolvedMethod.m_name;
				}
			}

			return name;
		}

		public string GetFieldName(uint fd, out uint td, out uint offset)
		{
			WireProtocol.Commands.Debugging_Resolve_Field.Result resolvedField = ResolveField(fd);

			if(resolvedField != null)
			{
				td = resolvedField.m_td;
				offset = resolvedField.m_offset;

				return resolvedField.m_name;
			}

			td = 0;
			offset = 0;

			return null;
		}

		public uint GetVirtualMethod(uint md, RuntimeValue obj)
		{
			WireProtocol.Commands.Debugging_Resolve_VirtualMethod cmd = new WireProtocol.Commands.Debugging_Resolve_VirtualMethod();

			cmd.m_md = md;
			cmd.m_obj = obj.ReferenceId;

			WireProtocol.IncomingMessage reply = SyncMessage(WireProtocol.Commands.c_Debugging_Resolve_VirtualMethod, 0, cmd);
			if(reply != null)
			{
				WireProtocol.Commands.Debugging_Resolve_VirtualMethod.Reply cmdReply = reply.Payload as WireProtocol.Commands.Debugging_Resolve_VirtualMethod.Reply;

				if(cmdReply != null)
				{
					return cmdReply.m_md;
				}
			}

			return 0;
		}

		public bool GetFrameBuffer(out ushort widthInWords, out ushort heightInPixels, out uint[] buf)
		{
			WireProtocol.IncomingMessage reply = SyncMessage(WireProtocol.Commands.c_Debugging_Lcd_GetFrame, 0, null);
			if(reply != null)
			{
				WireProtocol.Commands.Debugging_Lcd_GetFrame.Reply cmdReply = reply.Payload as WireProtocol.Commands.Debugging_Lcd_GetFrame.Reply;

				if(cmdReply != null)
				{
					widthInWords = cmdReply.m_header.m_widthInWords;
					heightInPixels = cmdReply.m_header.m_heightInPixels;
					buf = cmdReply.m_data;
					return true;
				}
			}

			widthInWords = 0;
			heightInPixels = 0;
			buf = null;
			return false;
		}

		private void Adjust1bppOrientation(uint[] buf)
		{
			//CLR_GFX_Bitmap::AdjustBitOrientation
			//The TinyCLR treats 1bpp bitmaps reversed from Windows
			//And most likely every other 1bpp format as well
			byte[] reverseTable = new byte[] {
				0x00, 0x80, 0x40, 0xC0, 0x20, 0xA0, 0x60, 0xE0,
				0x10, 0x90, 0x50, 0xD0, 0x30, 0xB0, 0x70, 0xF0,
				0x08, 0x88, 0x48, 0xC8, 0x28, 0xA8, 0x68, 0xE8,
				0x18, 0x98, 0x58, 0xD8, 0x38, 0xB8, 0x78, 0xF8,
				0x04, 0x84, 0x44, 0xC4, 0x24, 0xA4, 0x64, 0xE4,
				0x14, 0x94, 0x54, 0xD4, 0x34, 0xB4, 0x74, 0xF4,
				0x0C, 0x8C, 0x4C, 0xCC, 0x2C, 0xAC, 0x6C, 0xEC,
				0x1C, 0x9C, 0x5C, 0xDC, 0x3C, 0xBC, 0x7C, 0xFC,
				0x02, 0x82, 0x42, 0xC2, 0x22, 0xA2, 0x62, 0xE2,
				0x12, 0x92, 0x52, 0xD2, 0x32, 0xB2, 0x72, 0xF2,
				0x0A, 0x8A, 0x4A, 0xCA, 0x2A, 0xAA, 0x6A, 0xEA,
				0x1A, 0x9A, 0x5A, 0xDA, 0x3A, 0xBA, 0x7A, 0xFA,
				0x06, 0x86, 0x46, 0xC6, 0x26, 0xA6, 0x66, 0xE6,
				0x16, 0x96, 0x56, 0xD6, 0x36, 0xB6, 0x76, 0xF6,
				0x0E, 0x8E, 0x4E, 0xCE, 0x2E, 0xAE, 0x6E, 0xEE,
				0x1E, 0x9E, 0x5E, 0xDE, 0x3E, 0xBE, 0x7E, 0xFE,
				0x01, 0x81, 0x41, 0xC1, 0x21, 0xA1, 0x61, 0xE1,
				0x11, 0x91, 0x51, 0xD1, 0x31, 0xB1, 0x71, 0xF1,
				0x09, 0x89, 0x49, 0xC9, 0x29, 0xA9, 0x69, 0xE9,
				0x19, 0x99, 0x59, 0xD9, 0x39, 0xB9, 0x79, 0xF9,
				0x05, 0x85, 0x45, 0xC5, 0x25, 0xA5, 0x65, 0xE5,
				0x15, 0x95, 0x55, 0xD5, 0x35, 0xB5, 0x75, 0xF5,
				0x0D, 0x8D, 0x4D, 0xCD, 0x2D, 0xAD, 0x6D, 0xED,
				0x1D, 0x9D, 0x5D, 0xDD, 0x3D, 0xBD, 0x7D, 0xFD,
				0x03, 0x83, 0x43, 0xC3, 0x23, 0xA3, 0x63, 0xE3,
				0x13, 0x93, 0x53, 0xD3, 0x33, 0xB3, 0x73, 0xF3,
				0x0B, 0x8B, 0x4B, 0xCB, 0x2B, 0xAB, 0x6B, 0xEB,
				0x1B, 0x9B, 0x5B, 0xDB, 0x3B, 0xBB, 0x7B, 0xFB,
				0x07, 0x87, 0x47, 0xC7, 0x27, 0xA7, 0x67, 0xE7,
				0x17, 0x97, 0x57, 0xD7, 0x37, 0xB7, 0x77, 0xF7,
				0x0F, 0x8F, 0x4F, 0xCF, 0x2F, 0xAF, 0x6F, 0xEF,
				0x1F, 0x9F, 0x5F, 0xDF, 0x3F, 0xBF, 0x7F, 0xFF,
			};

			unsafe
			{
				fixed (uint* pbuf = buf)
				{
					byte* ptr = (byte*)pbuf;

					for(int i = buf.Length * 4; i > 0; i--)
					{
						*ptr = reverseTable[*ptr];
						ptr++;
					}
				}
			}
		}

		public System.Drawing.Bitmap GetFrameBuffer()
		{
			ushort widthInWords;
			ushort heightInPixels;
			uint[] buf;

			System.Drawing.Bitmap bmp = null;


			System.Drawing.Imaging.PixelFormat pixelFormat = System.Drawing.Imaging.PixelFormat.DontCare;

			if(GetFrameBuffer(out widthInWords, out heightInPixels, out buf))
			{
				CLRCapabilities.LCDCapabilities lcdCaps = Capabilities.LCD;

				int pixelsPerWord = 32 / (int)lcdCaps.BitsPerPixel;

				System.Diagnostics.Debug.Assert(heightInPixels == lcdCaps.Height);
				System.Diagnostics.Debug.Assert(widthInWords == (lcdCaps.Width + pixelsPerWord - 1) / pixelsPerWord);

				System.Drawing.Color[] colors = null;

				switch(lcdCaps.BitsPerPixel)
				{
					case 1:
						pixelFormat = System.Drawing.Imaging.PixelFormat.Format1bppIndexed;
						colors = new System.Drawing.Color[] { System.Drawing.Color.White, System.Drawing.Color.Black };
						Adjust1bppOrientation(buf);
						break;
					case 4:
					case 8:
                        //Not tested
						int cColors = 1 << (int)lcdCaps.BitsPerPixel;

						pixelFormat = (lcdCaps.BitsPerPixel == 4) ? System.Drawing.Imaging.PixelFormat.Format4bppIndexed : System.Drawing.Imaging.PixelFormat.Format8bppIndexed;

						colors = new System.Drawing.Color[cColors];

						for(int i = 0; i < cColors; i++)
						{
							int intensity = 256 / cColors * i;
							colors[i] = System.Drawing.Color.FromArgb(intensity, intensity, intensity);
						}

						break;
					case 16:
						pixelFormat = System.Drawing.Imaging.PixelFormat.Format16bppRgb565;
						break;
					default:
						System.Diagnostics.Debug.Assert(false);
						return null;
				}

				System.Drawing.Imaging.BitmapData bitmapData = null;

				try
				{
					bmp = new System.Drawing.Bitmap((int)lcdCaps.Width, (int)lcdCaps.Height, pixelFormat);
					System.Drawing.Rectangle rect = new System.Drawing.Rectangle(0, 0, (int)lcdCaps.Width, (int)lcdCaps.Height);

					if(colors != null)
					{
						System.Drawing.Imaging.ColorPalette palette = bmp.Palette;
						colors.CopyTo(palette.Entries, 0);
						bmp.Palette = palette;
					}

					bitmapData = bmp.LockBits(rect, System.Drawing.Imaging.ImageLockMode.WriteOnly, pixelFormat);
					IntPtr data = bitmapData.Scan0;

					unsafe
					{
						fixed (uint* pbuf = buf)
						{
							uint* src = (uint*)pbuf;
							uint* dst = (uint*)data.ToPointer();

							for(int i = buf.Length; i > 0; i--)
							{
								*dst = *src;
								dst++;
								src++;
							}

						}
					}
				}
				finally
				{
					if(bitmapData != null)
					{
						bmp.UnlockBits(bitmapData);
					}
				}
			}

			return bmp;
		}

		public void InjectButtons(uint pressed, uint released)
		{
			WireProtocol.Commands.Debugging_Button_Inject cmd = new WireProtocol.Commands.Debugging_Button_Inject();

			cmd.m_pressed = pressed;
			cmd.m_released = released;

			SyncMessage(WireProtocol.Commands.c_Debugging_Button_Inject, 0, cmd);
		}

		public ArrayList GetThreads()
		{
			ArrayList threads = new ArrayList();
			uint[] pids = GetThreadList();

			if(pids != null)
			{
				for(int i = 0; i < pids.Length; i++)
				{
					WireProtocol.Commands.Debugging_Thread_Stack.Reply reply = GetThreadStack(pids[i]);

					if(reply != null)
					{
						int depth = reply.m_data.Length;
						ThreadStatus ts = new ThreadStatus();

						ts.m_pid = pids[i];
						ts.m_status = reply.m_status;
						ts.m_flags = reply.m_flags;
						ts.m_calls = new string[depth];

						for(int j = 0; j < depth; j++)
						{
							ts.m_calls[depth - 1 - j] = String.Format("{0} [IP:{1:X4}]", GetMethodName(reply.m_data[j].m_md, true), reply.m_data[j].m_IP);
						}

						threads.Add(ts);
					}
				}

				return threads;
			}

			return null;
		}

		public bool Deployment_GetStatus(out uint entrypoint, out uint storageStart, out uint storageLength)
		{
			WireProtocol.Commands.Debugging_Deployment_Status.Reply status = Deployment_GetStatus();

			if(status != null)
			{
				entrypoint = status.m_entryPoint;
				storageStart = status.m_storageStart;
				storageLength = status.m_storageLength;

				return true;
			}
			else
			{
				entrypoint = 0;
				storageStart = 0;
				storageLength = 0;

				return false;
			}
		}

		public WireProtocol.Commands.Debugging_Deployment_Status.Reply Deployment_GetStatus()
		{
			WireProtocol.Commands.Debugging_Deployment_Status cmd = new WireProtocol.Commands.Debugging_Deployment_Status();
			WireProtocol.Commands.Debugging_Deployment_Status.Reply cmdReply = null;

			WireProtocol.IncomingMessage reply = SyncMessage(WireProtocol.Commands.c_Debugging_Deployment_Status, WireProtocol.Flags.c_NoCaching, cmd, 2, 10000);
			if(reply != null)
			{
				cmdReply = reply.Payload as WireProtocol.Commands.Debugging_Deployment_Status.Reply;
			}

			return cmdReply;
		}

		public bool Info_SetJMC(bool fJMC, ReflectionDefinition.Kind kind, uint index)
		{
			WireProtocol.Commands.Debugging_Info_SetJMC cmd = new WireProtocol.Commands.Debugging_Info_SetJMC();

			cmd.m_fIsJMC = (uint)(fJMC ? 1 : 0);
			cmd.m_kind = (uint)kind;
			cmd.m_raw = index;

			return WireProtocol.IncomingMessage.IsPositiveAcknowledge(SyncMessage(WireProtocol.Commands.c_Debugging_Info_SetJMC, 0, cmd));
		}

		private bool Deployment_Execute_Incremental(ArrayList assemblies, MessageHandler mh)
		{
			WireProtocol.Commands.Debugging_Deployment_Status.ReplyEx status = Deployment_GetStatus() as WireProtocol.Commands.Debugging_Deployment_Status.ReplyEx;

			if(status == null)
				return false;

			WireProtocol.Commands.Debugging_Deployment_Status.FlashSector[] sectors = status.m_data;

			int iAssembly = 0;

			//The amount of bytes that the deployment will take
			uint deployLength = 0;

			//Compute size of assemblies to deploy
			for(iAssembly = 0; iAssembly < assemblies.Count; iAssembly++)
			{
				byte[] assembly = (byte[])assemblies[iAssembly];
				deployLength += (uint)assembly.Length;
			}

			if(deployLength > status.m_storageLength)
			{
				if(mh != null)
					mh(string.Format("Deployment storage (size: {0} bytes) was not large enough to fit deployment assemblies (size: {1} bytes)", status.m_storageLength, deployLength));        
				return false;
			}

			//Compute maximum sector size
			uint maxSectorSize = 0;

			for(int iSector = 0; iSector < sectors.Length; iSector++)
			{
				maxSectorSize = Math.Max(maxSectorSize, sectors[iSector].m_length);
			}

			//pre-allocate sector data, and a buffer to hold an empty sector's data
			byte[] sectorData = new byte[maxSectorSize];
			byte[] sectorDataErased = new byte[maxSectorSize];

			Debug.Assert(status.m_eraseWord == 0 || status.m_eraseWord == 0xffffffff);

			byte bErase = (status.m_eraseWord == 0) ? (byte)0 : (byte)0xff;
			if(bErase != 0)
			{
				//Fill in data for what an empty sector looks like
				for(int i = 0; i < maxSectorSize; i++)
				{
					sectorDataErased[i] = bErase;
				}
			}

			uint bytesDeployed = 0;

			//The assembly we are using
			iAssembly = 0;
			//byte index into the assembly remaining to deploy
			uint iAssemblyIndex = 0;
			//deploy each sector, one at a time
			for(int iSector = 0; iSector < sectors.Length; iSector++)
			{
				WireProtocol.Commands.Debugging_Deployment_Status.FlashSector sector = sectors[iSector];

				uint cBytesLeftInSector = sector.m_length;
				//byte index into the sector that we are deploying to.
				uint iSectorIndex = 0;

				//fill sector with deployment data
				while(cBytesLeftInSector > 0 && iAssembly < assemblies.Count)
				{
					byte[] assembly = (byte[])assemblies[iAssembly];

					uint cBytesLeftInAssembly = (uint)assembly.Length - iAssemblyIndex;

					//number of bytes from current assembly to deploy in this sector
					uint cBytes = Math.Min(cBytesLeftInSector, cBytesLeftInAssembly);

					Array.Copy(assembly, iAssemblyIndex, sectorData, iSectorIndex, cBytes);

					cBytesLeftInSector -= cBytes;
					iAssemblyIndex += cBytes;
					iSectorIndex += cBytes;

					//Is assembly finished?
					if(iAssemblyIndex == assembly.Length)
					{
						//Next assembly
						iAssembly++;
						iAssemblyIndex = 0;

						//If there is enough room to waste the remainder of this sector, do so
						//to allow for incremental deployment, if this assembly changes for next deployment
						if(deployLength + cBytesLeftInSector <= status.m_storageLength)
						{
							deployLength += cBytesLeftInSector;
							break;
						}
					}
				}

				uint crc = WireProtocol.Commands.Debugging_Deployment_Status.c_CRC_Erased_Sentinel;

				if(iSectorIndex > 0)
				{
					//Fill in the rest with erased value
					Array.Copy(sectorDataErased, iSectorIndex, sectorData, iSectorIndex, cBytesLeftInSector);

					crc = CRC.ComputeCRC(sectorData, 0, (int)sector.m_length, 0);
				}

				//Has the data changed from what is in this sector
				if(sector.m_crc != crc)
				{
					//Is the data not erased
					if(sector.m_crc != WireProtocol.Commands.Debugging_Deployment_Status.c_CRC_Erased_Sentinel)
					{
						if(!this.EraseMemory(sector.m_start, sector.m_length))
						{
							return false;
						}

#if DEBUG
                        WireProtocol.Commands.Debugging_Deployment_Status.ReplyEx statusT = Deployment_GetStatus() as WireProtocol.Commands.Debugging_Deployment_Status.ReplyEx;
                        Debug.Assert(statusT != null);
                        Debug.Assert(statusT.m_data[iSector].m_crc == WireProtocol.Commands.Debugging_Deployment_Status.c_CRC_Erased_Sentinel);
#endif
					}

					//Is there anything to deploy
					if(iSectorIndex > 0)
					{
						bytesDeployed += iSectorIndex;
                        
						if(!this.WriteMemory(sector.m_start, sectorData, 0, (int)iSectorIndex))
						{
							return false;
						}
#if DEBUG
                        WireProtocol.Commands.Debugging_Deployment_Status.ReplyEx statusT = Deployment_GetStatus() as WireProtocol.Commands.Debugging_Deployment_Status.ReplyEx;
                        Debug.Assert(statusT != null);
                        Debug.Assert(statusT.m_data[iSector].m_crc == crc);
                        //Assert the data we are deploying is not sentinel value
                        Debug.Assert(crc != WireProtocol.Commands.Debugging_Deployment_Status.c_CRC_Erased_Sentinel);
#endif
					}
				}
			}

			if(mh != null)
			{
				if(bytesDeployed == 0)
				{
					mh("All assemblies on the device are up to date.  No assembly deployment was necessary.");        
				}
				else
				{
					mh(string.Format("Deploying assemblies for a total size of {0} bytes", bytesDeployed));        
				}
			}

			return true;
		}

		private bool Deployment_Execute_Full(ArrayList assemblies, MessageHandler mh)
		{
			uint entrypoint;
			uint storageStart;
			uint storageLength;
			uint deployLength;
			byte[] closeHeader = new byte[8];

			if(!Deployment_GetStatus(out entrypoint, out storageStart, out storageLength))
				return false;

			if(storageLength == 0)
				return false;

			deployLength = (uint)closeHeader.Length;

			foreach(byte[] assembly in assemblies)
			{
				deployLength += (uint)assembly.Length;
			}

			if(mh != null)
				mh(string.Format("Deploying assemblies for a total size of {0} bytes", deployLength));                

			if(deployLength > storageLength)
				return false;

			if(!EraseMemory(storageStart, deployLength))
				return false;

			foreach(byte[] assembly in assemblies)
			{
				//
				// Only word-aligned assemblies are allowed.
				//
				if(assembly.Length % 4 != 0)
					return false;

				if(!WriteMemory(storageStart, assembly))
					return false;

				storageStart += (uint)assembly.Length;
			}

			if(!WriteMemory(storageStart, closeHeader))
				return false;

			return true;
		}

		public delegate void MessageHandler(String msg);

		public bool Deployment_Execute(ArrayList assemblies)
		{
			return this.Deployment_Execute(assemblies, true, null);
		}

		public bool Deployment_Execute(ArrayList assemblies, bool fRebootAfterDeploy, MessageHandler mh)
		{
			bool fDeployedOK = false;

			if(!PauseExecution())
				return false;

			if(this.Capabilities.IncrementalDeployment)
			{
				if(mh != null)
					mh("Incrementally deploying assemblies to device");
				fDeployedOK = Deployment_Execute_Incremental(assemblies, mh);
			}
			else
			{
				if(mh != null)
					mh("Deploying assemblies to device");
				fDeployedOK = Deployment_Execute_Full(assemblies, mh);
			}

			if(!fDeployedOK)
			{
				if(mh != null)
					mh("Assemblies not successfully deployed to device.");
			}
			else
			{
				if(mh != null)
					mh("Assemblies successfully deployed to device.");
				if(fRebootAfterDeploy)
				{
					if(mh != null)
						mh("Rebooting device...");
					RebootDevice(RebootOption.RebootClrOnly);
				}
			}

			return fDeployedOK;
		}

		public bool SetProfilingMode(uint iSet, uint iReset, out uint iCurrent)
		{
			WireProtocol.Commands.Profiling_Command cmd = new WireProtocol.Commands.Profiling_Command();
			cmd.m_command = WireProtocol.Commands.Profiling_Command.c_Command_ChangeConditions;
			cmd.m_parm1 = iSet;
			cmd.m_parm2 = iReset;

			WireProtocol.IncomingMessage reply = SyncMessage(WireProtocol.Commands.c_Profiling_Command, 0, cmd);
			if(reply != null)
			{
				WireProtocol.Commands.Profiling_Command.Reply cmdReply = reply.Payload as WireProtocol.Commands.Profiling_Command.Reply;

				if(cmdReply != null)
				{
					iCurrent = cmdReply.m_raw;
				}
				else
				{
					iCurrent = 0;
				}

				return true;
			}

			iCurrent = 0;
			return false;
		}

		public bool SetProfilingMode(uint iSet, uint iReset)
		{
			uint iCurrent;

			return SetProfilingMode(iSet, iReset, out iCurrent);
		}

		public bool FlushProfilingStream()
		{
			WireProtocol.Commands.Profiling_Command cmd = new WireProtocol.Commands.Profiling_Command();
			cmd.m_command = WireProtocol.Commands.Profiling_Command.c_Command_FlushStream;
			SyncMessage(WireProtocol.Commands.c_Profiling_Command, 0, cmd);
			return true;
		}

		private WireProtocol.IncomingMessage DiscoverCLRCapability(uint caps)
		{
			WireProtocol.Commands.Debugging_Execution_QueryCLRCapabilities cmd = new WireProtocol.Commands.Debugging_Execution_QueryCLRCapabilities();

			cmd.m_caps = caps;

			return SyncMessage(WireProtocol.Commands.c_Debugging_Execution_QueryCLRCapabilities, 0, cmd);
		}

		private uint DiscoverCLRCapabilityUint(uint caps)
		{
			uint ret = 0;

			WireProtocol.IncomingMessage reply = DiscoverCLRCapability(caps);

			if(reply != null)
			{
				WireProtocol.Commands.Debugging_Execution_QueryCLRCapabilities.Reply cmdReply = reply.Payload as WireProtocol.Commands.Debugging_Execution_QueryCLRCapabilities.Reply;

				if(cmdReply != null && cmdReply.m_data != null && cmdReply.m_data.Length == 4)
				{
					object obj = (object)ret;

					new WireProtocol.Converter().Deserialize(obj, cmdReply.m_data);

					ret = (uint)obj;
				}
			}

			return ret;
		}

		private CLRCapabilities.Capability DiscoverCLRCapabilityFlags()
		{
			return (CLRCapabilities.Capability)DiscoverCLRCapabilityUint(WireProtocol.Commands.Debugging_Execution_QueryCLRCapabilities.c_CapabilityFlags);
		}

		private CLRCapabilities.SoftwareVersionProperties DiscoverSoftwareVersionProperties()
		{
			WireProtocol.IncomingMessage reply = DiscoverCLRCapability(WireProtocol.Commands.Debugging_Execution_QueryCLRCapabilities.c_CapabilitySoftwareVersion);

			WireProtocol.Commands.Debugging_Execution_QueryCLRCapabilities.SoftwareVersion ver = new Microsoft.SPOT.Debugger.WireProtocol.Commands.Debugging_Execution_QueryCLRCapabilities.SoftwareVersion();

			CLRCapabilities.SoftwareVersionProperties verCaps = new CLRCapabilities.SoftwareVersionProperties();

			if(reply != null)
			{
				WireProtocol.Commands.Debugging_Execution_QueryCLRCapabilities.Reply cmdReply = reply.Payload as WireProtocol.Commands.Debugging_Execution_QueryCLRCapabilities.Reply;

				if(cmdReply != null && cmdReply.m_data != null)
				{
					new WireProtocol.Converter().Deserialize(ver, cmdReply.m_data);

					verCaps = new CLRCapabilities.SoftwareVersionProperties(ver.m_buildDate, ver.m_compilerVersion);
				}
			}

			return verCaps;
		}

		private CLRCapabilities.LCDCapabilities DiscoverCLRCapabilityLCD()
		{
			WireProtocol.IncomingMessage reply = DiscoverCLRCapability(WireProtocol.Commands.Debugging_Execution_QueryCLRCapabilities.c_CapabilityLCD);

			WireProtocol.Commands.Debugging_Execution_QueryCLRCapabilities.LCD lcd = new Microsoft.SPOT.Debugger.WireProtocol.Commands.Debugging_Execution_QueryCLRCapabilities.LCD();

			CLRCapabilities.LCDCapabilities lcdCaps = new CLRCapabilities.LCDCapabilities();

			if(reply != null)
			{
				WireProtocol.Commands.Debugging_Execution_QueryCLRCapabilities.Reply cmdReply = reply.Payload as WireProtocol.Commands.Debugging_Execution_QueryCLRCapabilities.Reply;

				if(cmdReply != null && cmdReply.m_data != null)
				{
					new WireProtocol.Converter().Deserialize(lcd, cmdReply.m_data);

					lcdCaps = new CLRCapabilities.LCDCapabilities(lcd.m_width, lcd.m_height, lcd.m_bpp);
				}
			}

			return lcdCaps;
		}

		private CLRCapabilities.HalSystemInfoProperties DiscoverHalSystemInfoProperties()
		{
			WireProtocol.IncomingMessage reply = DiscoverCLRCapability(WireProtocol.Commands.Debugging_Execution_QueryCLRCapabilities.c_CapabilityHalSystemInfo);

			WireProtocol.Commands.Debugging_Execution_QueryCLRCapabilities.HalSystemInfo halSystemInfo = new Microsoft.SPOT.Debugger.WireProtocol.Commands.Debugging_Execution_QueryCLRCapabilities.HalSystemInfo();

			CLRCapabilities.HalSystemInfoProperties halProps = new CLRCapabilities.HalSystemInfoProperties();

			if(reply != null)
			{
				WireProtocol.Commands.Debugging_Execution_QueryCLRCapabilities.Reply cmdReply = reply.Payload as WireProtocol.Commands.Debugging_Execution_QueryCLRCapabilities.Reply;

				if(cmdReply != null && cmdReply.m_data != null)
				{
					new WireProtocol.Converter().Deserialize(halSystemInfo, cmdReply.m_data);

					halProps = new CLRCapabilities.HalSystemInfoProperties(
						halSystemInfo.m_releaseInfo.Version, halSystemInfo.m_releaseInfo.Info,
						halSystemInfo.m_OemModelInfo.OEM, halSystemInfo.m_OemModelInfo.Model, halSystemInfo.m_OemModelInfo.SKU,
						halSystemInfo.m_OemSerialNumbers.module_serial_number, halSystemInfo.m_OemSerialNumbers.system_serial_number
					);
				}
			}

			return halProps;
		}

		private CLRCapabilities.ClrInfoProperties DiscoverClrInfoProperties()
		{
			WireProtocol.IncomingMessage reply = DiscoverCLRCapability(WireProtocol.Commands.Debugging_Execution_QueryCLRCapabilities.c_CapabilityClrInfo);

			WireProtocol.Commands.Debugging_Execution_QueryCLRCapabilities.ClrInfo clrInfo = new Microsoft.SPOT.Debugger.WireProtocol.Commands.Debugging_Execution_QueryCLRCapabilities.ClrInfo();

			CLRCapabilities.ClrInfoProperties clrInfoProps = new CLRCapabilities.ClrInfoProperties();

			if(reply != null)
			{
				WireProtocol.Commands.Debugging_Execution_QueryCLRCapabilities.Reply cmdReply = reply.Payload as WireProtocol.Commands.Debugging_Execution_QueryCLRCapabilities.Reply;

				if(cmdReply != null && cmdReply.m_data != null)
				{
					new WireProtocol.Converter().Deserialize(clrInfo, cmdReply.m_data);

					clrInfoProps = new CLRCapabilities.ClrInfoProperties(clrInfo.m_clrReleaseInfo.Version, clrInfo.m_clrReleaseInfo.Info, clrInfo.m_TargetFrameworkVersion.Version);
				}
			}

			return clrInfoProps;
		}

		private CLRCapabilities.SolutionInfoProperties DiscoverSolutionInfoProperties()
		{
			WireProtocol.IncomingMessage reply = DiscoverCLRCapability(WireProtocol.Commands.Debugging_Execution_QueryCLRCapabilities.c_CapabilitySolutionReleaseInfo);

			WireProtocol.ReleaseInfo solutionInfo = new Microsoft.SPOT.Debugger.WireProtocol.ReleaseInfo();

			CLRCapabilities.SolutionInfoProperties solInfProps = new CLRCapabilities.SolutionInfoProperties();

			if(reply != null)
			{
				WireProtocol.Commands.Debugging_Execution_QueryCLRCapabilities.Reply cmdReply = reply.Payload as WireProtocol.Commands.Debugging_Execution_QueryCLRCapabilities.Reply;

				if(cmdReply != null && cmdReply.m_data != null)
				{
					new WireProtocol.Converter().Deserialize(solutionInfo, cmdReply.m_data);

					solInfProps = new CLRCapabilities.SolutionInfoProperties(solutionInfo.Version, solutionInfo.Info);
				}
			}

			return solInfProps;
		}

		private CLRCapabilities DiscoverCLRCapabilities()
		{
			return new CLRCapabilities(
				DiscoverCLRCapabilityFlags(),
				DiscoverCLRCapabilityLCD(),
				DiscoverSoftwareVersionProperties(),
				DiscoverHalSystemInfoProperties(),
				DiscoverClrInfoProperties(),
				DiscoverSolutionInfoProperties()
			);
		}

		~Engine()
		{
			Dispose(false);
		}

		public void Dispose()
		{
			Dispose(true);

			GC.SuppressFinalize(this);
		}

		private void Dispose(bool fDisposing)
		{
			try
			{
				Stop();

				if(m_state.SetValue(State.Value.Disposing))
				{
					IDisposable disposable = m_ctrl as IDisposable;

					if(disposable != null)
					{
						disposable.Dispose();
					}

					m_state.SetValue(State.Value.Disposed);
				}
			}
			catch
			{
			}
		}
	}
	public delegate void ConsoleOutputEventHandler(string text);
	public class Emulator
	{
		string m_exe;
		string m_args;
		string m_pipe;
		Process m_proc;
		public bool Verbose = true;

		event ConsoleOutputEventHandler m_eventOutput;
		event ConsoleOutputEventHandler m_eventError;

		public Emulator(string exe, ArrayList args)
		{
			ArrayList lst = new ArrayList();
			int i;

			for(i = 0; i < args.Count; i++)
			{
				string arg = (string)args[i];

				lst.Add("\"" + arg.Replace("\"", "\\\"") + "\"");
			}

			m_exe = exe;
			m_args = String.Join(" ", (string[])lst.ToArray(typeof(string)));
		}

		public PortDefinition_Emulator CreatePortDefinition()
		{
			if(m_pipe == null)
			{
				throw new ApplicationException("Emulator not started yet -- not pipe created");
			}

			return new PortDefinition_Emulator(m_pipe, m_pipe, 0);
		}

		public static PortDefinition[] EnumeratePipes()
		{
			SortedList lst = new SortedList();
			Regex re = new Regex("^TinyCLR_([0-9]+)_Port1$");

			try
			{
				String[] pipeNames = Directory.GetFiles(@"\\.\pipe");

				foreach(string pipe in pipeNames)
				{
					try
					{
						if(re.IsMatch(Path.GetFileName(pipe)))
						{
							int pid = Int32.Parse(re.Match(Path.GetFileName(pipe)).Groups[1].Value);
							PortDefinition pd = PortDefinition.CreateInstanceForEmulator("Emulator - pid " + pid, pipe, pid);

							lst.Add(pd.DisplayName, pd);
						}
					}
					catch
					{
					}
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

		public event ConsoleOutputEventHandler OnStandardOutput
		{
			add
			{
				m_eventOutput += value;
			}

			remove
			{
				m_eventOutput -= value;
			}
		}

		public event ConsoleOutputEventHandler OnStandardError
		{
			add
			{
				m_eventError += value;
			}

			remove
			{
				m_eventError -= value;
			}
		}

		public Process Process
		{
			get
			{
				return m_proc;
			}
		}

		public void Start(ProcessStartInfo psi)
		{
			psi.FileName = m_exe;
			psi.Arguments = m_args;

			m_proc = new Process();
			m_proc.StartInfo = psi;

			if(psi.RedirectStandardOutput)
			{
				m_proc.OutputDataReceived += new DataReceivedEventHandler(StandardOutputHandler);
			}

			if(psi.RedirectStandardError)
			{
				m_proc.ErrorDataReceived += new DataReceivedEventHandler(StandardErrorHandler);
			}

			m_proc.Start();

			if(psi.RedirectStandardOutput)
			{
				m_proc.BeginOutputReadLine();
			}

			if(psi.RedirectStandardError)
			{
				m_proc.BeginErrorReadLine();
			}


			m_pipe = PortDefinition_Emulator.PipeNameFromPid(m_proc.Id);
		}

		public void Start()
		{
			if(Verbose)
			{
				Console.WriteLine("Launching '{0}' with params '{1}'", m_exe, m_args);
			}

			ProcessStartInfo psi = new ProcessStartInfo();

			psi.RedirectStandardOutput = true;
			psi.RedirectStandardError = true;
			psi.UseShellExecute = false;
			psi.WorkingDirectory = Directory.GetCurrentDirectory();

			Start(psi);
		}

		public void WaitForExit()
		{
			m_proc.WaitForExit();
		}

		public void Stop()
		{
			if(m_proc != null)
			{
				if(m_proc.HasExited == false)
				{
					try
					{
						m_proc.Kill();
					}
					catch
					{
					}
				}

				m_proc = null;
			}
		}

		private void StandardOutputHandler(object sendingProcess, DataReceivedEventArgs outLine)
		{
			m_eventOutput(outLine.Data);
		}

		private void StandardErrorHandler(object sendingProcess, DataReceivedEventArgs outLine)
		{
			m_eventError(outLine.Data);
		}
	}

	[Serializable]
	public class PortDefinition_Emulator : PortDefinition
	{
		int m_pid;

		public PortDefinition_Emulator(string displayName, string port, int pid)
            : base(displayName, port)
		{
			m_pid = pid;
		}

		public PortDefinition_Emulator(string displayName, int pid)
            : this(displayName, PipeNameFromPid(pid), pid)
		{
		}

		public int Pid
		{
			get
			{
				return m_pid;
			}
		}

		public override Stream CreateStream()
		{
			AsyncFileStream afs = null;

			afs = new AsyncFileStream(m_port, System.IO.FileShare.ReadWrite);

			return afs;
		}

		internal static string PipeNameFromPid(int pid)
		{
			return string.Format(@"\\.\pipe\TinyCLR_{0}_Port1", pid.ToString());
		}
	}

	public class EmulatorDiscovery : IDisposable
	{
		public delegate void EmulatorChangedEventHandler();

		ManagementEventWatcher m_eventWatcher_Start;
		ManagementEventWatcher m_eventWatcher_Stop;
		EmulatorChangedEventHandler m_subscribers;

		~EmulatorDiscovery()
		{
			Dispose();
		}

		public EmulatorDiscovery()
		{
			m_eventWatcher_Start = new ManagementEventWatcher(new WqlEventQuery("Win32_ProcessStartTrace"));
			m_eventWatcher_Start.EventArrived += new EventArrivedEventHandler(HandleProcessEvent);

			m_eventWatcher_Stop = new ManagementEventWatcher(new WqlEventQuery("Win32_ProcessStopTrace"));
			m_eventWatcher_Stop.EventArrived += new EventArrivedEventHandler(HandleProcessEvent);
		}

		[MethodImplAttribute(MethodImplOptions.Synchronized)]
		public void Dispose()
		{
			if(m_eventWatcher_Start != null)
			{
				m_eventWatcher_Start.Stop();
				m_eventWatcher_Start = null;
			}
			if(m_eventWatcher_Stop != null)
			{
				m_eventWatcher_Stop.Stop();
				m_eventWatcher_Stop = null;
			}
			m_subscribers = null;
			GC.SuppressFinalize(this);
		}
		//
		// Subscribing to this event allows applications to be notified when emulators are started or stopped.
		//
		public event EmulatorChangedEventHandler OnEmulatorChanged
		{
			[MethodImplAttribute(MethodImplOptions.Synchronized)]
            add
			{
				// subscribe to Wmi for Win32_DeviceChangeEvent
				if(m_subscribers == null)
				{
					m_eventWatcher_Start.Start();
					m_eventWatcher_Stop.Start();
				}

				m_subscribers += value;
			}

			[MethodImplAttribute(MethodImplOptions.Synchronized)]
            remove
			{
				m_subscribers -= value;

				if(m_subscribers == null)
				{
					m_eventWatcher_Start.Stop();
					m_eventWatcher_Stop.Stop();
				}
			}
		}

		void HandleProcessEventCore()
		{
			EmulatorChangedEventHandler subscribers = m_subscribers;

			if(subscribers != null)
			{
				subscribers();
			}
		}

		void HandleProcessEvent(object sender, EventArrivedEventArgs args)
		{
			HandleProcessEventCore();

			Thread.Sleep(1000); // Give the Emulator some time to open the pipes.

			HandleProcessEventCore();
		}
	}
}

