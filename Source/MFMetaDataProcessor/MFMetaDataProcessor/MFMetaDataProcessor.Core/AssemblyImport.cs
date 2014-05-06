using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Mono.Cecil;

namespace MFMetaDataProcessor
{
	class AssemblyImport
	{
		public IEnumerable<AssemblyDefinition> Assemblies {
			get;
			set;
		}
	}

}
