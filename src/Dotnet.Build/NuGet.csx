#load "Command.csx"
#load "FileUtils.csx"
#load "Internalizer.csx"
#load "DotNet.csx"
using System.Text.RegularExpressions;
using System.Xml.Linq;
using static FileUtils;
using static Internalizer;


public static class NuGet
{
    private const string DefaultSource = "https://www.nuget.org/api/v2/package";

    private static string ApiKey = System.Environment.GetEnvironmentVariable("NUGET_APIKEY");

    public static void Push(string packagesFolder, string source = DefaultSource)
    {
        var packageFiles = Directory.GetFiles(packagesFolder, "*.nupkg");
        foreach (var packageFile in packageFiles)
        {
            Command.Execute("nuget", $"push {packageFile} -Source {source} -ApiKey {ApiKey}");
        }
    }

    public static void Install(string packageName, string outputDirectory)
    {
        Command.Execute("nuget", $"install {packageName} -OutputDirectory {outputDirectory}", ".");
    }

    public static void TryPush(string packagesFolder, string source = DefaultSource)
    {
        var packageFiles = Directory.GetFiles(packagesFolder, "*.nupkg");
        foreach (var packageFile in packageFiles)
        {
            Command.Capture("nuget", $"push {packageFile} -Source {source} -ApiKey {ApiKey}").Dump();
        }
    }

    public static void TryPush()
    {
        TryPush(BuildContext.NuGetArtifactsFolder);
    }
    public static void Pack(string pathToMetadataFolder, string outputFolder, string version = "")
    {
        string versionArgument = "";

        if (!string.IsNullOrWhiteSpace(version))
        {
            versionArgument = $"-version {version}";
        }

        var spec = Directory.GetFiles(pathToMetadataFolder, "*.nuspec").Single();
        Command.Execute("nuget", $"pack {spec} {versionArgument} -OutputDirectory {outputFolder}");
    }

    public static void CreateSourcePackage(string pathToRepository, string projectName, string outputFolder, params PackageReference[] dependencies)
    {
        NuGetUtils.CreateSourcePackage(pathToRepository, projectName, outputFolder, dependencies);
    }

    public static void PackAsTool(string pathToProjectFolder, string pathToBinaries, string outputFolder)
    {
        string pathToProjectFile = Directory.GetFiles(pathToProjectFolder, "*.csproj").Single();
        string packageId = ReadAssemblyName(pathToProjectFile);
        using (var disposableFolder = new DisposableFolder())
        {
            CreateSpecificationFromProject(pathToProjectFile, pathToBinaries, packageId, disposableFolder.Path);
            NuGet.Pack(disposableFolder.Path, outputFolder);
        }
    }

    private static string ReadAssemblyName(string pathToProjectFile)
    {
        var projectFile = XDocument.Load(pathToProjectFile);
        var assemblyName = projectFile.Descendants("AssemblyName").SingleOrDefault()?.Value;
        if (assemblyName == null)
        {
            return Path.GetFileNameWithoutExtension(pathToProjectFile);
        }
        return assemblyName;
    }

    private static string CreateDotnetToolSettings(string projectFile, string packageId, string packageBuildFolder)
    {
        var entryPoint = $"{packageId}.dll";
        var commandName = packageId;
        var runner = "dotnet";

        var commandNameAttribute = new XAttribute("Name", commandName);
        var entryPointAttribute = new XAttribute("EntryPoint", entryPoint);
        var runnerAttribute = new XAttribute("Runner", runner);


        var commandElement = new XElement("Command", commandNameAttribute, entryPointAttribute, runnerAttribute);
        var commandsElement = new XElement("Commands", commandElement);
        var dotNetCliToolElement = new XElement("DotNetCliTool", commandsElement);

        var fileName = Path.Combine(packageBuildFolder, "DotnetToolSettings.xml");
        using (var fileStream = new FileStream(Path.Combine(packageBuildFolder, "DotnetToolSettings.xml"), FileMode.Create))
        {
            new XDocument(dotNetCliToolElement).Save(fileStream);
        }
        return fileName;
    }

    private static void CreateSpecificationFromProject(string pathToProjectFile, string pathToBinaries, string packageId, string packageBuildFolder)
    {
        var projectFile = XDocument.Load(pathToProjectFile);
        var authors = projectFile.Descendants("Authors").SingleOrDefault()?.Value ?? "Warning: Provide a value for authors";
        var description = projectFile.Descendants("Description").SingleOrDefault()?.Value ?? "Warning: Provide a value for description";
        var versionPrefix = projectFile.Descendants("VersionPrefix").SingleOrDefault()?.Value;
        var versionSuffix = projectFile.Descendants("VersionSuffix").SingleOrDefault()?.Value;

        string version;
        if (versionSuffix != null)
        {
            version = $"{versionPrefix}-{versionSuffix}";
        }
        else
        {
            version = versionPrefix;
        }
        if (version == null)
        {
            version = "1.0.0";
        }

        var tags = projectFile.Descendants("PackageTags").SingleOrDefault()?.Value;
        var iconUrl = projectFile.Descendants("PackageIconUrl").SingleOrDefault()?.Value;
        var projectUrl = projectFile.Descendants("PackageProjectUrl").SingleOrDefault()?.Value;
        var licenseUrl = projectFile.Descendants("PackageLicenseUrl").SingleOrDefault()?.Value;
        var repositoryUrl = projectFile.Descendants("RepositoryUrl").SingleOrDefault()?.Value;

        var packageElement = new XElement("package");
        var metadataElement = new XElement("metadata");
        packageElement.Add(metadataElement);

        metadataElement.Add(new XElement("id", packageId.ToLower()));
        metadataElement.Add(new XElement("version", version));

        metadataElement.Add(new XElement("authors", authors));

        if (!string.IsNullOrWhiteSpace(licenseUrl))
        {
            metadataElement.Add(new XElement("licenseUrl", licenseUrl));
        }
        if (!string.IsNullOrWhiteSpace(projectUrl))
        {
            metadataElement.Add(new XElement("projectUrl", projectUrl));
        }

        if (!string.IsNullOrWhiteSpace(iconUrl))
        {
            metadataElement.Add(new XElement("iconUrl", iconUrl));
        }

        metadataElement.Add(new XElement("description", description));
        metadataElement.Add(new XElement("tags", repositoryUrl));
        metadataElement.Add(new XElement("packageTypes", new XElement("packageType", new XAttribute("name", "DotnetTool"))));
        metadataElement.Add(new XElement("dependencies", new XElement("dependency", new XAttribute("id", "Microsoft.NETCore.Platforms"), new XAttribute("version", "2.0.1"))));
        var filesElement = new XElement("files");
        packageElement.Add(filesElement);

        var srcGlobPattern = $@"{pathToBinaries}\**\*";
        filesElement.Add(CreateFileElement(srcGlobPattern, @"tools\netcoreapp2.0\any"));
        var dotnetToolSettingsFile = CreateDotnetToolSettings(pathToProjectFile, packageId, packageBuildFolder);
        filesElement.Add(CreateFileElement(dotnetToolSettingsFile, @"tools\netcoreapp2.0\any"));

        using (var fileStream = new FileStream(Path.Combine(packageBuildFolder, "dotnet-script.nuspec"), FileMode.Create))
        {
            new XDocument(packageElement).Save(fileStream);
        }
    }

    private static XElement CreateFileElement(string src, string target)
    {
        var srcAttribute = new XAttribute("src", src);
        var targetAttribute = new XAttribute("target", target);
        return new XElement("file", srcAttribute, targetAttribute);
    }
}

public class NuGetMetadataFile
{
    private const string Template = "<?xml version=\"1.0\" encoding=\"utf-8\"?><package/>";

    private string _id;

    private string _authors;

    private string _version;

    private string _owners;

    private string _projectUrl;

    private string _description;

    private string _tags;

    private string _copyright;

    private string _releaseNotesUrl;

    private string _license = "MIT";
    private List<(string id, string version)> dependencies = new List<(string id, string version)>();

    private List<string> _contentFiles = new List<string>();

    private List<(string str, string target)> _files = new List<(string str, string target)>();

    public NuGetMetadataFile WithId(string id)
    {
        _id = id;
        return this;
    }

    public NuGetMetadataFile WithVersion(string version)
    {
        _version = version;
        return this;
    }

    public NuGetMetadataFile WithAuthors(string authors)
    {
        _authors = authors;
        return this;
    }

    public NuGetMetadataFile WithOwners(string owners)
    {
        _owners = owners;
        return this;
    }

    public NuGetMetadataFile WithProjectUrl(string projectUrl)
    {
        _projectUrl = projectUrl;
        return this;
    }

    public NuGetMetadataFile WithDescription(string description)
    {
        _description = description;
        return this;
    }

    public NuGetMetadataFile WithTags(string tags)
    {
        _tags = tags;
        return this;
    }

    public NuGetMetadataFile WithCopyright(string copyright)
    {
        _copyright = copyright;
        return this;
    }

    public NuGetMetadataFile WithReleaseNotesUrl(string releaseNotesUrl)
    {
        _releaseNotesUrl = releaseNotesUrl;
        return this;
    }

    public NuGetMetadataFile WithLicense(string licence)
    {
        _license = licence;
        return this;
    }


    public NuGetMetadataFile AddDependency(string id, string version)
    {
        dependencies.Add((id, version));
        return this;
    }

    public NuGetMetadataFile AddFile(string source, string target = null)
    {
        _files.Add((source, target ?? source));
        return this;
    }

    public NuGetMetadataFile AddContentFile(string file)
    {
        _contentFiles.Add(file);
        return this;
    }

    public void Save(string url)
    {
        var document = XDocument.Parse(Template);
        var packageElement = document.Descendants("package").First();
        var metadataElement = new XElement("metadata");
        AddElement(metadataElement, "id", $"{_id}.Source");
        AddElement(metadataElement, "version", _version);
        AddElement(metadataElement, "authors", _authors);
        AddElement(metadataElement, "owners", _owners);
        AddElement(metadataElement, "projectUrl", _projectUrl);
        AddElement(metadataElement, "description", _description);
        AddElement(metadataElement, "tags", _tags);
        AddElement(metadataElement, "copyright", _copyright);
        AddElement(metadataElement, "releaseNotes", _releaseNotesUrl);
        //metadataElement.Add(new XElement("license", new XAttribute("type", "expression"), _license));
        packageElement.Add(metadataElement);
        packageElement.Add(CreateFilesElement());
        metadataElement.Add(CreateContentFilesElement());

        document.Save(url);
    }

    private XElement CreateContentFilesElement()
    {
        var contentFilesElement = new XElement("contentFiles");
        foreach (var contentFile in _contentFiles)
        {
            contentFilesElement.Add(new XElement("files", new XAttribute("include", contentFile)));
        }
        return contentFilesElement;
    }

    private XElement CreateFilesElement()
    {
        var filesElement = new XElement("files");
        foreach (var file in _files)
        {
            filesElement.Add(new XElement("file", new XAttribute("src", file.str), new XAttribute("target", file.target)));
        }
        return filesElement;
    }



    private void AddElement(XElement parent, string name, string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return;
        }
        parent.Add(new XElement(name, value));
    }
}

public static class NuGetUtils
{

    public static void CreateSourcePackage(string pathToRepository, string projectName, string outputFolder, params PackageReference[] dependencies)
    {
        if (dependencies == null)
        {
            dependencies = Array.Empty<PackageReference>();
        }

        using (var sourceRepoFolder = new DisposableFolder())
        {
            Copy(pathToRepository, sourceRepoFolder.Path, new[] { ".vs", "obj" });

            var pathToProjectFile = FindFile(sourceRepoFolder.Path, $"{projectName}.csproj");
            var projectFile = XDocument.Load(pathToProjectFile);
            string pathToSourceProjectFolder = Path.GetDirectoryName(pathToProjectFile);
            string pathToSourceFile = Path.Combine(pathToSourceProjectFolder, $"{projectName}.cs");
            var targetFrameworks = GetTargetFrameworks(projectFile);

            var publicTypes = GetPublicTypes(pathToSourceFile);
            Internalize(pathToSourceProjectFolder, publicTypes);
            string pathToTestProject = Path.Combine(sourceRepoFolder.Path, "src", $"{projectName}.Tests");
            //DotNet.Test(pathToTestProject);
            DotNet.Build(pathToSourceProjectFolder);
            var nugetMetadataFile = CreateMetadataFromProjectFile(projectFile, projectName);
            using (var nugetPackFolder = new DisposableFolder())
            {

                foreach (var targetFramework in targetFrameworks)
                {
                    CopySourceFile(nugetPackFolder.Path, pathToSourceFile, projectName, targetFramework);
                    nugetMetadataFile.AddContentFile($"cs/{targetFramework}/{projectName}.cs");
                    nugetMetadataFile.AddFile($"contentFiles/cs/{targetFramework}/{projectName}/{projectName}.cs.pp");
                }
                var pathToNuGetMetadata = Path.Combine(nugetPackFolder.Path, $"{projectName}.Source.nuspec");

                nugetMetadataFile.Save(pathToNuGetMetadata);
                WriteLine(File.ReadAllText(pathToNuGetMetadata));


                NuGet.Pack(nugetPackFolder.Path, outputFolder);
            }
        }
    }

    private static void CopySourceFile(string nugetPackFolder, string pathToSourceFile, string projectName, string targetFramework)
    {
        var contentFolder = CreateDirectory(nugetPackFolder, "contentFiles", "cs", targetFramework, projectName);
        string pathToSourceFileTemplate = Path.Combine(contentFolder, $"{projectName}.cs.pp");
        Copy(pathToSourceFile, pathToSourceFileTemplate);
        var frameworkConstant = targetFramework.ToUpper().Replace(".", "_");
        var lines = File.ReadAllLines(pathToSourceFileTemplate).ToList();
        lines.Insert(0, $"#define {frameworkConstant}");
        File.WriteAllLines(pathToSourceFileTemplate, lines);
        FileUtils.ReplaceInFile(@"namespace \S*", $"namespace $rootnamespace$.{projectName}", pathToSourceFileTemplate);
    }

    private static string[] GetTargetFrameworks(XDocument projectFile)
    {
        var targetFrameworkElement = projectFile.Descendants("TargetFramework").FirstOrDefault();
        if (targetFrameworkElement != null)
        {
            return new[] { targetFrameworkElement.Value };
        }
        else
        {
            var targetFrameworksElement = projectFile.Descendants("TargetFrameworks").FirstOrDefault();
            return targetFrameworksElement.Value.Split(";", StringSplitOptions.RemoveEmptyEntries);
        }
    }

    private static string[] GetPublicTypes(string pathToSourceFile)
    {
        const string pattern = @"^\s*\[NoInternalize\]\n\s*public\s*(?>class|interface|enum)\s*(\S*)\s.*\n";
        var content = File.ReadAllText(pathToSourceFile);
        var matches = Regex.Matches(content, pattern);
        return matches.Select(m => m.Groups[1].Value).ToArray();
    }

    private static NuGetMetadataFile CreateMetadataFromProjectFile(XDocument projectFile, string projectName)
    {
        return new NuGetMetadataFile()
            .WithId(projectName)
            .WithVersion(projectFile.Descendants("Version").FirstOrDefault()?.Value)
            .WithAuthors(projectFile.Descendants("Authors").FirstOrDefault()?.Value)
            .WithOwners(projectFile.Descendants("Owners").FirstOrDefault()?.Value)
            .WithCopyright(projectFile.Descendants("Copyright").FirstOrDefault()?.Value)
            .WithProjectUrl(projectFile.Descendants("PackageProjectUrl").FirstOrDefault()?.Value)
            .WithLicense(projectFile.Descendants("PackageLicenseExpression").FirstOrDefault()?.Value)
            .WithDescription(projectFile.Descendants("Description").FirstOrDefault()?.Value)
            .WithTags(projectFile.Descendants("Tags").FirstOrDefault()?.Value);
    }
}

public class PackageReference
{
    public PackageReference(string id, string version)
    {
        Id = id;
        Version = version;
    }

    public string Id { get; }
    public string Version { get; }
}