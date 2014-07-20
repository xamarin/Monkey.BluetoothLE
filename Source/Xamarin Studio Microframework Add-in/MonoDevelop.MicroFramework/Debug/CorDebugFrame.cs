using System;
using System.Collections;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.SPOT.Debugger;
using WireProtocol = Microsoft.SPOT.Debugger.WireProtocol;

namespace Microsoft.SPOT.Debugger
{
	public enum CorFrameType
	{
		ILFrame,
		NativeFrame,
		InternalFrame,
	}

	public class CorDebugFrame
	{
		const uint STACK_BEGIN = uint.MaxValue;
		const uint IP_NOT_INITIALIZED = uint.MaxValue;
		public const uint DEPTH_CLR_INVALID = uint.MaxValue;
		CorDebugChain m_chain;
		CorDebugFunction m_function;
		uint m_IP;
		uint m_depthTinyCLR;
		internal uint m_depthCLR;
		WireProtocol.Commands.Debugging_Thread_Stack.Reply.Call m_call;
		#if ALL_VALUES
        CorDebugValue[] m_valueLocals;
        CorDebugValue[] m_valueArguments;
        CorDebugValue[] m_valueEvalStack;
#endif
		public CorDebugFrame (CorDebugChain chain, WireProtocol.Commands.Debugging_Thread_Stack.Reply.Call call, uint depth)
		{
			m_chain = chain;
			m_depthTinyCLR = depth;
			m_call = call;
			m_IP = IP_NOT_INITIALIZED;
		}

		public CorDebugFrame Clone ()
		{
			return (CorDebugFrame)MemberwiseClone ();
		}

		public CorDebugProcess Process {
			[System.Diagnostics.DebuggerHidden]
            get { return this.Chain.Thread.Process; }
		}

		public CorDebugAppDomain AppDomain {
			get { return this.Function.AppDomain; }
		}

		public Engine Engine {
			[System.Diagnostics.DebuggerHidden]
            get { return this.Process.Engine; }
		}

		public CorDebugChain Chain {
			[System.Diagnostics.DebuggerHidden]
            get { return m_chain; }
		}

		public CorDebugThread Thread {
			[System.Diagnostics.DebuggerHidden]
            get { return m_chain.Thread; }
		}

		public uint DepthCLR {
			[System.Diagnostics.DebuggerHidden]
            get { return this.m_depthCLR; }
		}

		public uint DepthTinyCLR {
			[System.Diagnostics.DebuggerHidden]
            get { return this.m_depthTinyCLR; }
		}

		public static uint AppDomainIdFromCall (Engine engine, WireProtocol.Commands.Debugging_Thread_Stack.Reply.Call call)
		{
			uint appDomainId = CorDebugAppDomain.CAppDomainIdForNoAppDomainSupport;

			if (engine.Capabilities.AppDomains) {
				WireProtocol.Commands.Debugging_Thread_Stack.Reply.CallEx callEx = call as WireProtocol.Commands.Debugging_Thread_Stack.Reply.CallEx;

				appDomainId = callEx.m_appDomainID;
			}

			return appDomainId;
		}

		public virtual CorDebugFunction Function {
			get {
				if (m_function == null) {
					uint appDomainId = AppDomainIdFromCall (this.Engine, m_call);

					CorDebugAppDomain appDomain = Process.GetAppDomainFromId (appDomainId);
					CorDebugAssembly assembly = appDomain.AssemblyFromIndex (m_call.m_md);
					;
                    
					uint tkMethod = TinyCLR_TypeSystem.TinyCLRTokenFromMethodIndex (m_call.m_md);
                                 
					m_function = assembly.GetFunctionFromTokenTinyCLR (tkMethod);
				}

				return m_function;
			}
		}

		public uint IP {
			get {
				if (m_IP == IP_NOT_INITIALIZED) {
					m_IP = Function.HasSymbols ? Function.GetILCLRFromILTinyCLR (m_call.m_IP) : m_call.m_IP;
				}

				return m_IP;
			}
		}

		public uint IP_TinyCLR {
			[System.Diagnostics.DebuggerHidden]
            get { return m_call.m_IP; }
		}

		private CorDebugValue GetStackFrameValue (uint dwIndex, Engine.StackValueKind kind)
		{
			return CorDebugValue.CreateValue (this.Engine.GetStackFrameValue (m_chain.Thread.Id, m_depthTinyCLR, kind, dwIndex), this.AppDomain);
		}

		public uint Flags {
			get {
				WireProtocol.Commands.Debugging_Thread_Stack.Reply.CallEx callEx = m_call as WireProtocol.Commands.Debugging_Thread_Stack.Reply.CallEx;
				return (callEx == null) ? 0 : callEx.m_flags;
			}
		}
		#if ALL_VALUES
        private CorDebugValue[] EnsureValues(ref CorDebugValue[] values, uint cInfo, Engine.StackValueKind kind)
        {
            if (values == null)
            {
                values = CorDebugValue.CreateValues(this.Engine.GetStackFrameValueAll(m_chain.Thread.ID, this.DepthTinyCLR, cInfo, kind), this.Process);
            }

            return values;
        }

        
        private CorDebugValue[] Locals
        {
            get
            {
                return EnsureValues(ref m_valueLocals, m_function.PdbxMethod.NumLocal, Debugger.Engine.StackValueKind.Local);
            }
        }

        private CorDebugValue[] Arguments
        {
            get
            {
                return EnsureValues(ref m_valueArguments, m_function.PdbxMethod.NumArg, Debugger.Engine.StackValueKind.Argument);
            }
        }

        private CorDebugValue[] Evals
        {
            get
            {
                return EnsureValues(ref m_valueEvalStack, m_function.PdbxMethod.MaxStack, Debugger.Engine.StackValueKind.EvalStack);
            }
        }
#endif
		public static void GetStackRange (CorDebugThread thread, uint depthCLR, out ulong start, out ulong end)
		{
			for (CorDebugThread threadT = thread.GetRealCorDebugThread (); threadT != thread; threadT = threadT.NextThread) {
				Debug.Assert (threadT.IsSuspended);
				depthCLR += threadT.Chain.NumFrames;
			}

			start = depthCLR;
			end = start;
		}

		public CorFrameType FrameType {
			get {
				if (this is CorDebugInternalFrame)
					return CorFrameType.InternalFrame;
				else
					return CorFrameType.ILFrame;
			}
		}

		public CorDebugValue GetArgument (int i)
		{
			return GetStackFrameValue ((uint)(i + 1), Engine.StackValueKind.Argument);
		}

		public int GetArgumentCount ()
		{
			return 0;//Microframework not supporting :(
		}

		public CorDebugValue GetLocalVariable (uint index)
		{
			return GetStackFrameValue (index, Engine.StackValueKind.Local);
		}
	}

	public class CorDebugInternalFrame : CorDebugFrame
	{
		CorDebugInternalFrameType m_type;

		public CorDebugInternalFrame (CorDebugChain chain, CorDebugInternalFrameType type)
            : base (chain, null, CorDebugFrame.DEPTH_CLR_INVALID)
		{
			m_type = type;
		}

		public CorDebugInternalFrameType FrameInternalType {
			get { return m_type; }
		}

		public override CorDebugFunction Function {
			get { return null; }
		}
	}
}
