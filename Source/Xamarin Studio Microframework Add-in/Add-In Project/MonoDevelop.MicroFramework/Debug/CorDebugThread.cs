using System;
using System.Collections;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.SPOT.Debugger;
using WireProtocol = Microsoft.SPOT.Debugger.WireProtocol;
using System.Collections.Generic;

namespace Microsoft.SPOT.Debugger
{
	public class CorDebugThread
	{
		CorDebugProcess m_process;
		CorDebugChain m_chain;
		uint m_id;
		bool m_fSuspended;
		bool m_fSuspendedSav;
		//for function eval, need to remember if this thread is suspended before suspending for function eval..
		CorDebugValue m_currentException;
		CorDebugEval m_eval;
		bool m_fSuspendThreadEvents;
		CorDebugAppDomain m_initialAppDomain;
		bool m_fExited;
		//Doubly-linked list of virtual threads.  The head of the list is the real thread
		//All other threads (at this point), should be the cause of a function eval
		CorDebugThread m_threadPrevious;
		CorDebugThread m_threadNext;

		public CorDebugThread (CorDebugProcess process, uint id, CorDebugEval eval)
		{
			m_process = process;
			m_id = id;
			m_fSuspended = false;
			m_eval = eval;
		}

		public CorDebugEval CurrentEval {
			get { return m_eval; }         
		}

		public bool Exited {
			get { return m_fExited; }
			set {
				if (value)
					m_fExited = value;
			}
		}

		public bool SuspendThreadEvents {
			get { return m_fSuspendThreadEvents; }
			set { m_fSuspendThreadEvents = value; }
		}

		public void AttachVirtualThread (CorDebugThread thread)
		{
			CorDebugThread threadLast = this.GetLastCorDebugThread ();

			threadLast.m_threadNext = thread;
			thread.m_threadPrevious = threadLast;

			m_process.AddThread (thread);
			Debug.Assert (Process.IsExecutionPaused);

			threadLast.m_fSuspendedSav = threadLast.m_fSuspended;
			threadLast.IsSuspended = true;
		}

		public bool RemoveVirtualThread (CorDebugThread thread)
		{
			//can only remove last thread
			CorDebugThread threadLast = this.GetLastCorDebugThread ();

			Debug.Assert (threadLast.IsVirtualThread && !this.IsVirtualThread);
			if (threadLast != thread)
				return false;

			CorDebugThread threadNextToLast = threadLast.m_threadPrevious;

			threadNextToLast.m_threadNext = null;
			threadNextToLast.IsSuspended = threadNextToLast.m_fSuspendedSav;

			threadLast.m_threadPrevious = null;

			//Thread will be removed from process.m_alThreads when the ThreadTerminated breakpoint is hit
			return true;
		}

		public Engine Engine {
			[System.Diagnostics.DebuggerHidden]
            get { return m_process.Engine; }
		}

		public CorDebugProcess Process {
			[System.Diagnostics.DebuggerHidden]
            get { return m_process; }
		}

		public CorDebugAppDomain AppDomain {
			get {
				CorDebugAppDomain appDomain = m_initialAppDomain;

				if (!m_fExited) {

					CorDebugThread thread = GetLastCorDebugThread ();

					CorDebugFrame frame = thread.Chain.ActiveFrame;

					appDomain = frame.AppDomain;
				}

				return appDomain;
			}
		}

		public uint Id {
			[System.Diagnostics.DebuggerHidden]
            get { return m_id; }
		}

		public void StoppedOnException ()
		{
			m_currentException = CorDebugValue.CreateValue (Engine.GetThreadException (m_id), this.AppDomain);
		}
		//This is the only thread that cpde knows about
		public CorDebugThread GetRealCorDebugThread ()
		{
			CorDebugThread thread;

			for (thread = this; thread.m_threadPrevious != null; thread = thread.m_threadPrevious) {
			}

			return thread;
		}

		public CorDebugThread GetLastCorDebugThread ()
		{
			CorDebugThread thread;

			for (thread = this; thread.m_threadNext != null; thread = thread.m_threadNext) {
			}

			return thread;
		}

		public CorDebugThread PreviousThread {
			get { return m_threadPrevious; }
		}

		public CorDebugThread NextThread {
			get { return m_threadNext; }
		}

		public bool IsVirtualThread {
			get { return m_eval != null; }
		}

		public bool IsLogicalThreadSuspended {
			get {
				return GetLastCorDebugThread ().IsSuspended;
			}
		}

		public bool IsSuspended {
			get { return m_fSuspended; }
			set {
				bool fSuspend = value;

				if (fSuspend && !IsSuspended) {
					this.Engine.SuspendThread (Id);
				} else if (!fSuspend && IsSuspended) {
					this.Engine.ResumeThread (Id);
				}

				m_fSuspended = fSuspend;
			}
		}

		public CorDebugChain Chain {
			get {
				if (m_chain == null) {
					WireProtocol.Commands.Debugging_Thread_Stack.Reply ts = this.Engine.GetThreadStack (m_id);

					if (ts != null) {
						m_fSuspended = (ts.m_flags & WireProtocol.Commands.Debugging_Thread_Stack.Reply.TH_F_Suspended) != 0;
						m_chain = new CorDebugChain (this, ts.m_data);

						if (m_initialAppDomain == null) {
							CorDebugFrame initialFrame = m_chain.GetFrameFromDepthTinyCLR (0);
							m_initialAppDomain = initialFrame.AppDomain;
						}
					}
				}
				return m_chain;
			}
		}

		public void RefreshChain ()
		{
			if (m_chain != null) {
				m_chain.RefreshFrames ();
			}
		}

		public void ResumingExecution ()
		{
			if (IsSuspended) {
				RefreshChain ();
			} else {
				m_chain = null;
				m_currentException = null;
			}
		}

		public CorDebugFrame ActiveFrame {
			get {
				Debug.Assert (!IsVirtualThread);
				return GetLastCorDebugThread ().Chain.ActiveFrame;
			}
		}

		public List<CorDebugChain> Chains {
			get {
				Debug.Assert (!IsVirtualThread);

				var chains = new List<CorDebugChain> ();

				for (CorDebugThread thread = this.GetLastCorDebugThread (); thread != null; thread = thread.m_threadPrevious) {
					CorDebugChain chain = thread.Chain;

					if (chain != null) {
						chains.Add (chain);
					}
				}

				return chains;
			}
		}

		public CorDebugValue CurrentException {
			get {
				return GetLastCorDebugThread ().m_currentException;
			}
		}
	}
}
