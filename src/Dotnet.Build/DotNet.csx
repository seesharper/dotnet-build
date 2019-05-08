#load "Command.csx"
#load "FileUtils.csx"

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

        Command.Execute("dotnet", $"test -c release {targetFramework}  /property:CollectCoverage=true /property:Include=\"[{projectName}*]*\" /property:Exclude=\"[*Tests*]*\" /property:CoverletOutputFormat=\\\"opencover,lcov,json\\\" /property:CoverletOutput={codeCoverageArtifactsFolder}/coverage /property:Threshold={threshold}", pathToTestProjectFolder);
        var pathToOpenCoverResult = Path.Combine(codeCoverageArtifactsFolder, "coverage.opencover.xml");
        Command.Execute("dotnet", $"reportgenerator \"-reports:{pathToOpenCoverResult}\"  \"-targetdir:{codeCoverageArtifactsFolder}/Report\" \"-reportTypes:XmlSummary;Xml;HtmlInline_AzurePipelines_Dark\" \"--verbosity:warning\"", pathToTestProjectFolder);
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