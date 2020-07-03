#load "Command.csx"
#load "FileUtils.csx"
#load "CodeCoverageReportGenerator.csx"
#load "BuildContext.csx"

using System.Globalization;
using System.Xml.Linq;
using static FileUtils;

public static class DotNet
{
    private const string DefaultSource = "https://www.nuget.org/api/v2/package";

    private static string NuGetApiKey = System.Environment.GetEnvironmentVariable("NUGET_APIKEY");

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
    public static void TestWithCodeCoverage(string pathToTestProjectFolder, string codeCoverageArtifactsFolder, int threshold = 100, string targetFramework = null)
    {
        if (!string.IsNullOrWhiteSpace(targetFramework))
        {
            targetFramework = $" -f {targetFramework} ";
        }

        Command.Execute("dotnet", $"test -c release {targetFramework} --collect:\"XPlat Code Coverage\" --results-directory={codeCoverageArtifactsFolder} -- DataCollectionRunSettings.DataCollectors.DataCollector.Configuration.Format=lcov,cobertura", pathToTestProjectFolder);

        var pathToTempCoberturaResults = FileUtils.FindFile(codeCoverageArtifactsFolder, "coverage.cobertura.xml");
        var pathToTempLineCoverageResults = FileUtils.FindFile(codeCoverageArtifactsFolder, "coverage.info");
        var pathToFuckedUpTempFolder = Path.GetDirectoryName(pathToTempCoberturaResults);

        FileUtils.Copy(pathToTempCoberturaResults, codeCoverageArtifactsFolder);
        FileUtils.Copy(pathToTempLineCoverageResults, codeCoverageArtifactsFolder);
        FileUtils.RemoveDirectory(pathToFuckedUpTempFolder);

        var pathToCoberturaResults = Path.Combine(codeCoverageArtifactsFolder, "coverage.cobertura.xml");
        CheckCoberturaCoverage(pathToCoberturaResults, threshold);
        CodeCoverageReportGenerator.Generate(pathToCoberturaResults, $"{codeCoverageArtifactsFolder}/Report");
    }

    private static void CheckCoberturaCoverage(string reportFile, int threshold)
    {
        var thresholdNormalized = 100.0;
        var coverageXml = XDocument.Load(reportFile).Descendants("coverage");
        var lineRate = double.Parse(coverageXml.Attributes("line-rate").FirstOrDefault().Value, CultureInfo.InvariantCulture) * 100;
        var branchRate = double.Parse(coverageXml.Attributes("branch-rate").FirstOrDefault().Value, CultureInfo.InvariantCulture) * 100;

        if (lineRate < thresholdNormalized)
        {
            throw new InvalidOperationException($"Line coverage < {thresholdNormalized} ({lineRate})");
        }

        if (branchRate < thresholdNormalized)
        {
            throw new InvalidOperationException($"Branch coverage < {thresholdNormalized} ({branchRate})");
        }

        Console.WriteLine($"Coverage OK (>= {thresholdNormalized})");
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
            TestWithCodeCoverage(Path.GetDirectoryName(testProject), BuildContext.TestCoverageArtifactsFolder, BuildContext.CodeCoverageThreshold, targetFramework);
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

    public static void Push(string packagesFolder, string source = DefaultSource)
    {
        var packageFiles = Directory.GetFiles(packagesFolder, "*.nupkg");
        foreach (var packageFile in packageFiles)
        {
            Command.Execute("dotnet", $"nuget push {packageFile} --source {source} --api-key {NuGetApiKey}");
        }
    }

    public static void Push()
    {
        Push(BuildContext.NuGetArtifactsFolder);
    }


    public static void TryPush(string packagesFolder, string source = DefaultSource)
    {
        var packageFiles = Directory.GetFiles(packagesFolder, "*.nupkg");
        foreach (var packageFile in packageFiles)
        {
            Command.Capture("dotnet", $"nuget push {packageFile} --source {source} --api-key {NuGetApiKey}").Dump();
        }
    }

    public static void TryPush()
    {
        TryPush(BuildContext.NuGetArtifactsFolder);
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