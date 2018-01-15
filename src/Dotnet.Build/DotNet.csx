#load "Command.csx"

public static class DotNet
{
    public static void Test(string pathToProjectFolder)
    {
        string pathToTestProject = FindProjectFile(pathToProjectFolder);
        Command.Execute("dotnet","test " + pathToTestProject + " --configuration Release").EnsureSuccessfulExitCode().Dump();   
    }
    
    public static void Pack(string pathToProjectFolder, string pathToPackageOutputFolder)
    {
        string pathToProjectFile = FindProjectFile(pathToProjectFolder);
        Command.Execute("dotnet",$"pack {pathToProjectFile} --configuration Release --output {pathToPackageOutputFolder} ").EnsureSuccessfulExitCode().Dump();   
    }
    
    public static void Build(string pathToProjectFolder)
    {
        string pathToProjectFile = FindProjectFile(pathToProjectFolder);
        Command.Execute("dotnet","--version").EnsureSuccessfulExitCode().Dump();
        Command.Execute("dotnet","restore " + pathToProjectFile).EnsureSuccessfulExitCode().Dump();;        
        Command.Execute("dotnet","build " + pathToProjectFile + " --configuration Release").EnsureSuccessfulExitCode().Dump();   
    }

    public static void Publish(string pathToProjectFolder)
    {
         string pathToProjectFile = FindProjectFile(pathToProjectFolder);
         Command.Execute("dotnet","publish " + pathToProjectFile + " --configuration Release").EnsureSuccessfulExitCode().Dump(); 
    }


    private static string FindProjectFile(string pathToProjectFolder)
    {
        return Directory.GetFiles(pathToProjectFolder, "*.csproj").Single();
    }
}