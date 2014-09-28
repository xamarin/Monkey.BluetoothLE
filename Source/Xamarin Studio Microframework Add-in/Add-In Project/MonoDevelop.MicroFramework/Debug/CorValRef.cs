using System;
using Microsoft.SPOT.Debugger;

namespace MonoDevelop.MicroFramework
{
	public class CorValRef
	{
		CorDebugValue val;
		readonly ValueLoader loader;
		int version;

		public delegate CorDebugValue ValueLoader ();

		public CorValRef (CorDebugValue val)
		{
			this.val = val;
			this.version = MicroFrameworkDebuggerSession.EvaluationTimestamp;
		}

		public CorValRef (CorDebugValue val, ValueLoader loader)
		{
			this.val = val;
			this.loader = loader;
			this.version = MicroFrameworkDebuggerSession.EvaluationTimestamp;
		}

		public CorValRef (ValueLoader loader)
		{
			this.val = loader ();
			this.loader = loader;
			this.version = MicroFrameworkDebuggerSession.EvaluationTimestamp;
		}

		public bool IsValid {
			get { return version == MicroFrameworkDebuggerSession.EvaluationTimestamp; }
			set {
				if (value)
					version = MicroFrameworkDebuggerSession.EvaluationTimestamp;
				else
					version = -1;
			}
		}

		public void Reload ()
		{
			if (loader != null) {
				// Obsolete value, get a new one
				CorDebugValue v = loader ();
				version = MicroFrameworkDebuggerSession.EvaluationTimestamp;
				if (v != null)
					val = v;
			}
		}

		public CorDebugValue Val {
			get {
				if (version < MicroFrameworkDebuggerSession.EvaluationTimestamp) {
					Reload ();
				}
				return val;
			}
		}

		public void SetValue (CorEvaluationContext ctx, CorValRef corValRef)
		{
			throw new NotImplementedException ();
		}
	}
}

