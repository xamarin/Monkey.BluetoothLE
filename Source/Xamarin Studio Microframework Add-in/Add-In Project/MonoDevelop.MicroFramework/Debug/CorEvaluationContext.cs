using System;
using Mono.Debugging.Evaluation;
using DC = Mono.Debugging.Client;
using Microsoft.SPOT.Debugger;

namespace MonoDevelop.MicroFramework
{
	public class CorEvaluationContext: EvaluationContext
	{
		CorDebugEval corEval;
		CorDebugFrame frame;
		int frameIndex;
		int evalTimestamp;
		readonly CorDebugBacktrace backtrace;
		CorDebugThread thread;
		uint threadId;

		public MicroFrameworkDebuggerSession Session { get; set; }

		internal CorEvaluationContext (MicroFrameworkDebuggerSession session, CorDebugBacktrace backtrace, int index, DC.EvaluationOptions ops) : base (ops)
		{
			Session = session;
			base.Adapter = session.ObjectAdapter;
			frameIndex = index;
			this.backtrace = backtrace;
			evalTimestamp = MicroFrameworkDebuggerSession.EvaluationTimestamp;
			Evaluator = session.GetEvaluator (CorDebugBacktrace.CreateFrame (session, Frame));
		}

		public MicroFrameworkObjectValueAdaptor Adaptor {
			get { return (MicroFrameworkObjectValueAdaptor)base.Adapter; }
		}

		void CheckTimestamp ()
		{
			if (evalTimestamp != MicroFrameworkDebuggerSession.EvaluationTimestamp) {
				thread = null;
				frame = null;
				corEval = null;
			}
		}

		public CorDebugThread Thread {
			get {
				CheckTimestamp ();
				if (thread == null)
					thread = Session.process.GetThread (threadId);
				return thread;
			}
			set {
				thread = value;
				threadId = thread.Id;
			}
		}

		public CorDebugFrame Frame {
			get {
				CheckTimestamp ();
				if (frame == null) {
					frame = backtrace.FrameList [frameIndex];
				}
				return frame;
			}
		}

		public CorDebugEval Eval {
			get {
				CheckTimestamp ();
				if (corEval == null)
					corEval = Thread.CurrentEval;
				return corEval;
			}
		}

		public override void CopyFrom (EvaluationContext ctx)
		{
			base.CopyFrom (ctx);
			frame = ((CorEvaluationContext)ctx).frame;
			frameIndex = ((CorEvaluationContext)ctx).frameIndex;
			evalTimestamp = ((CorEvaluationContext)ctx).evalTimestamp;
			Thread = ((CorEvaluationContext)ctx).Thread;
			Session = ((CorEvaluationContext)ctx).Session;
		}

		public override void WriteDebuggerError (Exception ex)
		{
			Session.Frontend.NotifyDebuggerOutput (true, ex.Message);
		}

		public override void WriteDebuggerOutput (string message, params object[] values)
		{
			Session.Frontend.NotifyDebuggerOutput (false, string.Format (message, values));
		}

		public CorDebugValue RuntimeInvoke (CorDebugFunction function, CorDebugType[] typeArgs, CorDebugValue thisObj, CorDebugValue[] arguments)
		{
			return Session.RuntimeInvoke (this, function, typeArgs, thisObj, arguments);
		}
	}
}
