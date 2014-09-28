using System;
using System.Threading;
using System.IO;
using System.Runtime.CompilerServices;
using System.Diagnostics;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Microsoft.SPOT.Debugger;
using WireProtocol = Microsoft.SPOT.Debugger.WireProtocol;

namespace Microsoft.SPOT.Debugger
{
	public class CorDebugAppDomain
	{
		public const uint CAppDomainIdForNoAppDomainSupport = 1;
		private readonly CorDebugProcess m_process;
		private readonly ArrayList m_assemblies;
		private readonly uint m_id;
		private string m_name;
		private readonly Guid m_guidProgramId;

		public CorDebugAppDomain (CorDebugProcess process, uint id)
		{
			m_process = process;
			m_id = id;
			m_assemblies = new ArrayList ();
			m_guidProgramId = Guid.NewGuid ();
		}

		public string Name {
			get { return m_name; }
		}

		public uint Id {
			get { return m_id; }
		}

		public CorDebugAssembly AssemblyFromIdx (uint idx)
		{
			return CorDebugAssembly.AssemblyFromIdx (idx, m_assemblies);
		}

		public CorDebugAssembly AssemblyFromIndex (uint index)
		{
			return CorDebugAssembly.AssemblyFromIndex (index, m_assemblies);            
		}

		public bool UpdateAssemblies ()
		{
			uint[] assemblies = null;            
			List<ManagedCallbacks.ManagedCallback> callbacks = new System.Collections.Generic.List<ManagedCallbacks.ManagedCallback> ();

			if (this.Process.Engine.Capabilities.AppDomains) {
				WireProtocol.Commands.Debugging_Resolve_AppDomain.Reply reply = this.Process.Engine.ResolveAppDomain (m_id);

				if (reply != null) {
					m_name = reply.Name;
					assemblies = reply.m_data;
				}

				if (assemblies == null) {
					//assembly is already unloaded
					assemblies = new uint[0];
				}
			} else {
				WireProtocol.Commands.Debugging_Resolve_Assembly[] reply = this.Process.Engine.ResolveAllAssemblies ();
				assemblies = new uint[reply.Length];

				for (int iAssembly = 0; iAssembly < assemblies.Length; iAssembly++) {
					assemblies [iAssembly] = reply [iAssembly].m_idx;
				}
			}

			//Convert Assembly Index to Idx.
			for (uint iAssembly = 0; iAssembly < assemblies.Length; iAssembly++) {
				assemblies [iAssembly] = TinyCLR_TypeSystem.IdxAssemblyFromIndex (assemblies [iAssembly]);
			}            

			//Unload dead assemblies
			for (int iAssembly = m_assemblies.Count - 1; iAssembly >= 0; iAssembly--) {
				CorDebugAssembly assembly = (CorDebugAssembly)m_assemblies [iAssembly];

				if (Array.IndexOf (assemblies, assembly.Idx) < 0) {                 
					callbacks.Add (new ManagedCallbacks.ManagedCallbackAssembly (assembly, ManagedCallbacks.ManagedCallbackAssembly.EventType.UnloadModule));
					callbacks.Add (new ManagedCallbacks.ManagedCallbackAssembly (assembly, ManagedCallbacks.ManagedCallbackAssembly.EventType.UnloadAssembly));

					m_assemblies.RemoveAt (iAssembly);                    
				}
			}

			//Load new assemblies                                    
			for (int i = 0; i < assemblies.Length; i++) {
				uint idx = assemblies [i];

				CorDebugAssembly assembly = AssemblyFromIdx (idx);

				if (assembly == null) {
					//Get the primary assembly from CorDebugProcess
					assembly = this.Process.AssemblyFromIdx (idx);

					Debug.Assert (assembly != null);

					//create a new CorDebugAssemblyInstance
					assembly = assembly.CreateAssemblyInstance (this);

					Debug.Assert (assembly != null);

					m_assemblies.Add (assembly);

					//cpde expects mscorlib to be the first assembly it hears about
					int index = (assembly.Name == "mscorlib") ? 0 : callbacks.Count;

					callbacks.Insert (index, new ManagedCallbacks.ManagedCallbackAssembly (assembly, ManagedCallbacks.ManagedCallbackAssembly.EventType.LoadAssembly));
					callbacks.Insert (index + 1, new ManagedCallbacks.ManagedCallbackAssembly (assembly, ManagedCallbacks.ManagedCallbackAssembly.EventType.LoadModule));
				}
			}

			this.Process.EnqueueEvents (callbacks);

			return callbacks.Count > 0;            
		}

		public CorDebugProcess Process {
			[DebuggerHidden]
            get { return m_process; }
		}

		public Engine Engine {
			[DebuggerHidden]
            get { return m_process.Engine; }
		}
	}
}
