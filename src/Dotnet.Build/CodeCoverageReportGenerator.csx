#r "nuget:ReportGenerator.Core, 4.3.0"
using Palmmedia.ReportGenerator.Core;

public static class CodeCoverageReportGenerator
{
    /// <summary>
    /// Generates a code coverage report based on a coverage file in the OpenCover file format"
    /// </summary>
    /// <param name="pathToOpenCoverResult">The path to the coverage file.</param>
    /// <param name="codeCoverageArtifactsFolder">The output folder to place the report.</param>
    public static void Generate(string pathToOpenCoverResult, string codeCoverageArtifactsFolder)
    {
        var cliArguments = new Dictionary<string, string>();
        cliArguments.Add("reports", $"{pathToOpenCoverResult}");
        cliArguments.Add("targetdir", $"{codeCoverageArtifactsFolder}");
        cliArguments.Add("reportTypes", "XmlSummary;Xml;HtmlInline_AzurePipelines_Dark");

        var generator = new Generator();
        var settings = new Settings();
        var riskHotspotsAnalysisThresholds = new Palmmedia.ReportGenerator.Core.CodeAnalysis.RiskHotspotsAnalysisThresholds();
        var configuration = new ReportConfigurationBuilder().Create(cliArguments);
        generator.GenerateReport(configuration, settings, riskHotspotsAnalysisThresholds);
    }
}
