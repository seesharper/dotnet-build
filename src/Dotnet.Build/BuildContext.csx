#load "Git.csx"
#load "FileUtils.csx"

using System;
using System.Xml.Linq;
using static FileUtils;

public static class BuildContext
{
    private static Lazy<string> artifactsFolder = new Lazy<string>(() => CreateDirectory(BuildFolder, "Artifacts"));

    private static Lazy<string> nugetArtifactsFolder = new Lazy<string>(() => CreateDirectory(ArtifactsFolder, "NuGet"));

    private static Lazy<string> gitHubArtifactsFolder = new Lazy<string>(() => CreateDirectory(ArtifactsFolder, "GitHub"));

    private static Lazy<string> testCoverageArtifactsFolder = new Lazy<string>(() => CreateDirectory(ArtifactsFolder, "TestCoverage"));

    static BuildContext()
    {
        RepositoryFolder = Git.Default.GetRepositoryInfo().RootFolder;
    }

    public static string Owner => Git.Open(RepositoryFolder).GetRepositoryInfo().Owner;

    public static string ProjectName => Git.Open(RepositoryFolder).GetRepositoryInfo().ProjectName;

    public static string BuildFolder => FindFolder("build");

    public static string RepositoryFolder { get; set; }

    public static string SourceFolder => FindFolder("src");

    public static string ArtifactsFolder => artifactsFolder.Value;

    public static string NuGetArtifactsFolder => nugetArtifactsFolder.Value;

    public static string GitHubArtifactsFolder => gitHubArtifactsFolder.Value;

    public static string GitHubReleaseNotesPath => Path.Combine(GitHubArtifactsFolder, "ReleaseNotes.md");

    public static string TestCoverageArtifactsFolder => testCoverageArtifactsFolder.Value;
    public static string[] TestProjects => FindProjectFiles().Where(IsTestProject).ToArray();

    public static string[] PackableProjects => SourceProjects.Where(IsPackable).ToArray();

    public static string[] PublishableProjects => SourceProjects.Where(IsPublishable).ToArray();

    public static string[] SourceProjects => FindProjectFiles().Where(p => !IsTestProject(p)).ToArray();

    public static string LatestTag => Git.Open(RepositoryFolder).GetLatestTag();

    public static bool IsTagCommit => Git.Open(RepositoryFolder).IsTagCommit();

    public static string CurrentShortCommitHash => Git.Open(RepositoryFolder).GetCurrentShortCommitHash();

    public static int CodeCoverageThreshold { get; set; } = 100;

    private static string FindSourceFolder()
    {
        var options = new EnumerationOptions();
        options.MatchCasing = MatchCasing.CaseInsensitive;
        options.RecurseSubdirectories = true;
        var sourceFolder = Directory.GetFiles(RepositoryFolder, "src", options).FirstOrDefault();
        if (sourceFolder == null)
        {
            throw new InvalidOperationException($"Source folder (src) not found in repository({RepositoryFolder})");
        }
        return sourceFolder;
    }

    private static string FindFolder(string folderName)
    {
        var options = new EnumerationOptions();
        options.MatchCasing = MatchCasing.CaseInsensitive;
        options.RecurseSubdirectories = true;
        var sourceFolder = Directory.GetDirectories(RepositoryFolder, folderName, options).FirstOrDefault();
        if (sourceFolder == null)
        {
            throw new InvalidOperationException($"Folder ({folderName}) not found in repository({RepositoryFolder})");
        }
        return sourceFolder;
    }


    private static string[] FindProjectFiles()
    {
        var options = new EnumerationOptions();
        options.MatchCasing = MatchCasing.CaseInsensitive;
        options.RecurseSubdirectories = true;
        var projectFiles = Directory.GetFiles(SourceFolder, "*.csproj", options);
        return projectFiles;
    }

    private static bool IsTestProject(string pathToProjectFile)
    {
        return pathToProjectFile.Contains("tests", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsPackable(string pathToProjectFile)
    {
        var projectFile = XDocument.Load(pathToProjectFile);
        var isPackable = projectFile.Descendants("IsPackable").SingleOrDefault()?.Value;
        if (isPackable == null)
        {
            return true;
        }

        return isPackable.Equals(bool.TrueString, StringComparison.InvariantCultureIgnoreCase);
    }

    private static bool IsPublishable(string pathToProjectFile)
    {
        var projectFile = XDocument.Load(pathToProjectFile);
        var isPackable = projectFile.Descendants("IsPackable").SingleOrDefault()?.Value;
        if (isPackable == null)
        {
            return true;
        }

        return isPackable.Equals(bool.TrueString, StringComparison.InvariantCultureIgnoreCase);
    }

    private static bool IsLibrary(string pathToProjectFile)
    {
        var projectFile = XDocument.Load(pathToProjectFile);
        var outputType = projectFile.Descendants("OutputType").SingleOrDefault()?.Value;
        if (outputType == null)
        {
            return true;
        }

        return outputType.Equals("library", StringComparison.InvariantCultureIgnoreCase);
    }
}

