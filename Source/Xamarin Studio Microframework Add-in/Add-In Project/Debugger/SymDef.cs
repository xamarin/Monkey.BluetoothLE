////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Copyright (c) Microsoft Corporation.  All rights reserved.
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

using System;
using System.Collections;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Reflection;
using System.Threading;
using System.Runtime.Serialization;

namespace Microsoft.SPOT.Debugger
{
	public class SymDef
	{
		static public void Parse(string file, Hashtable code, Hashtable data)
		{
			if(System.IO.File.Exists(file) == false)
			{
				throw new System.IO.FileNotFoundException(String.Format("Cannot find {0}", file));
			}

			using(System.IO.StreamReader reader = new StreamReader(file))
			{
				Regex reCode = new Regex("^0x([0-9a-fA-F]*) A (.*)");
				Regex reData = new Regex("^0x([0-9a-fA-F]*) D (.*)");
				string line;
				uint address;

				while((line = reader.ReadLine()) != null)
				{
					if(code != null && reCode.IsMatch(line))
					{
						GroupCollection group = reCode.Match(line).Groups;

						address = UInt32.Parse(group[1].Value, System.Globalization.NumberStyles.HexNumber);

						code[group[2].Value] = address;
					}

					if(data != null && reData.IsMatch(line))
					{
						GroupCollection group = reData.Match(line).Groups;

						address = UInt32.Parse(group[1].Value, System.Globalization.NumberStyles.HexNumber);

						data[group[2].Value] = address;
					}
				}
			}
		}
	}
}
