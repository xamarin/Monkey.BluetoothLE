using System;
using Mono.Debugging.Evaluation;
using Microsoft.SPOT.Debugger;
using System.Collections.Generic;
using Mono.Debugging.Client;
using MonoDevelop.MicroFramework;
using System.Reflection;
using Mono.Cecil.Cil;

namespace Microsoft.SPOT.Debugger
{
	public class CorDebugBacktrace: BaseBacktrace
	{
		CorDebugThread thread;
		readonly uint threadId;
		readonly MicroFrameworkDebuggerSession session;
		List<CorDebugFrame> frames;
		int evalTimestamp;

		public CorDebugBacktrace (CorDebugThread thread, MicroFrameworkDebuggerSession session) : base (session.ObjectAdapter)
		{
			this.session = session;
			this.thread = thread;
			threadId = thread.Id;
			frames = new List<CorDebugFrame> (GetFrames (thread));
			evalTimestamp = MicroFrameworkDebuggerSession.EvaluationTimestamp;
		}

		internal static IEnumerable<CorDebugFrame> GetFrames (CorDebugThread thread)
		{
			foreach (CorDebugChain chain in thread.Chains) {
				if (!chain.IsManaged)
					continue;
				foreach (CorDebugFrame frame in chain.Frames)
					yield return frame;
			}
		}

		internal List<CorDebugFrame> FrameList {
			get {
				if (evalTimestamp != MicroFrameworkDebuggerSession.EvaluationTimestamp) {
//					thread = session.GetThread(threadId);
					frames = new List<CorDebugFrame> (GetFrames (thread));
					evalTimestamp = MicroFrameworkDebuggerSession.EvaluationTimestamp;
				}
				return frames;
			}
		}

		protected override EvaluationContext GetEvaluationContext (int frameIndex, EvaluationOptions options)
		{
			return new CorEvaluationContext (session, this, frameIndex, options) {
				Thread = thread
			};
		}

		#region IBacktrace Members

		public override AssemblyLine[] Disassemble (int frameIndex, int firstLine, int count)
		{
			return new AssemblyLine[0];
		}

		public override int FrameCount {
			get { return FrameList.Count; }
		}

		public override StackFrame[] GetStackFrames (int firstIndex, int lastIndex)
		{
			if (lastIndex >= FrameList.Count)
				lastIndex = FrameList.Count - 1;
			StackFrame[] array = new StackFrame[lastIndex - firstIndex + 1];
			for (int n = 0; n < array.Length; n++)
				array [n] = CreateFrame (session, FrameList [n + firstIndex]);
			return array;
		}

		internal static StackFrame CreateFrame (MicroFrameworkDebuggerSession session, CorDebugFrame frame)
		{
			string file = "";
			int line = 0;
			string method = "";
			string lang = "";

			if (frame.FrameType == CorFrameType.ILFrame) {
				if (frame.Function != null) {
					uint tk = TinyCLR_TypeSystem.SymbollessSupport.TinyCLRTokenFromMethodDefToken (frame.Function.Token);
					uint md = TinyCLR_TypeSystem.ClassMemberIndexFromTinyCLRToken (tk, frame.Function.Assembly);
					method = session.Engine.GetMethodName (md, true);
					var reader = frame.Function.Assembly.DebugData;
					if (reader != null) {
						var sim = new MethodSymbols (new Mono.Cecil.MetadataToken (frame.Function.Token));
						//Ugliest hack ever
						if(reader is Mono.Cecil.Mdb.MdbReader) {
							for(int i = 0; i < 100; i++)
								sim.Variables.Add(new VariableDefinition(null));
						}
						reader.Read (sim);
						InstructionSymbol prevSp = new InstructionSymbol (-1, null);
						foreach (var sp in sim.Instructions) {
							if (sp.Offset > frame.IP)
								break;
							prevSp = sp;
						}
						if (prevSp.Offset != -1) {
							line = prevSp.SequencePoint.StartLine;
							file = prevSp.SequencePoint.Document.Url;
						}
					}
				}
				lang = "Managed";
			}
//			else if(frame.FrameType == CorFrameType.NativeFrame)
//			{
//				frame.GetNativeIP(out address);
//				method = "<Unknown>";
//				lang = "Native";
//			}
			else if (frame.FrameType == CorFrameType.InternalFrame) {
				switch (((CorDebugInternalFrame)frame).FrameInternalType) {
				case CorDebugInternalFrameType.STUBFRAME_M2U:
					method = "[Managed to Native Transition]";
					break;
				case CorDebugInternalFrameType.STUBFRAME_U2M:
					method = "[Native to Managed Transition]";
					break;
				case CorDebugInternalFrameType.STUBFRAME_LIGHTWEIGHT_FUNCTION:
					method = "[Lightweight Method Call]";
					break;
				case CorDebugInternalFrameType.STUBFRAME_APPDOMAIN_TRANSITION:
					method = "[Application Domain Transition]";
					break;
				case CorDebugInternalFrameType.STUBFRAME_FUNC_EVAL:
					method = "[Function Evaluation]";
					break;
				}
			}
			if (method == null)
				method = "<Unknown>";
			var loc = new SourceLocation (method, file, line);
			return new StackFrame ((long)0, loc, lang);
		}

		#endregion
	}
}

