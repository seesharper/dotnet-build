#r "nuget: CliWrap, 3.10.0"
#load "Command.csx"
#load "FileUtils.csx"
#load "CodeCoverageReportGenerator.csx"
#load "BuildContext.csx"
#load "Vulnerabilities.csx"
#load "Outdated.csx"
using System.Globalization;
using System.Threading;
using System.Xml.Linq;
using Newtonsoft.Json;
using static FileUtils;


public static class DotNet
{
    private const string DefaultSource = "https://www.nuget.org/api/v2/package";

    private static string NuGetApiKey = System.Environment.GetEnvironmentVariable("NUGET_APIKEY");

    public static async Task CheckPackageVulnerabilities(string pathToProject, Func<VulnerabilityReport, bool> success = null, string source = "https://api.nuget.org/v3/index.json")
    {
        success ??= report => report.Projects.All(p => p.Frameworks.All(f => f.TopLevelPackages.All(t => !t.Vulnerabilities.Any()))
            && p.Frameworks.All(f => f.TransitivePackages.All(t => !t.Vulnerabilities.Any())));
        await Command.ExecuteAsync("dotnet", $"restore {pathToProject}", BuildContext.RepositoryFolder);
        var result = await Command.CaptureAsync("dotnet", $"list {pathToProject} package --vulnerable --include-transitive --format json --source {source}", BuildContext.RepositoryFolder);
        var report = JsonConvert.DeserializeObject<VulnerabilityReport>(result.StandardOut);

        if (!success(report))
        {
            throw new InvalidOperationException("Package vulnerabilities found" + Environment.NewLine + result.StandardOut);
        }
    }


    public static async Task CheckPackageVulnerabilities(Func<VulnerabilityReport, bool> success = null, string source = "https://api.nuget.org/v3/index.json", bool includeTestProjects = false)
    {
        foreach (var project in BuildContext.SourceProjects)
        {
            await CheckPackageVulnerabilities(project, success, source);
        }
        if (includeTestProjects)
        {
            foreach (var project in BuildContext.TestProjects)
            {
                await CheckPackageVulnerabilities(project, success, source);
            }
        }
    }

    public static async Task CheckPackageVersions(Func<DependencyReport, bool> success = null, string source = "https://api.nuget.org/v3/index.json")
    {
        success ??= report => report.Projects.All(p => p.Frameworks.All(f => f.TopLevelPackages.All(t => t.ResolvedVersion == t.LatestVersion)));
        await Command.ExecuteAsync("dotnet", $"restore {BuildContext.RepositoryFolder}", BuildContext.RepositoryFolder);
        var result = await Command.CaptureAsync("dotnet", "list package --outdated --format json", BuildContext.RepositoryFolder);
        var report = JsonConvert.DeserializeObject<DependencyReport>(result.StandardOut);

        if (!success(report))
        {
            throw new InvalidOperationException("Package versions outdated" + Environment.NewLine + result.StandardOut);
        }
    }


    /// <summary>
    /// Executes the tests in the given path. The path may the full path to a csproj file
    /// that represents a test project or it may be the path to a csx file that contains tests
    /// </summary>
    /// <param name="path"></param>
    public static void Test(string path)
    {
        string pathToProjectFile = FindProjectFile(path);
        if (pathToProjectFile.EndsWith("csproj", StringComparison.InvariantCultureIgnoreCase))
        {
            var stdErrBuffer = new StringBuilder();
            var result = CliWrap.Cli.Wrap("dotnet").WithArguments($"test {pathToProjectFile} --configuration Release")
                .WithStandardErrorPipe(CliWrap.PipeTarget.ToStringBuilder(stdErrBuffer))
                .WithStandardOutputPipe(CliWrap.PipeTarget.ToStream(Console.OpenStandardOutput()))
                .WithValidation(CliWrap.CommandResultValidation.None)
                .ExecuteAsync().GetAwaiter().GetResult();
            if (result.ExitCode != 0)
            {
                Console.WriteLine(stdErrBuffer.ToString());
                throw new InvalidOperationException($"The command dotnet test {BuildContext.RepositoryFolder} --configuration Release failed. {stdErrBuffer}");
            }
            return;
        }

        if (pathToProjectFile.EndsWith("csx", StringComparison.InvariantCultureIgnoreCase))
        {
            Command.Execute("dotnet", $"script {path}");
            return;
        }

        throw new InvalidOperationException($"No tests found at the path {path}");
    }

    public static async Task TestAsync(string path, int timeoutInSeconds = 1800)
    {
        var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(timeoutInSeconds));
        string pathToProjectFile = FindProjectFile(path);
        if (pathToProjectFile.EndsWith("csproj", StringComparison.InvariantCultureIgnoreCase))
        {
            var stdErrBuffer = new StringBuilder();
            try
            {
                var result = await CliWrap.Cli.Wrap("dotnet").WithArguments($"test {pathToProjectFile} --configuration Release")
                .WithStandardErrorPipe(CliWrap.PipeTarget.ToStringBuilder(stdErrBuffer))
                .WithStandardOutputPipe(CliWrap.PipeTarget.ToStream(Console.OpenStandardOutput()))
                .WithValidation(CliWrap.CommandResultValidation.None)
                .ExecuteAsync(cancellationTokenSource.Token);
                if (result.ExitCode != 0)
                {
                    Console.WriteLine(stdErrBuffer.ToString());
                    throw new InvalidOperationException($"The command dotnet test {BuildContext.RepositoryFolder} --configuration Release failed. {stdErrBuffer}");
                }
                return;
            }
            catch (OperationCanceledException)
            {
                throw new TimeoutException($"The command dotnet test {BuildContext.RepositoryFolder} --configuration Release timed out after {timeoutInSeconds} seconds.");
            }

        }

        if (pathToProjectFile.EndsWith("csx", StringComparison.InvariantCultureIgnoreCase))
        {
            await Command.ExecuteAsync("dotnet", $"script {path}");
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
            Test(testProject);
        }
    }

    /// <summary>
    /// Executes all test projects found in <see cref="BuildContext.TestProjects"/>.
    /// </summary>
    public static async Task TestAsync(int timeoutInSeconds = 1800)
    {
        var testprojects = BuildContext.TestProjects;
        foreach (var testProject in testprojects)
        {
            await TestAsync(testProject, timeoutInSeconds);
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

        var settingsFile = FindFile(pathToTestProjectFolder, "coverlet.runsettings");
        if (settingsFile == null)
        {
            Command.Execute("dotnet", $"test -c release {targetFramework} --collect:\"XPlat Code Coverage\" --results-directory={codeCoverageArtifactsFolder} -- DataCollectionRunSettings.DataCollectors.DataCollector.Configuration.ExcludeByAttribute=GeneratedCodeAttribute -- DataCollectionRunSettings.DataCollectors.DataCollector.Configuration.Format=lcov,cobertura", pathToTestProjectFolder);
        }
        else
        {
            Error.WriteLine($"Found runsettings file at {settingsFile}");
            Command.Execute("dotnet", $"test -c release {targetFramework} --collect:\"XPlat Code Coverage\" --results-directory={codeCoverageArtifactsFolder} --settings {settingsFile}", pathToTestProjectFolder);
        }



        var pathToTempCoberturaResults = FileUtils.FindFile(codeCoverageArtifactsFolder, "coverage.cobertura.xml");
        var pathToTempLineCoverageResults = FileUtils.FindFile(codeCoverageArtifactsFolder, "coverage.info");
        var pathToFuckedUpTempFolder = Path.GetDirectoryName(pathToTempCoberturaResults);

        FileUtils.Copy(pathToTempCoberturaResults, codeCoverageArtifactsFolder);
        FileUtils.Copy(pathToTempLineCoverageResults, codeCoverageArtifactsFolder);
        FileUtils.RemoveDirectory(pathToFuckedUpTempFolder);

        var pathToCoberturaResults = Path.Combine(codeCoverageArtifactsFolder, "coverage.cobertura.xml");
        CodeCoverageReportGenerator.Generate(pathToCoberturaResults, Path.Combine(codeCoverageArtifactsFolder, "Report"));
        CheckCoberturaCoverage(pathToCoberturaResults, threshold);
    }

    /// <summary>
    /// Executes the tests with code coverage.
    /// </summary>
    /// <param name="pathToProjectFolder"></param>
    /// <param name="codeCoverageArtifactsFolder"></param>
    public static async Task TestWithCodeCoverageAsync(string pathToTestProjectFolder, string codeCoverageArtifactsFolder, int threshold = 100, string targetFramework = null, int timeoutInSeconds = 1800)
    {
        var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(timeoutInSeconds));

        if (!string.IsNullOrWhiteSpace(targetFramework))
        {
            targetFramework = $" -f {targetFramework} ";
        }

        var settingsFile = FindFile(pathToTestProjectFolder, "coverlet.runsettings");
        var stdErrBuffer = new StringBuilder();

        try
        {
            if (settingsFile == null)
            {
                var args = $"test -c release {targetFramework} --collect:\"XPlat Code Coverage\" --results-directory={codeCoverageArtifactsFolder} -- DataCollectionRunSettings.DataCollectors.DataCollector.Configuration.ExcludeByAttribute=GeneratedCodeAttribute -- DataCollectionRunSettings.DataCollectors.DataCollector.Configuration.Format=lcov,cobertura";
                var result = await CliWrap.Cli.Wrap("dotnet").WithArguments(args)
                    .WithWorkingDirectory(pathToTestProjectFolder)
                    .WithStandardErrorPipe(CliWrap.PipeTarget.ToStringBuilder(stdErrBuffer))
                    .WithStandardOutputPipe(CliWrap.PipeTarget.ToStream(Console.OpenStandardOutput()))
                    .WithValidation(CliWrap.CommandResultValidation.None)
                    .ExecuteAsync(cancellationTokenSource.Token);
                if (result.ExitCode != 0)
                {
                    Console.WriteLine(stdErrBuffer.ToString());
                    throw new InvalidOperationException($"The command dotnet test failed. {stdErrBuffer}");
                }
            }
            else
            {
                Error.WriteLine($"Found runsettings file at {settingsFile}");
                var args = $"test -c release {targetFramework} --collect:\"XPlat Code Coverage\" --results-directory={codeCoverageArtifactsFolder} --settings {settingsFile}";
                var result = await CliWrap.Cli.Wrap("dotnet").WithArguments(args)
                    .WithWorkingDirectory(pathToTestProjectFolder)
                    .WithStandardErrorPipe(CliWrap.PipeTarget.ToStringBuilder(stdErrBuffer))
                    .WithStandardOutputPipe(CliWrap.PipeTarget.ToStream(Console.OpenStandardOutput()))
                    .WithValidation(CliWrap.CommandResultValidation.None)
                    .ExecuteAsync(cancellationTokenSource.Token);
                if (result.ExitCode != 0)
                {
                    Console.WriteLine(stdErrBuffer.ToString());
                    throw new InvalidOperationException($"The command dotnet test failed. {stdErrBuffer}");
                }
            }
        }
        catch (OperationCanceledException)
        {
            throw new TimeoutException($"The command dotnet test timed out after {timeoutInSeconds} seconds.");
        }

        var pathToTempCoberturaResults = FileUtils.FindFile(codeCoverageArtifactsFolder, "coverage.cobertura.xml");
        var pathToTempLineCoverageResults = FileUtils.FindFile(codeCoverageArtifactsFolder, "coverage.info");
        var pathToFuckedUpTempFolder = Path.GetDirectoryName(pathToTempCoberturaResults);

        FileUtils.Copy(pathToTempCoberturaResults, codeCoverageArtifactsFolder);
        FileUtils.Copy(pathToTempLineCoverageResults, codeCoverageArtifactsFolder);
        FileUtils.RemoveDirectory(pathToFuckedUpTempFolder);

        var pathToCoberturaResults = Path.Combine(codeCoverageArtifactsFolder, "coverage.cobertura.xml");
        CodeCoverageReportGenerator.Generate(pathToCoberturaResults, Path.Combine(codeCoverageArtifactsFolder, "Report"));
        CheckCoberturaCoverage(pathToCoberturaResults, threshold);
    }


    private static void CheckCoberturaCoverage(string reportFile, int threshold)
    {
        var thresholdNormalized = threshold;
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

        Console.WriteLine($"Coverage OK (>= {thresholdNormalized}) Line({lineRate}) Branch ({branchRate})");
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

    public static void TestSolutionsWithCodeCoverage()
    {
        var solutions = BuildContext.Solutions;
        foreach (var solution in solutions)
        {
            TestSolutionWithCodeCoverage(solution, BuildContext.ArtifactsFolder, BuildContext.CodeCoverageThreshold);
        }
    }

    public static void TestSolutionWithCodeCoverage(string solution, string codeCoverageArtifactsFolder, int threshold)
    {
        var settingsFile = FindFile(Path.GetDirectoryName(solution), "coverlet.runsettings");
        if (settingsFile == null)
        {
            Command.Execute("dotnet", $"test {solution} -c release --collect:\"XPlat Code Coverage\" --results-directory={codeCoverageArtifactsFolder} -- DataCollectionRunSettings.DataCollectors.DataCollector.Configuration.ExcludeByAttribute=GeneratedCodeAttribute -- DataCollectionRunSettings.DataCollectors.DataCollector.Configuration.Format=lcov,cobertura", BuildContext.RepositoryFolder);
        }
        else
        {
            Error.WriteLine($"Found runsettings file at {settingsFile}");
            Command.Execute("dotnet", $"test {solution} -c release --collect:\"XPlat Code Coverage\" --results-directory={codeCoverageArtifactsFolder} --settings {settingsFile}", BuildContext.RepositoryFolder);
        }



        var options = new EnumerationOptions();
        options.MatchCasing = MatchCasing.CaseInsensitive;
        options.RecurseSubdirectories = true;
        var coberturaResultPaths = Directory.GetFiles(codeCoverageArtifactsFolder, "coverage.cobertura.xml", options);
        var coberturaResults = string.Join(';', coberturaResultPaths);
        var reportsPath = Path.Combine(codeCoverageArtifactsFolder, "Report");

        CodeCoverageReportGenerator.Generate(coberturaResults, reportsPath);
        CheckCoberturaCoverage(Path.Combine(reportsPath, "Cobertura.xml"), threshold);
        foreach (var result in coberturaResultPaths)
        {
            RemoveDirectory(Path.GetDirectoryName(result));
        }
    }

    public static async Task TestSolutionWithCodeCoverageAsync(string solution, string codeCoverageArtifactsFolder, int threshold, int timeoutInSeconds = 1800)
    {
        var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(timeoutInSeconds));

        var settingsFile = FindFile(Path.GetDirectoryName(solution), "coverlet.runsettings");
        var stdErrBuffer = new StringBuilder();

        try
        {
            if (settingsFile == null)
            {
                var args = $"test {solution} -c release --collect:\"XPlat Code Coverage\" --results-directory={codeCoverageArtifactsFolder} -- DataCollectionRunSettings.DataCollectors.DataCollector.Configuration.ExcludeByAttribute=GeneratedCodeAttribute -- DataCollectionRunSettings.DataCollectors.DataCollector.Configuration.Format=lcov,cobertura";
                var result = await CliWrap.Cli.Wrap("dotnet").WithArguments(args)
                    .WithWorkingDirectory(Path.GetDirectoryName(solution))
                    .WithStandardErrorPipe(CliWrap.PipeTarget.ToStringBuilder(stdErrBuffer))
                    .WithStandardOutputPipe(CliWrap.PipeTarget.ToStringBuilder(stdErrBuffer))
                    .WithValidation(CliWrap.CommandResultValidation.ZeroExitCode)
                    .ExecuteAsync(cancellationTokenSource.Token);
                if (result.ExitCode != 0)
                {
                    var error = stdErrBuffer.ToString();
                    Console.WriteLine(stdErrBuffer.ToString());
                    throw new InvalidOperationException($"The command dotnet test failed. {stdErrBuffer}");
                }
            }
            else
            {
                Error.WriteLine($"Found runsettings file at {settingsFile}");
                var args = $"test {solution} -c release --collect:\"XPlat Code Coverage\" --results-directory={codeCoverageArtifactsFolder} --settings {settingsFile}";
                var result = await CliWrap.Cli.Wrap("dotnet").WithArguments(args)
                    .WithWorkingDirectory(BuildContext.RepositoryFolder)
                    .WithStandardErrorPipe(CliWrap.PipeTarget.ToStringBuilder(stdErrBuffer))
                    .WithStandardOutputPipe(CliWrap.PipeTarget.ToStream(Console.OpenStandardOutput()))
                    .WithValidation(CliWrap.CommandResultValidation.None)
                    .ExecuteAsync(cancellationTokenSource.Token);
                if (result.ExitCode != 0)
                {
                    Console.WriteLine(stdErrBuffer.ToString());
                    throw new InvalidOperationException($"The command dotnet test failed. {stdErrBuffer}");
                }
            }
        }
        catch (OperationCanceledException)
        {
            throw new TimeoutException($"The command dotnet test timed out after {timeoutInSeconds} seconds.");
        }

        var options = new EnumerationOptions();
        options.MatchCasing = MatchCasing.CaseInsensitive;
        options.RecurseSubdirectories = true;
        var coberturaResultPaths = Directory.GetFiles(codeCoverageArtifactsFolder, "coverage.cobertura.xml", options);
        var coberturaResults = string.Join(';', coberturaResultPaths);
        var reportsPath = Path.Combine(codeCoverageArtifactsFolder, "Report");

        CodeCoverageReportGenerator.Generate(coberturaResults, reportsPath);
        CheckCoberturaCoverage(Path.Combine(reportsPath, "Cobertura.xml"), threshold);
        foreach (var result in coberturaResultPaths)
        {
            RemoveDirectory(Path.GetDirectoryName(result));
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

        var document = XDocument.Load(pathToProjectFile);
        var version = document.Descendants("Version").SingleOrDefault()?.Value;
        if (string.IsNullOrWhiteSpace(version))
        {
            version = BuildContext.LatestTag;
        }

        var versionProperty = $" /property:Version={version.Replace("v", string.Empty, StringComparison.OrdinalIgnoreCase)} ";


        Command.Execute("dotnet", $"pack {pathToProjectFile} --configuration Release --output {pathToPackageOutputFolder} {commitHash} {versionProperty}");
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

    public static void Publish()
    {
        var publishableProjects = BuildContext.PublishableProjects;
        foreach (var publishableProject in publishableProjects)
        {
            Command.Execute("dotnet", $"publish {publishableProject} -c release -o {BuildContext.GitHubArtifactsFolder}");
        }
        ;
    }
}



private static string FindProjectFile(string pathToProjectFolder)
{
    if (GetPathType(pathToProjectFolder) == PathType.File)
    {
        return pathToProjectFolder;
    }

    return Directory.GetFiles(pathToProjectFolder, "*.csproj").SingleOrDefault();
}
