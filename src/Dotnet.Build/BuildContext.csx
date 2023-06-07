#load "Git.csx"
#load "FileUtils.csx"

using System;
using System.Xml.Linq;
using static FileUtils;

public static class BuildContext
{
    private static Lazy<string> artifactsFolder;

    private static Lazy<string> nugetArtifactsFolder;

    private static Lazy<string> gitHubArtifactsFolder;

    private static Lazy<string> testCoverageArtifactsFolder;

    private static string repositoryFolder;

    static BuildContext()
    {
        RepositoryFolder = Git.Default.GetRepositoryInfo().RootFolder;
        InitializeBuildContext();
    }

    private static void InitializeBuildContext()
    {
        artifactsFolder = new Lazy<string>(() => CreateDirectory(BuildFolder, "Artifacts"));
        nugetArtifactsFolder = new Lazy<string>(() => CreateDirectory(ArtifactsFolder, "NuGet"));
        gitHubArtifactsFolder = new Lazy<string>(() => CreateDirectory(ArtifactsFolder, "GitHub"));
        testCoverageArtifactsFolder = new Lazy<string>(() => CreateDirectory(ArtifactsFolder, "TestCoverage"));
    }

    public static string Owner => Git.Open(RepositoryFolder).GetRepositoryInfo().Owner;

    public static string ProjectName => Git.Open(RepositoryFolder).GetRepositoryInfo().ProjectName;

    public static string BuildFolder => FindFolder("build");

    public static string RepositoryFolder { get => repositoryFolder; set { repositoryFolder = value; InitializeBuildContext(); } }

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
        var projectFile = XDocument.Load(pathToProjectFile);
        var isTestProjectElement = projectFile.Descendants("IsTestProject").FirstOrDefault();
        if (isTestProjectElement == null)
        {
            return pathToProjectFile.Contains("tests", StringComparison.OrdinalIgnoreCase);
        }
        else
        {
            if (isTestProjectElement.Value == null)
            {
                return true;
            }
            else
            {
                return isTestProjectElement.Value.Equals("true", StringComparison.OrdinalIgnoreCase);
            }
        }
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
        var isPublishable = projectFile.Descendants("IsPublishable").SingleOrDefault()?.Value;
        if (isPublishable == null)
        {
            return false;
        }

        return isPublishable.Equals(bool.TrueString, StringComparison.InvariantCultureIgnoreCase);
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

