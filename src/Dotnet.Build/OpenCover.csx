#load "NuGet.csx"
#load "FileUtils.csx"
using System.Text.RegularExpressions;
using static FileUtils;

public static class OpenCover
{
    private static string pathToOpenCover;
    private static string pathToReportGenerator;

    public static void Execute(string pathToTestRunner, string testRunnerArgs, string pathToCoverageFile, string filter = "+[*]*")
    {
        using (var packagesFolder = new DisposableFolder())
        {
            NuGetHelper.Install("OpenCover", packagesFolder.Path);
            NuGetHelper.Install("ReportGenerator", packagesFolder.Path);
            pathToOpenCover = FileUtils.FindFile(packagesFolder.Path, "OpenCover.Console.exe");
            pathToReportGenerator = FileUtils.FindFile(packagesFolder.Path, "ReportGenerator.exe");
            var args = $"-target:\"{pathToTestRunner}\" -targetargs:\"{testRunnerArgs}\" -output:\"{pathToCoverageFile}\" -filter:\"{filter}\" -register:user -excludebyattribute:*.ExcludeFromCodeCoverage*";
            Command.Execute(pathToOpenCover, args, ".");
            CreateSummaryFile(pathToCoverageFile);
        }
    }

    private static void CreateSummaryFile(string pathToCoverageFile)
    {
        var targetDirectory = Path.GetDirectoryName(pathToCoverageFile);
        var args = $"-reports:\"{pathToCoverageFile}\" -targetdir:\"{targetDirectory}\" -reporttypes:xmlsummary";
        Command.Execute(pathToReportGenerator, args);
        var pathToSummaryFile = Path.Combine(targetDirectory, "summary.xml");
        ValidateCodeCoverage(pathToSummaryFile);
    }

    private static void ValidateCodeCoverage(string pathToSummaryFile)
    {

        var summaryContent = FileUtils.ReadFile(pathToSummaryFile);
        var coverage = Regex.Match(summaryContent, "LineCoverage>(.*)<", RegexOptions.IgnoreCase).Groups[1].Captures[0].Value;

        WriteLine("Code coverage is {0}", coverage);

        if (coverage != "100%")
        {
            MatchCollection matchesRepresentingClassesWithInsufficientCoverage = Regex.Matches(summaryContent, @"Class name=""(.*?)"" coverage=""(\d{1,2}|\d+\.\d+)""");
            foreach (Match match in matchesRepresentingClassesWithInsufficientCoverage)
            {
                var className = match.Groups[1].Captures[0].Value.Replace("&lt;", "<").Replace("&gt;", ">");
                var classCoverage = match.Groups[2].Captures[0].Value;
                WriteLine("Class name: {0} has only {1}% coverage", className, classCoverage);
            }

            throw new InvalidOperationException("Deploy failed. Test coverage is only " + coverage);
        }


    }
}