using System;

namespace MFMetaDataProcessor.Console
{
	class MainClass
	{
		public static void Main (string[] args)
		{
			var md = new MetaDataProcessor ();

			for (int i = 0; i < args.Length; i++) {
				var arg = args [i];
				if (arg == "-parse" && i + 1 < args.Length) {
					md.Parse (args [i + 1]);
					i++;
				} else if (arg == "-compile" && i + 1 < args.Length) {
					md.Compile (args [i + 1]);
					i++;
				} else {
					// TODO: More args and commands
					throw new NotImplementedException ();
				}
			}
		}
	}
}
