#load "NuGet.csx"
#load "FileUtils.csx"
#load "OpenCover.csx"



using static FileUtils;

public static class xUnit
{
    public static void AnalyzeCodeCoverage(string pathToTestAssembly, string filter = null)
    {
        using (var packagesFolder = new DisposableFolder())
        {
            NuGetHelper.Install("xunit.runner.console", packagesFolder.Path);
            var pathToTestRunner = FileUtils.FindFile(packagesFolder.Path, "xunit.console.exe");
            string pathToCoverageFile = Path.Combine(Path.GetDirectoryName(pathToTestAssembly), "coverage.xml");
            var testRunnerArgs = $"{pathToTestAssembly} -noshadow -notrait \"Category=Verification\"";
            Command.Execute(pathToTestRunner, testRunnerArgs, ".");
            OpenCover.Execute(pathToTestRunner, testRunnerArgs, pathToCoverageFile, filter);
        }
    }
}