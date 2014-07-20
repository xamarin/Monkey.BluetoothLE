using System;
using Mono.Debugging.Evaluation;
using Mono.Debugging.Client;

namespace MonoDevelop.MicroFramework
{
	public class VariableValueReference: ValueReference
	{
		readonly CorValRef var;
		readonly ObjectValueFlags flags;
		readonly string name;

		public VariableValueReference (EvaluationContext ctx, CorValRef var, string name, ObjectValueFlags flags)
			: base (ctx)
		{
			this.flags = flags;
			this.var = var;
			this.name = name;
		}

		public override object Value {
			get {
				return var;
			}
			set {
				throw new NotImplementedException ();
//				var.SetValue (Context, (CorValRef) value);
			}
		}

		public override string Name {
			get {
				return name;
			}
		}

		public override object Type {
			get {
				return var.Val.ExactType;
			}
		}

		public override ObjectValueFlags Flags {
			get {
				return flags;
			}
		}
	}
}

