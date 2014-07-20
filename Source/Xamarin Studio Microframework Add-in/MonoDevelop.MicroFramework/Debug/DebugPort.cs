using System;
using System.IO;
using System.Reflection;
using System.Diagnostics;
using System.Collections;
using System.Runtime.InteropServices;
using Microsoft.Win32;
using System.Net;
using System.Net.Sockets;

namespace Microsoft.SPOT.Debugger
{
	public class DebugPort
	{
		private Guid m_guid;
		private DebugPortSupplier m_portSupplier;
		private ArrayList m_alProcesses;
		private string m_name;
		private PortFilter m_portFilter;
		private ConnectionPoint m_cpDebugPortEvents2;
		//This can't be shared with other debugPorts, for remote attaching to multiple processes....???
		protected uint m_pidNext;

		public DebugPort(PortFilter portFilter, DebugPortSupplier portSupplier)
		{
			m_name = NameFromPortFilter(portFilter);
			m_portSupplier = portSupplier;
			m_portFilter = portFilter;
			m_cpDebugPortEvents2 = new ConnectionPoint();
			m_alProcesses = ArrayList.Synchronized(new ArrayList(1));
			m_pidNext = 1;
			m_guid = Guid.NewGuid();
		}

		internal CorDebugProcess TryAddProcess(string name)
		{            
			CorDebugProcess process = null;
			PortDefinition portDefinition = null;

			//this is kind of bogus.  How should the attach dialog be organized????

			switch(m_portFilter)
			{
				case PortFilter.TcpIp:
					for(int retry = 0; retry < 5; retry++)
					{
						try
						{
							portDefinition = PortDefinition.CreateInstanceForTcp(name);
							break;
						}
						catch(System.Net.Sockets.SocketException)
						{
							System.Threading.Thread.Sleep(1000);
						}
					}
					break;
			}

			if(portDefinition != null)
			{
				process = this.EnsureProcess(portDefinition);
			}

			return process;
		}

		public bool ContainsProcess(CorDebugProcess process)
		{
			return m_alProcesses.Contains(process);
		}

		public void RefreshProcesses()
		{
			PortDefinition[] ports = null;

			switch(m_portFilter)
			{
				case PortFilter.Emulator:
#if USE_CONNECTION_MANAGER
                    ports = this.DebugPortSupplier.Manager.EnumeratePorts(m_portFilter);
#else
					ports = Emulator.EnumeratePipes();
#endif
					break;
				case PortFilter.Serial:
				case PortFilter.Usb:
				case PortFilter.TcpIp:
					ports = GetPersistablePortDefinitions();
					break;
				default:
					Debug.Assert(false);
					throw new ApplicationException();
			}

			ArrayList processes = new ArrayList(m_alProcesses.Count + ports.Length);

			for(int i = ports.Length - 1; i >= 0; i--)
			{
				PortDefinition portDefinition = (PortDefinition)ports[i];
				CorDebugProcess process = EnsureProcess(portDefinition);

				processes.Add(process);
			}

			for(int i = m_alProcesses.Count - 1; i >= 0; i--)
			{
				CorDebugProcess process = (CorDebugProcess)m_alProcesses[i];
                
				if(!processes.Contains(process))
				{
					RemoveProcess(process);
				}
			}
		}

		public DebugPortSupplier DebugPortSupplier
		{
			[System.Diagnostics.DebuggerHidden]
            get { return m_portSupplier; }
		}

		public void AddProcess(CorDebugProcess process)
		{
			if(!m_alProcesses.Contains(process))
			{
				m_alProcesses.Add(process);
			}
		}

		public CorDebugProcess EnsureProcess(PortDefinition portDefinition)
		{
			CorDebugProcess process = ProcessFromPortDefinition(portDefinition);            

			if(process == null)
			{
				process = new CorDebugProcess(this, portDefinition);

				uint pid;
				if(portDefinition is PortDefinition_Emulator)
				{
					Debug.Assert(this.IsLocalPort);
					pid = (uint)((PortDefinition_Emulator)portDefinition).Pid;
				}
				else
				{
					pid = m_pidNext++;
				}

				process.SetPid(pid);

				AddProcess(process);
			}

			return process;
		}

		private void RemoveProcess(CorDebugProcess process)
		{
			process.Terminate(0);
			m_alProcesses.Remove(process);
		}

		public void RemoveProcess(PortDefinition portDefinition)
		{
			CorDebugProcess process = ProcessFromPortDefinition(portDefinition);

			if(process != null)
			{
				RemoveProcess(process);
			}
		}

		public PortDefinition[] GetPersistablePortDefinitions()
		{
			PortDefinition[] ports = null;

			switch(m_portFilter)
			{
				case PortFilter.Emulator:
					Debug.Assert(false);
					break;
				case PortFilter.Serial:
					ports = AsyncSerialStream.EnumeratePorts();
					break;
				case PortFilter.Usb:
					{
					if (MonoDevelop.Core.Platform.IsWindows) {
						PortDefinition[] portUSB;
						PortDefinition[] portWinUSB;
                        
						portUSB = AsyncUsbStream.EnumeratePorts ();
						portWinUSB = WinUsb_AsyncUsbStream.EnumeratePorts ();

						int lenUSB = portUSB != null ? portUSB.Length : 0;
						int lenWinUSB = portWinUSB != null ? portWinUSB.Length : 0;

						ports = new PortDefinition[lenUSB + lenWinUSB];

						if (lenUSB > 0) {
							Array.Copy (portUSB, ports, lenUSB);
						}
						if (lenWinUSB > 0) {
							Array.Copy (portWinUSB, 0, ports, lenUSB, lenWinUSB);
						}
					} else {
						ports = LibUsb_AsyncUsbStream.EnumeratePorts ();
					}
					}
					break;
				case PortFilter.TcpIp:
					ports = PortDefinition_Tcp.EnumeratePorts(false);
					break;         
				default:
					Debug.Assert(false);
					throw new ApplicationException();
			}

			return ports;
		}

		public bool AreProcessIdEqual(uint pid1, uint pid2)
		{
			return pid1 == pid2;
		}

		public Guid PortId
		{
			get { return m_guid; }
		}

		public PortFilter PortFilter
		{
			get { return m_portFilter; }
		}

		public bool IsLocalPort
		{
			get { return m_portFilter == PortFilter.Emulator; }
		}

		public string Name
		{
			[System.Diagnostics.DebuggerHidden]
            get { return m_name; }
		}

		public CorDebugProcess GetDeviceProcess(string deviceName, int eachSecondRetryMaxCount)
		{
			if(string.IsNullOrEmpty(deviceName))
				throw new Exception("DebugPort.GetDeviceProcess() called with no argument");

			VsPackage.MessageCentre.StartProgressMsg(String.Format(DiagnosticStrings.StartDeviceSearch, deviceName, eachSecondRetryMaxCount));

			CorDebugProcess process = this.InternalGetDeviceProcess(deviceName);
			if(process != null)
				return process;

			if(eachSecondRetryMaxCount < 0)
				eachSecondRetryMaxCount = 0;

			for(int i = 0; i < eachSecondRetryMaxCount && process == null; i++)
			{
				VsPackage.MessageCentre.DeployDot();
				System.Threading.Thread.Sleep(1000);
				process = this.InternalGetDeviceProcess(deviceName);
			}                
            
			VsPackage.MessageCentre.StopProgressMsg(String.Format((process == null) 
                                                                    ? DiagnosticStrings.DeviceFound
                                                                    : DiagnosticStrings.DeviceNotFound,
				deviceName));
			return process;            
		}

		public CorDebugProcess GetDeviceProcess(string deviceName)
		{
			if(string.IsNullOrEmpty(deviceName))
				throw new Exception("DebugPort.GetDeviceProcess() called with no argument");
                            
			return this.InternalGetDeviceProcess(deviceName);
		}

		private CorDebugProcess InternalGetDeviceProcess(string deviceName)
		{
			CorDebugProcess process = null;

			RefreshProcesses();
            
			for(int i = 0; i < m_alProcesses.Count; i++)
			{
				CorDebugProcess processT = (CorDebugProcess)m_alProcesses[i];
				PortDefinition pd = processT.PortDefinition;
				if(String.Compare(GetDeviceName(pd), deviceName, true) == 0)
				{
					process = processT;
					break;
				}
			}

			if(m_portFilter == PortFilter.TcpIp && process == null)
			{
				process = EnsureProcess(PortDefinition.CreateInstanceForTcp(deviceName));                
			}
                            
			return process;
		}

		public static string NameFromPortFilter(PortFilter portFilter)
		{
			switch(portFilter)
			{
				case PortFilter.Serial:
					return "Serial";
				case PortFilter.Emulator:
					return "Emulator";
				case PortFilter.Usb:
					return "USB";
				case PortFilter.TcpIp:
					return "TCP/IP";
				default:
					Debug.Assert(false);
					return "";
			}
		}

		public string GetDeviceName(PortDefinition pd)
		{
			return pd.PersistName;
		}

		private bool ArePortEntriesEqual(PortDefinition pd1, PortDefinition pd2)
		{
			if(pd1.Port != pd2.Port)
				return false;
            
			if(pd1.GetType() != pd2.GetType())
				return false;
            
			if(pd1 is PortDefinition_Emulator && pd2 is PortDefinition_Emulator)
			{
				if(((PortDefinition_Emulator)pd1).Pid != ((PortDefinition_Emulator)pd2).Pid)
					return false;
			}
            
			return true;
		}

		private CorDebugProcess ProcessFromPortDefinition(PortDefinition portDefinition)
		{
			foreach(CorDebugProcess process in m_alProcesses)
			{
				if(ArePortEntriesEqual(portDefinition, process.PortDefinition))
					return process;
			}

			return null;
		}

		private CorDebugProcess GetProcess(uint ProcessId)
		{
			foreach(CorDebugProcess process in m_alProcesses)
			{
				if(AreProcessIdEqual(ProcessId, process.Id))
				{
					return process;
				}
			}

			return null;
		}
	}
}
