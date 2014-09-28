using System;
using System.Collections;
using System.Runtime.InteropServices;
using System.Diagnostics;
using Microsoft.SPOT.Debugger;
using WireProtocol = Microsoft.SPOT.Debugger.WireProtocol;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.SPOT.Debugger
{
	[Serializable]
	public enum CorDebugInternalFrameType
	{
		STUBFRAME_NONE = 0x00000000,
		STUBFRAME_M2U = 0x0000001,
		STUBFRAME_U2M = 0x0000002,
		STUBFRAME_APPDOMAIN_TRANSITION = 0x00000003,
		STUBFRAME_LIGHTWEIGHT_FUNCTION = 0x00000004,
		STUBFRAME_FUNC_EVAL = 0x00000005,
	}

	public class CorDebugChain
	{
		CorDebugThread m_thread;
		CorDebugFrame[] m_frames;

		public CorDebugChain(CorDebugThread thread, WireProtocol.Commands.Debugging_Thread_Stack.Reply.Call[] calls)
		{
			m_thread = thread;

			ArrayList frames = new ArrayList(calls.Length);
			bool lastFrameWasUnmanaged = false;

			if(thread.IsVirtualThread)
			{
				frames.Add(new CorDebugInternalFrame(this, CorDebugInternalFrameType.STUBFRAME_FUNC_EVAL));
			}

			for(uint i = 0; i < calls.Length; i++)
			{
				WireProtocol.Commands.Debugging_Thread_Stack.Reply.Call call = calls[i];
				WireProtocol.Commands.Debugging_Thread_Stack.Reply.CallEx callEx = call as WireProtocol.Commands.Debugging_Thread_Stack.Reply.CallEx;

				if(callEx != null)
				{
					if((callEx.m_flags & WireProtocol.Commands.Debugging_Thread_Stack.Reply.c_AppDomainTransition) != 0)
					{
						//No internal frame is used in the TinyCLR.  This is simply to display the AppDomain transition 
						//in the callstack of Visual Studio.
						frames.Add(new CorDebugInternalFrame(this, CorDebugInternalFrameType.STUBFRAME_APPDOMAIN_TRANSITION));
					}

					if((callEx.m_flags & WireProtocol.Commands.Debugging_Thread_Stack.Reply.c_PseudoStackFrameForFilter) != 0)
					{
						//No internal frame is used in the TinyCLR for filters.  This is simply to display the transition 
						//in the callstack of Visual Studio.
						frames.Add(new CorDebugInternalFrame(this, CorDebugInternalFrameType.STUBFRAME_M2U));
						frames.Add(new CorDebugInternalFrame(this, CorDebugInternalFrameType.STUBFRAME_U2M));
					}

					if((callEx.m_flags & WireProtocol.Commands.Debugging_Thread_Stack.Reply.c_MethodKind_Interpreted) != 0)
					{
						if(lastFrameWasUnmanaged)
						{
							frames.Add(new CorDebugInternalFrame(this, CorDebugInternalFrameType.STUBFRAME_U2M));
						}

						lastFrameWasUnmanaged = false;
					}
					else
					{
						if(!lastFrameWasUnmanaged)
						{
							frames.Add(new CorDebugInternalFrame(this, CorDebugInternalFrameType.STUBFRAME_M2U));
						}

						lastFrameWasUnmanaged = true;
					}
				}


				frames.Add(new CorDebugFrame(this, call, i));
			}

			m_frames = (CorDebugFrame[])frames.ToArray(typeof(CorDebugFrame));

			uint depthCLR = 0;
			for(int iFrame = m_frames.Length - 1; iFrame >= 0; iFrame--)
			{
				m_frames[iFrame].m_depthCLR = depthCLR;
				depthCLR++;
			}
		}

		public CorDebugThread Thread
		{
			[System.Diagnostics.DebuggerHidden]
            get { return m_thread; }
		}

		public Engine Engine
		{
			get { return m_thread.Engine; }
		}

		public uint NumFrames
		{            
			[System.Diagnostics.DebuggerHidden]
            get { return (uint)m_frames.Length; }
		}

		public void RefreshFrames()
		{
			// The frames need to be different after resuming execution
			if(m_frames != null)
			{
				for(int iFrame = 0; iFrame < m_frames.Length; iFrame++)
				{
					m_frames[iFrame] = m_frames[iFrame].Clone();
				}
			}
		}

		public CorDebugFrame GetFrameFromDepthCLR(uint depthCLR)
		{
			int index = m_frames.Length - 1 - (int)depthCLR;

			if(Utility.InRange(index, 0, m_frames.Length - 1))
				return m_frames[index];
			return null;
		}

		public CorDebugFrame GetFrameFromDepthTinyCLR(uint depthTinyCLR)
		{
			for(uint iFrame = depthTinyCLR; iFrame < m_frames.Length; iFrame++)
			{
				CorDebugFrame frame = m_frames[iFrame];

				if(frame.DepthTinyCLR == depthTinyCLR)
				{
					return frame;
				}
			}

			return null;
		}

		public CorDebugFrame ActiveFrame
		{
			get { return GetFrameFromDepthCLR(0); }
		}

		public bool IsManaged
		{
			get
			{
				return true; 
			}
		}

		public List<CorDebugFrame> Frames
		{
			get
			{
				var frames = m_frames.ToList();
				//Reverse the order for the enumerator
				frames.Reverse();
				return frames;
			}
		}
	}
}
