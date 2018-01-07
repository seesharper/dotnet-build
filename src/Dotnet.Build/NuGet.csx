#load "Command.csx"

public static class NuGet
{
    private const string DefaultSource = "https://www.nuget.org/api/v2/package";
    
    private static string ApiKey = System.Environment.GetEnvironmentVariable("NUGET_APIKEY");
           
    public static void Push(string packagesFolder, string source = DefaultSource)
    {
        var packageFiles = Directory.GetFiles(packagesFolder, "*.nupkg");        
        foreach(var packageFile in packageFiles)
        {            
            Command.Execute("nuget", $"push {packageFile} -Source {source} -ApiKey {ApiKey}").Dump();
        }
    }

    public static void Pack(string pathToMetadataFolder, string outputFolder)
    {
        var spec = Directory.GetFiles(pathToMetadataFolder,"*.nuspec").Single();
        Command.Execute("nuget",$"pack {spec} -OutputDirectory {outputFolder}").EnsureSuccessfulExitCode().Dump();
    }
}