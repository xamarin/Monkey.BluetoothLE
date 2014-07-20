using System;
using Mono.Debugging.Evaluation;
using Microsoft.SPOT.Debugger;
using System.Collections;

namespace MonoDevelop.MicroFramework
{
	class ArrayAdaptor: ICollectionAdaptor
	{
		readonly CorEvaluationContext ctx;
		CorDebugValueArray array;
		public readonly CorValRef obj;

		public ArrayAdaptor (EvaluationContext ctx, CorValRef obj, CorDebugValueArray array)
		{
			this.ctx = (CorEvaluationContext)ctx;
			this.array = array;
			this.obj = obj;
		}

		public int[] GetDimensions ()
		{
			return array != null ? array.GetDimensions () : new int[0];
		}

		public object GetElement (int[] indices)
		{
			return new CorValRef (delegate {
				// If we have a zombie state array, reload it.
				if (!obj.IsValid) {
					obj.Reload ();
					array = MicroFrameworkObjectValueAdaptor.GetRealObject (ctx, obj) as CorDebugValueArray;
				}

				return array != null ? array.GetElement (indices) : null;
			});
		}

		public Array GetElements (int[] indices, int count)
		{
			// FIXME: the point of this method is to be more efficient than getting 1 array element at a time...
			var elements = new ArrayList ();

			int[] idx = new int[indices.Length];
			for (int i = 0; i < indices.Length; i++)
				idx [i] = indices [i];

			for (int i = 0; i < count; i++) {
				elements.Add (GetElement (idx));
				idx [idx.Length - 1]++;//I think this is bug
			}

			return elements.ToArray ();
		}

		public void SetElement (int[] indices, object val)
		{
			CorValRef it = (CorValRef)GetElement (indices);
			it.SetValue (ctx, (CorValRef)val);
		}

		public object ElementType {
			get {
				return array.ExactType.FirstTypeParameter;
			}
		}
	}

	public class StringAdaptor: IStringAdaptor
	{
		readonly CorEvaluationContext ctx;
		readonly CorDebugValueString str;
		readonly CorValRef obj;

		public StringAdaptor (EvaluationContext ctx, CorValRef obj, CorDebugValueString str)
		{
			this.ctx = (CorEvaluationContext)ctx;
			this.str = str;
			this.obj = obj;
		}

		public int Length {
			get { return str.String.Length; }
		}

		public string Value {
			get { return str.String; }
		}

		public string Substring (int index, int length)
		{
			return str.String.Substring (index, length);
		}
	}
}

