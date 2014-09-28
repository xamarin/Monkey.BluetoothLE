using System;
using MonoDevelop.Projects;
using System.Xml;
using System.IO;

namespace MonoDevelop.MicroFramework
{
	public class MicroFrameworkProjectBinding : IProjectBinding
	{
		public string Name
		{
			get { return ".NETMicroFramework"; }
		}

		public Project CreateProject (ProjectCreateInformation info, XmlElement projectOptions)
		{
			string lang = projectOptions.GetAttribute ("language");
			return CreateProject (lang, info, projectOptions);
		}

		protected DotNetProject CreateProject (string languageName, ProjectCreateInformation info, XmlElement projectOptions)
		{
			return new MicroFrameworkProject(languageName, info, projectOptions);
		}

		public Project CreateSingleFileProject (string file)
		{
			IDotNetLanguageBinding binding = LanguageBindingService.GetBindingPerFileName (file) as IDotNetLanguageBinding;
			if (binding != null) {
				ProjectCreateInformation info = new ProjectCreateInformation ();
				info.ProjectName = Path.GetFileNameWithoutExtension (file);
				info.SolutionPath = Path.GetDirectoryName (file);
				info.ProjectBasePath = Path.GetDirectoryName (file);
				Project project = CreateProject (binding.Language, info, null);
				project.Files.Add (new ProjectFile (file));
				return project;
			}
			return null;
		}

		public bool CanCreateSingleFileProject (string file)
		{
			IDotNetLanguageBinding binding = LanguageBindingService.GetBindingPerFileName (file) as IDotNetLanguageBinding;
			return binding != null;
		}
	}
}

