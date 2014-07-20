using System;
using System.Runtime.InteropServices;
using Microsoft.SPOT.Debugger.WireProtocol;
using BreakpointDef = Microsoft.SPOT.Debugger.WireProtocol.Commands.Debugging_Execution_BreakpointDef;

namespace Microsoft.SPOT.Debugger
{
	public abstract class CorDebugBreakpoint : CorDebugBreakpointBase
	{
		public CorDebugBreakpoint(CorDebugAppDomain appDomain) : base(appDomain)
		{       
			this.Kind = BreakpointDef.c_HARD;
		}

		protected abstract Type TypeToMarshal
		{
			get;
		}

		public override void Hit(BreakpointDef breakpointDef)
		{
			CorDebugThread thread = this.Process.GetThread(breakpointDef.m_pid);
                        
			this.Process.EnqueueEvent(new ManagedCallbacks.ManagedCallbackBreakpoint(thread, this, this.TypeToMarshal));         
		}
	}

	public class CorDebugFunctionBreakpoint : CorDebugBreakpoint
	{
		private CorDebugFunction m_function;
		private Pdbx.IL m_il;

		public CorDebugFunctionBreakpoint(CorDebugFunction function, uint ilCLR) : base(function.AppDomain)
		{
			m_function = function;
			m_il = new Pdbx.IL();
			m_il.CLR = ilCLR;
			m_il.TinyCLR = function.GetILTinyCLRFromILCLR(ilCLR);

			m_breakpointDef.m_IP = m_il.TinyCLR;            
			m_breakpointDef.m_md = m_function.MethodDef_Index;            

			this.Active = true;   
		}

		protected override Type TypeToMarshal
		{
			[System.Diagnostics.DebuggerHidden]
            get { return typeof(CorDebugFunctionBreakpoint); }
		}
		/*
            Function breakpoints are a bit special.  In order not to burden the TinyCLR with duplicate function
            breakpoints for each AppDomain. 
        */
		public override bool Equals(CorDebugBreakpointBase breakpoint)
		{
			CorDebugFunctionBreakpoint bp = breakpoint as CorDebugFunctionBreakpoint;

			if(bp == null)
				return false;

			if(this.m_breakpointDef.m_IP != bp.m_breakpointDef.m_IP)
				return false;
			if(this.m_breakpointDef.m_md != bp.m_breakpointDef.m_md)
				return false;

			return true;
		}

		public override bool IsMatch(Commands.Debugging_Execution_BreakpointDef breakpointDef)
		{
			if(breakpointDef.m_flags != BreakpointDef.c_HARD)
				return false;
			if(breakpointDef.m_IP != m_breakpointDef.m_IP)
				return false;
			if(breakpointDef.m_md != m_breakpointDef.m_md)
				return false;

			CorDebugThread thread = m_function.Process.GetThread(breakpointDef.m_pid);
			CorDebugFrame frame = thread.Chain.ActiveFrame;
			if(frame.AppDomain != m_function.AppDomain)
				return false;

			return true;
		}
	}
}