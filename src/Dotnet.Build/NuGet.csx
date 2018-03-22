#load "Command.csx"
#load "FileUtils.csx"
using System.Xml.Linq;
using static FileUtils;

public static class NuGet
{
    private const string DefaultSource = "https://www.nuget.org/api/v2/package";
    
    private static string ApiKey = System.Environment.GetEnvironmentVariable("NUGET_APIKEY");
           
    public static void Push(string packagesFolder, string source = DefaultSource)
    {
        var packageFiles = Directory.GetFiles(packagesFolder, "*.nupkg");        
        foreach(var packageFile in packageFiles)
        {            
            Command.Execute("nuget", $"push {packageFile} -Source {source} -ApiKey {ApiKey}");           
        }
    }

    public static void TryPush(string packagesFolder, string source = DefaultSource)
    {
        var packageFiles = Directory.GetFiles(packagesFolder, "*.nupkg");        
        foreach(var packageFile in packageFiles)
        {            
            Command.Capture("nuget", $"push {packageFile} -Source {source} -ApiKey {ApiKey}").Dump();           
        }
    }

    public static void Pack(string pathToMetadataFolder, string outputFolder)
    {
        var spec = Directory.GetFiles(pathToMetadataFolder,"*.nuspec").Single();
        Command.Execute("nuget",$"pack {spec} -OutputDirectory {outputFolder}");
    }

    public static void PackAsTool(string pathToProjectFolder, string pathToBinaries, string outputFolder)
    {
        string pathToProjectFile = Directory.GetFiles(pathToProjectFolder, "*.csproj").Single();
        string packageId = ReadAssemblyName(pathToProjectFile);
        using(var disposableFolder = new DisposableFolder())
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
        var entryPoint = $"{packageId}.dll" ;
        var commandName = packageId;
        var runner = "dotnet";

        var commandNameAttribute = new XAttribute("Name", commandName); 
        var entryPointAttribute = new XAttribute("EntryPoint", entryPoint);
        var runnerAttribute = new XAttribute("Runner", runner);

       
        var commandElement = new XElement("Command", commandNameAttribute, entryPointAttribute, runnerAttribute);   
        var commandsElement = new XElement("Commands", commandElement);
        var dotNetCliToolElement = new XElement("DotNetCliTool", commandsElement);  

        var fileName = Path.Combine(packageBuildFolder, "DotnetToolSettings.xml");
        using (var fileStream = new FileStream(Path.Combine(packageBuildFolder, "DotnetToolSettings.xml"),FileMode.Create))
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
        metadataElement.Add(new XElement("dependencies", new XElement("dependency", new XAttribute("id","Microsoft.NETCore.Platforms"), new XAttribute("version", "2.0.1"))));
        var filesElement = new XElement("files");
        packageElement.Add(filesElement);
        
        var srcGlobPattern = $@"{pathToBinaries}\**\*";
        filesElement.Add(CreateFileElement(srcGlobPattern,@"tools\netcoreapp2.0\any"));
        var dotnetToolSettingsFile = CreateDotnetToolSettings(pathToProjectFile, packageId, packageBuildFolder);
        filesElement.Add(CreateFileElement(dotnetToolSettingsFile, @"tools\netcoreapp2.0\any"));

        using (var fileStream = new FileStream(Path.Combine(packageBuildFolder, "dotnet-script.nuspec"),FileMode.Create))
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