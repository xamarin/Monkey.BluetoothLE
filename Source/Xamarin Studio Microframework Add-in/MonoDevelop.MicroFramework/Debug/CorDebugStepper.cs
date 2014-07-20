using System;
using System.Reflection;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.SPOT.Debugger.WireProtocol;
using BreakpointDef = Microsoft.SPOT.Debugger.WireProtocol.Commands.Debugging_Execution_BreakpointDef;

namespace Microsoft.SPOT.Debugger
{
	[Serializable]
	public enum CorDebugStepReason
	{
		STEP_NORMAL = 0,
		STEP_RETURN = 1,
		STEP_CALL = 2,
		STEP_EXCEPTION_FILTER = 3,
		STEP_EXCEPTION_HANDLER = 4,
		STEP_INTERCEPT = 5,
		STEP_EXIT = 6,
	}

	public struct COR_DEBUG_STEP_RANGE
	{
		public uint startOffset;
		public uint endOffset;
	}

	[Serializable]
	public enum CorDebugIntercept
	{
		INTERCEPT_NONE = 0x0,
		INTERCEPT_CLASS_INIT = 0x1,
		INTERCEPT_EXCEPTION_FILTER = 0x2,
		INTERCEPT_SECURITY = 0x4,
		INTERCEPT_CONTEXT_POLICY = 0x8,
		INTERCEPT_INTERCEPTION = 0x10,
		INTERCEPT_ALL = 0xffff,
	}

	public class CorDebugStepper : CorDebugBreakpointBase
	{
		//if a stepper steps into/out of a frame, need to update m_frame
		CorDebugFrame m_frame;
		CorDebugThread m_thread;
		COR_DEBUG_STEP_RANGE[] m_ranges;
		CorDebugStepReason m_reasonStopped;
		CorDebugIntercept m_interceptMask;

		public CorDebugStepper (CorDebugFrame frame)
            : base (frame.AppDomain)
		{
			Initialize (frame);
		}

		private void Initialize (CorDebugFrame frame)
		{
			m_frame = frame;
			m_thread = frame.Thread;

			InitializeBreakpointDef ();
		}

		private new ushort Kind {
			[System.Diagnostics.DebuggerHidden]
			get { return base.Kind; }
			set {
				if ((value & BreakpointDef.c_STEP_IN) != 0)
					value |= BreakpointDef.c_STEP_OVER;

				value |= BreakpointDef.c_STEP_OUT | BreakpointDef.c_EXCEPTION_CAUGHT | BreakpointDef.c_THREAD_TERMINATED;

				base.Kind = value;
			}
		}

		private void InitializeBreakpointDef ()
		{
			m_breakpointDef.m_depth = m_frame.DepthTinyCLR;
			m_breakpointDef.m_pid = m_thread.Id;

			if (m_ranges != null && m_ranges.Length > 0) {
				m_breakpointDef.m_IPStart = m_ranges [0].startOffset;
				m_breakpointDef.m_IPEnd = m_ranges [0].endOffset;
			} else {
				m_breakpointDef.m_IPStart = 0;
				m_breakpointDef.m_IPEnd = 0;
			}

			Dirty ();
		}

		public void StepRange (bool into, COR_DEBUG_STEP_RANGE[] ranges)
		{
			m_ranges = ranges;
			for (int iRange = 0; iRange < m_ranges.Length; iRange++) {
				COR_DEBUG_STEP_RANGE range = m_ranges [iRange];
				m_ranges [iRange].startOffset = this.m_frame.Function.GetILTinyCLRFromILCLR (range.startOffset);
				m_ranges [iRange].endOffset = this.m_frame.Function.GetILTinyCLRFromILCLR (range.endOffset);
			}
			InitializeBreakpointDef ();
			this.Kind = into ? BreakpointDef.c_STEP_IN : BreakpointDef.c_STEP_OVER;
			this.Active = true;
		}

		public void Step (bool into)
		{
			InitializeBreakpointDef ();
			this.Kind = into ? BreakpointDef.c_STEP_IN : BreakpointDef.c_STEP_OVER;
			this.Active = true;
		}

		public override bool ShouldBreak (BreakpointDef breakpointDef)
		{
			bool fStop = true;
			CorDebugStepReason reason;

			//optimize, optimize, optimize No reason to get list of threads, and get thread stack for each step!!!            
			ushort flags = breakpointDef.m_flags;
			int depthOld = (int)m_frame.DepthTinyCLR;
			int depthNew = (int)breakpointDef.m_depth;
			int dDepth = depthNew - depthOld;

			if ((flags & BreakpointDef.c_STEP) != 0) {
				if ((flags & BreakpointDef.c_STEP_IN) != 0) {
					if (this.Process.Engine.Capabilities.ExceptionFilters && breakpointDef.m_depthExceptionHandler == BreakpointDef.c_DEPTH_STEP_INTERCEPT) {
						reason = CorDebugStepReason.STEP_INTERCEPT;
					} else {
						reason = CorDebugStepReason.STEP_CALL;
					}
				} else if ((flags & BreakpointDef.c_STEP_OVER) != 0) {
					reason = CorDebugStepReason.STEP_NORMAL;
				} else {
					if (this.Process.Engine.Capabilities.ExceptionFilters & breakpointDef.m_depthExceptionHandler == BreakpointDef.c_DEPTH_STEP_EXCEPTION_HANDLER) {
						reason = CorDebugStepReason.STEP_EXCEPTION_HANDLER;
					} else {
						reason = CorDebugStepReason.STEP_RETURN;
					}
				}
			} else if ((flags & BreakpointDef.c_EXCEPTION_CAUGHT) != 0) {
				reason = CorDebugStepReason.STEP_EXCEPTION_HANDLER;
				if (dDepth > 0)
					fStop = false;
				else if (dDepth == 0)
					fStop = (this.Debugging_Execution_BreakpointDef.m_flags & BreakpointDef.c_STEP_OVER) != 0;
				else
					fStop = true;
			} else if ((flags & BreakpointDef.c_THREAD_TERMINATED) != 0) {
				reason = CorDebugStepReason.STEP_EXIT;

				this.Active = false;
				fStop = false;
			} else {
				Debug.Assert (false);
				throw new ApplicationException ("Invalid stepper hit received");
			}

			if (m_ranges != null && reason == CorDebugStepReason.STEP_NORMAL && breakpointDef.m_depth == this.Debugging_Execution_BreakpointDef.m_depth) {
				foreach (COR_DEBUG_STEP_RANGE range in m_ranges) {
					if (Utility.InRange (breakpointDef.m_IP, range.startOffset, range.endOffset - 1)) {
						fStop = false;
						break;
					}
				}

				Debug.Assert (Utility.FImplies (m_ranges != null && m_ranges.Length == 1, fStop));
			}

			if (fStop && reason != CorDebugStepReason.STEP_EXIT) {
				uint depth = breakpointDef.m_depth;
				CorDebugFrame frame = this.m_thread.Chain.GetFrameFromDepthTinyCLR (depth);

				m_ranges = null;
				Initialize (frame);

				//Will callback with wrong reason if stepping through internal calls?????                
				//If we don't stop at an internal call, we need to reset/remember the range somehow?
				//This might be broken if a StepRange is called that causes us to enter an internal function                
				fStop = !m_frame.Function.IsInternal;
			}

			m_reasonStopped = reason;
			return fStop;
		}

		public override void Hit (BreakpointDef breakpointDef)
		{
			this.m_ranges = null;
			this.Active = false;
			this.Process.EnqueueEvent (new ManagedCallbacks.ManagedCallbackStepComplete (m_frame.Thread, this, m_reasonStopped));
		}

		public void SetJmcStatus (bool fJMC)
		{
			bool fJMCOld = (this.Debugging_Execution_BreakpointDef.m_flags & BreakpointDef.c_STEP_JMC) != 0;

			if (fJMC != fJMCOld) {
				if (fJMC)
					this.Debugging_Execution_BreakpointDef.m_flags |= BreakpointDef.c_STEP_JMC;
				else
					unchecked {
						this.Debugging_Execution_BreakpointDef.m_flags &= (ushort)(~BreakpointDef.c_STEP_JMC);
					}

				this.Dirty ();
			}
		}
	}
}
