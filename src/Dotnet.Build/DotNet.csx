#load "Command.csx"
#load "FileUtils.csx"
#load "CodeCoverageReportGenerator.csx"
#load "BuildContext.csx"

using System.Xml.Linq;
using static FileUtils;

public static class DotNet
{
    /// <summary>
    /// Executes the tests in the given path. The path may the full path to a csproj file
    /// that represents a test project or it may be the
    /// </summary>
    /// <param name="path"></param>
    public static void Test(string path)
    {
        string pathToProjectFile = FindProjectFile(path);
        if (pathToProjectFile.EndsWith("csproj", StringComparison.InvariantCultureIgnoreCase))
        {
            Command.Execute("dotnet", "test " + pathToProjectFile + " --configuration Release");
            return;
        }

        if (pathToProjectFile.EndsWith("csx", StringComparison.InvariantCultureIgnoreCase))
        {
            Command.Execute("dotnet", $"script {path}");
            return;
        }

        throw new InvalidOperationException($"No tests found at the path {path}");
    }

    /// <summary>
    /// Executes all test projects found in <see cref="BuildContext.TestProjects"/>.
    /// </summary>
    public static void Test()
    {
        var testprojects = BuildContext.TestProjects;
        foreach (var testProject in testprojects)
        {
            Command.Execute("dotnet", "test " + testProject + " --configuration Release");
        }
    }

    /// <summary>
    /// Executes the tests with code coverage.
    /// </summary>
    /// <param name="pathToProjectFolder"></param>
    /// <param name="codeCoverageArtifactsFolder"></param>
    public static void TestWithCodeCoverage(string projectName, string pathToTestProjectFolder, string codeCoverageArtifactsFolder, int threshold = 100, string targetFramework = null)
    {
        if (!string.IsNullOrWhiteSpace(targetFramework))
        {
            targetFramework = $" -f {targetFramework} ";
        }
        //dotnet test -c release -f netcoreapp2.0 /p:CollectCoverage=true /p:Exclude="[xunit*]*%2c[*Tests*]*"
        Command.Execute("dotnet", $"test -c release {targetFramework}  /property:CollectCoverage=true /property:Exclude=\"[xunit*]*%2c[*Tests*]*\" /property:CoverletOutputFormat=\\\"opencover,lcov,json\\\" /property:CoverletOutput={codeCoverageArtifactsFolder}/coverage /property:Threshold={threshold}", pathToTestProjectFolder);
        var pathToOpenCoverResult = Path.Combine(codeCoverageArtifactsFolder, "coverage.opencover.xml");
        CodeCoverageReportGenerator.Generate(pathToOpenCoverResult, $"{codeCoverageArtifactsFolder}/Report");
    }

    /// <summary>
    /// Executes all test projects found in <see cref="BuildContext.TestProjects"/> with test coverage.
    /// </summary>
    public static void TestWithCodeCoverage()
    {
        var testprojects = BuildContext.TestProjects;
        foreach (var testProject in testprojects)
        {
            var targetFramework = GetTargetFrameWork(testProject);
            TestWithCodeCoverage(BuildContext.ProjectName, Path.GetDirectoryName(testProject), BuildContext.TestCoverageArtifactsFolder, BuildContext.CodeCoverageThreshold, targetFramework);
        }

        string GetTargetFrameWork(string pathToProjectFile)
        {
            var projectFile = XDocument.Load(pathToProjectFile);
            var targetFrameworks = projectFile.Descendants("TargetFrameworks").SingleOrDefault()?.Value;
            if (targetFrameworks != null)
            {
                return targetFrameworks.Split(";").First();
            }

            return null;
        }
    }

    public static void Pack()
    {
        foreach (var packableProject in BuildContext.PackableProjects)
        {
            Pack(Path.GetDirectoryName(packableProject), BuildContext.NuGetArtifactsFolder, BuildContext.CurrentShortCommitHash);
        }
    }


    public static void Pack(string pathToProjectFolder, string pathToPackageOutputFolder, string commitHash = "")
    {
        if (!string.IsNullOrWhiteSpace(commitHash))
        {
            commitHash = $" /property:CommitHash={commitHash} ";
        }
        string pathToProjectFile = FindProjectFile(pathToProjectFolder);
        Command.Execute("dotnet", $"pack {pathToProjectFile} --configuration Release --output {pathToPackageOutputFolder} {commitHash}");
    }

    public static void Build(string pathToProjectFolder)
    {
        string pathToProjectFile = FindProjectFile(pathToProjectFolder);
        Command.Execute("dotnet", "--version", pathToProjectFolder);
        Command.Execute("dotnet", $"restore {pathToProjectFile}", pathToProjectFolder);
        Command.Execute("dotnet", $"build {pathToProjectFile} --configuration Release", pathToProjectFolder);
    }

    public static void Publish(string pathToProjectFolder, string outputFolder = null, string targetFramework = null)
    {
        string pathToProjectFile = FindProjectFile(pathToProjectFolder);
        var args = $"publish {pathToProjectFile} --configuration Release";
        if (!string.IsNullOrWhiteSpace(outputFolder))
        {
            args = args + $" --output {outputFolder}";
        }

        if (!string.IsNullOrWhiteSpace(targetFramework))
        {
            args = args + $" -f {targetFramework}";
        }

        Command.Execute("dotnet", args);
    }

    private static string FindProjectFile(string pathToProjectFolder)
    {
        if (GetPathType(pathToProjectFolder) == PathType.File)
        {
            return pathToProjectFolder;
        }

        return Directory.GetFiles(pathToProjectFolder, "*.csproj").SingleOrDefault();
    }
}