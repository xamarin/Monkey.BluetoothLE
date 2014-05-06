using System;

namespace MFMetaDataProcessor
{
	public class MetaDataProcessor
	{
		Parser pr;

		bool fromAssembly;
		bool fromImage;

		/// <summary>
		/// Matches
		/// HRESULT Cmd_Compile( CLR_RT_ParseOptions::ParameterList* params = NULL )
		/// from MetaDataProcessor.cpp:952 in CLR/Tools/MetaDataProcessor
		/// </summary>
		public void Parse (string fileName)
		{
			fromAssembly = true;
			fromImage = false;

			pr = new Parser ();
			pr.Analyze (fileName);
		}

		/// <summary>
		/// Matches
		/// HRESULT Cmd_Compile( CLR_RT_ParseOptions::ParameterList* params = NULL )
		/// from MetaDataProcessor.cpp:952 in CLR/Tools/MetaDataProcessor
		/// </summary>
		public void Compile (string fileName)
		{
			throw new NotImplementedException ();
		}
	}
}

