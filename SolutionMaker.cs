using Microsoft.Build.Evaluation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace SolGen
{
    public class SolutionMaker
    {
        private readonly string [] _buildConfigurations;
        private const string CsProjFileExtension = ".csproj";
        private const string VcxProjFileExtension = ".vcxproj";
        private const string FsProjFileExtension = ".fsproj";

        private const string ProjectGuidPropertyName = "ProjectGuid";
        private const string PlatformPropertyName = "Platform";
        private const string ProjectReferencePropertyName = "ProjectReference";
        private const string ProjectFilePropertyName = "ProjectFile";

        private const string CsProjGuid = "{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}";
        private const string FolderGuid = "{2150E333-8FDC-42A3-9474-1A3956D46DE8}";
        private const string VcxProjGuid = "{8BC9CEB8-8B4A-11D0-8D11-00A0C91BC942}";
        private const string FsProjGuid = "{F2A71F9B-5D33-465A-A702-920D77279786}";

        private readonly string _rootFolder;
        private readonly string _solutionFileName;
        private readonly Dictionary<string, ProjectInfo> _solutionProjects;
        private string _commonRoot;

        public SolutionMaker(string solutionFilePath, string [] buildConfigurations)
        {
            if (buildConfigurations == null || buildConfigurations.Length == 0)
            {
                buildConfigurations = new [] { "Any CPU" };
            }

            _buildConfigurations = buildConfigurations;
            _rootFolder = Path.GetDirectoryName(solutionFilePath);
            _solutionFileName = Path.GetFileName(solutionFilePath);
            _solutionProjects = new Dictionary<string, ProjectInfo>(StringComparer.CurrentCultureIgnoreCase);
            _commonRoot = _rootFolder;
        }

        public void AddProject(string path)
        {
            ProcessProjectFile(path);
        }

        public void CreateSolution()
        {
            WriteSolutionFile(Path.Combine(_rootFolder, _solutionFileName));
        }

        private void ProcessProjectFile(string path)
        {
            if (_solutionProjects.ContainsKey(path))
                return;

            try
            {
                ProjectCollection collection = new ProjectCollection();
                collection.RemoveGlobalProperty("Platform");
                Dictionary<string, string> properties = new Dictionary<string, string>();
                Project project = new Project(path, properties, null, collection, ProjectLoadSettings.IgnoreMissingImports);
                ProjectInfo pinfo = new ProjectInfo
                {
                    MsBuildProject = project,
                    FilePath = Path.GetDirectoryName(path),
                    Filename = Path.GetFileName(path)
                };

                foreach (ProjectProperty buildProperty in project.Properties)
                {
                    if (buildProperty.Name == ProjectGuidPropertyName)
                    {
                        pinfo.ProjectGuid = buildProperty.EvaluatedValue;
                    }

                    if (buildProperty.Name == PlatformPropertyName)
                    {
                        pinfo.Platform = buildProperty.EvaluatedValue;
                    }
                }

                _solutionProjects[path] = pinfo;

                CreatePath(pinfo);

                GatherProjectReferences(pinfo);
                Console.WriteLine(pinfo);
            }
            catch (Exception e)
            {
                Console.Error.WriteLine(e);
            }
        }

        /// <summary>
        /// Retrieves the information on references in the project
        /// </summary>
        private void GatherProjectReferences(ProjectInfo projectInfo)
        {
            foreach (ProjectItem buildItem in projectInfo.MsBuildProject.Items)
            {
                if (buildItem.ItemType == ProjectReferencePropertyName || buildItem.ItemType == ProjectFilePropertyName)
                {
                    projectInfo.References.Add(buildItem.EvaluatedInclude);
                    ProcessProjectFile(Path.GetFullPath(Path.Combine(projectInfo.FilePath, buildItem.EvaluatedInclude)));
                }
            }
        }

        private void CreatePath(ProjectInfo projectInfo)
        {
            string projectFolderPath = !projectInfo.IsFolder ? 
                Path.GetDirectoryName(projectInfo.FilePath) :
                projectInfo.FilePath;

            if (projectFolderPath == null)
                return;

            if (!_solutionProjects.ContainsKey(projectFolderPath))
            {
                var folder = new ProjectInfo
                {
                    Filename = Path.GetFileName(projectFolderPath),
                    FilePath = Path.GetDirectoryName(projectFolderPath),
                    IsFolder = true
                };

                if (string.IsNullOrEmpty(folder.Filename))
                    return;

                if (_commonRoot.StartsWith(projectFolderPath, StringComparison.CurrentCultureIgnoreCase))
                {
                    _commonRoot = projectFolderPath;
                }

                else if (!projectFolderPath.StartsWith(_commonRoot, StringComparison.CurrentCultureIgnoreCase))
                {
                    _commonRoot = string.Empty;
                }

                _solutionProjects.Add(projectFolderPath, folder);

                projectInfo.FolderGuid = folder.ProjectGuid;
                CreatePath(folder);
            }
            else
            {
                projectInfo.FolderGuid = _solutionProjects[projectFolderPath].ProjectGuid;
            }
        }

        private void WriteSolutionFile(string solutionFile)
        {
            StreamWriter writer = new StreamWriter(solutionFile);

            writer.WriteLine("Microsoft Visual Studio Solution File, Format Version 11.00");
            writer.WriteLine("# Visual Studio 2010");

            foreach (ProjectInfo projectInfo in _solutionProjects.Values)
            {
                var projectInfoCopy = projectInfo.ShallowCopy();
                if (!projectInfoCopy.IsFolder || 
                    string.Compare(Path.Combine(projectInfoCopy.FilePath, projectInfoCopy.Filename), _commonRoot, StringComparison.InvariantCultureIgnoreCase) != 0)
                {
                    WriteProjectEntry(writer, projectInfoCopy, Path.GetDirectoryName(solutionFile));
                }
            }

            // Project and folder relations
            writer.WriteLine("Global");
            writer.WriteLine("\tGlobalSection(NestedProjects) = preSolution");
            const string format = "\t\t{0} = {1}";

            // Folder relations
            foreach (ProjectInfo folderInfo in _solutionProjects.Values)
            {
                if (folderInfo.ProjectGuid != null && folderInfo.FolderGuid != null && folderInfo.FilePath != _commonRoot)
                {
                    writer.WriteLine(format, folderInfo.ProjectGuid, folderInfo.FolderGuid);
                }
            }

            writer.WriteLine("\tEndGlobalSection");
            writer.WriteLine("\tGlobalSection(SolutionConfigurationPlatforms) = preSolution");
            string [] buildModes = { "Debug", "Release" };
            foreach(var buildMode in buildModes)
            {
                foreach (var buildConfig in _buildConfigurations)
                {
                    writer.WriteLine("\t\t{0}|{1} = {0}|{1}", buildMode, buildConfig);
                }
            }

            writer.WriteLine("\tEndGlobalSection");
            writer.WriteLine("\tGlobalSection(ProjectConfigurationPlatforms) = postSolution");
            foreach (ProjectInfo projectInfo in _solutionProjects.Values.Where(prj => !prj.IsFolder))
            {
                foreach (var buildMode in buildModes)
                {
                    foreach (var buildConfig in _buildConfigurations)
                    {
                        string bc = buildConfig != "Mixed Platforms" ? buildConfig : projectInfo.Platform;
                        if (bc == "AnyCPU")
                        {
                            bc = "Any CPU";
                        }

                        writer.WriteLine("\t\t{0}.{1}|{2}.ActiveCfg = {3}|{4}", projectInfo.ProjectGuid, buildMode, buildConfig, buildMode, bc);
                        writer.WriteLine("\t\t{0}.{1}|{2}.Build.0 = {3}|{4}", projectInfo.ProjectGuid, buildMode, buildConfig, buildMode, bc);
                    }
                }
            }
            writer.WriteLine("\tEndGlobalSection");

            writer.WriteLine("EndGlobal");
            writer.Close();
        }

        private static string LookupGuid(string extension)
        {
            switch (extension.ToLower())
            {
                case CsProjFileExtension:
                    return CsProjGuid;
                case VcxProjFileExtension:
                    return VcxProjGuid;
                case FsProjFileExtension:
                    return FsProjGuid;
                default:
                    return null;
            }
        }

        private static void WriteProjectEntry(TextWriter writer, ProjectInfo projectInfo, string rootFolder)
        {
            string projectPath;
            string guid;

            if (projectInfo.IsFolder == false)
            {
                string projectDir = Path.GetDirectoryName(projectInfo.FilePath) ?? string.Empty;
                if (projectDir.StartsWith(rootFolder, StringComparison.InvariantCultureIgnoreCase))
                {
                    projectPath = Path.Combine(projectInfo.FilePath, projectInfo.Filename).Substring(rootFolder.Length + 1);
                }
                else
                {
                    projectPath = GetRelativePath(rootFolder, Path.Combine(projectInfo.FilePath, projectInfo.Filename));
                }

                guid = LookupGuid(Path.GetExtension(projectInfo.Filename));
            }
            else
            {
                projectPath = projectInfo.ProjectGuid;
                guid = FolderGuid;
            }

            if (guid != null)
            {
                string format = "Project('{0}') = '{1}', '{2}', '{3}'".Replace('\'', '"');
                writer.WriteLine(format, guid, projectInfo.Filename, projectPath, projectInfo.ProjectGuid);
                writer.WriteLine("EndProject");
            }
        }

        private static string GetRelativePath(string fromPath, string toPath)
        {
            string[] fromDirectories = fromPath.Split(Path.DirectorySeparatorChar);
            string[] toDirectories = toPath.Split(Path.DirectorySeparatorChar);

            // Get the shortest of the two paths
            int length = fromDirectories.Length < toDirectories.Length
                             ? fromDirectories.Length
                             : toDirectories.Length;

            int lastCommonRoot = -1;
            int index;

            // Find common root
            for (index = 0; index < length; index++)
            {
                if (fromDirectories[index].Equals(toDirectories[index], StringComparison.InvariantCultureIgnoreCase))
                {
                    lastCommonRoot = index;
                }
                else
                {
                    break;
                }
            }

            // If we didn't find a common prefix then abandon
            if (lastCommonRoot == -1)
            {
                return null;
            }

            // Add the required number of "..\" to move up to common root level
            StringBuilder relativePath = new StringBuilder();
            for (index = lastCommonRoot + 1; index < fromDirectories.Length; index++)
            {
                relativePath.Append(".." + Path.DirectorySeparatorChar);
            }

            // Add on the folders to reach the destination
            for (index = lastCommonRoot + 1; index < toDirectories.Length - 1; index++)
            {
                relativePath.Append(toDirectories[index] + Path.DirectorySeparatorChar);
            }

            relativePath.Append(toDirectories[toDirectories.Length - 1]);

            return relativePath.ToString();
        }

        /// <summary>
        /// Represents a project reference or loaded project.
        /// </summary>
        private class ProjectInfo
        {
            public ProjectInfo()
            {
                ProjectGuid = Guid.NewGuid().ToString("B");
                IsFolder = false;
            }

            public string ProjectGuid = null;
            public string Filename = null;
            public string FilePath { get; set; }
            public string FolderGuid = null;
            public bool IsFolder { get; set; }
            public string Platform { get; set; }

            // list of assemblynames
            public readonly List<string> References = new List<string>();

            public Project MsBuildProject;

            public ProjectInfo ShallowCopy()
            {
                return (ProjectInfo)this.MemberwiseClone();
            }

            public override string ToString()
            {
                StringBuilder sb = new StringBuilder();
                sb.AppendFormat("'{0}', ", Filename);
                sb.AppendFormat("'{0}', ", FilePath);

                return sb.ToString();
            }
        }
    }
}
