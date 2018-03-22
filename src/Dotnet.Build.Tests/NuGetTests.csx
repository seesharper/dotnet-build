#! "netcoreapp2.0"
#r "nuget: FluentAssertions, 4.19.4"
#load "../Dotnet.Build/Command.csx"
#load "../Dotnet.Build/NuGet.csx"
#load "../Dotnet.Build/DotNet.csx"
#load "../Dotnet.Build/FileUtils.csx"

#load "nuget:ScriptUnit, 0.1.3"
#load "TestUtils.csx"

using static ScriptUnit;   
using static FileUtils;
using FluentAssertions;


//return await AddTestsFrom<NuGetTests>().Execute();

public class NuGetTests
{    
    public void ShouldCreateToolPackage()
    {
        using(var projectFolder = new DisposableFolder())
        {
            Command.Execute("dotnet",$"new console -o {projectFolder.Path}");
            var outputFolder = Path.Combine(projectFolder.Path, "bin");
            DotNet.Publish(projectFolder.Path, outputFolder);
            NuGet.PackAsTool(projectFolder.Path,outputFolder,projectFolder.Path);            
        }
    }
  
  	
}