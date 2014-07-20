using System;
using System.IO;
using System.Reflection;
using System.Diagnostics;
using System.Collections;
using System.Runtime.InteropServices;
using Microsoft.Win32;

namespace Microsoft.SPOT.Debugger
{
	public class DebugPortSupplier
	{
		private static DebugPortSupplierPrivate s_portSupplier;

		private class DebugPortSupplierPrivate : DebugPortSupplier
		{
			private DebugPort[] m_ports;
			#if USE_CONNECTION_MANAGER
            private ConnectionManager.Manager m_manager;
#endif
			public DebugPortSupplierPrivate () : base (true)
			{
#if USE_CONNECTION_MANAGER
                m_manager = new ConnectionManager.Manager();
#endif
				m_ports = new DebugPort[] {
					new DebugPort (PortFilter.Emulator, this),
					new DebugPort (PortFilter.Usb, this),
					new DebugPort (PortFilter.Serial, this),
					new DebugPort (PortFilter.TcpIp, this),
				};

			}
			#if USE_CONNECTION_MANAGER
            public override ConnectionManager.Manager Manager
            {
                [System.Diagnostics.DebuggerHidden]
                get { return m_manager; }
            }
#endif
			public override DebugPort FindPort (string name)
			{
				for (int i = 0; i < m_ports.Length; i++) {
					DebugPort port = m_ports [i];

					if (String.Compare (port.Name, name, true) == 0) {
						return port;
					}
				}

				return null;
			}

			public override DebugPort[] Ports {
				get { return (DebugPort[])m_ports.Clone (); }
			}

			private DebugPort DebugPortFromPortFilter (PortFilter portFilter)
			{
				foreach (DebugPort port in m_ports) {
					if (port.PortFilter == portFilter)
						return port;
				}

				return null;
			}
		}

		private DebugPortSupplier (bool fPrivate)
		{
		}

		public DebugPortSupplier ()
		{
			lock (typeof(DebugPortSupplier)) {
				if (s_portSupplier == null) {
					s_portSupplier = new DebugPortSupplierPrivate ();
				}
			}
		}

		public virtual DebugPort[] Ports {
			get { return s_portSupplier.Ports; }
		}

		public virtual DebugPort FindPort (string name)
		{
			return s_portSupplier.FindPort (name);
		}
		#if USE_CONNECTION_MANAGER
        public virtual ConnectionManager.Manager Manager
        {
            get { return s_portSupplier.Manager; }
        }
#endif
	}
}
