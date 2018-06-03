#! "netcoreapp2.0"
#r "nuget: FluentAssertions, 4.19.4"
#load "../Dotnet.Build/Command.csx"
#load "../Dotnet.Build/DotNet.csx"
#load "../Dotnet.Build/xUnit.csx"

#load "nuget:ScriptUnit, 0.1.3"
#load "TestUtils.csx"


using FluentAssertions;
using static ScriptUnit;
using static FileUtils;

//await AddTestsFrom<DotNetTests>().AddFilter(m => m.IsDefined(typeof(OnlyThisAttribute), true)).Execute();

public class DotNetTests
{
    
    public void ShouldBuildProject()
    {
        using(var projectFolder = new DisposableFolder())
        {
            Command.Execute("dotnet",$"new console -o {projectFolder.Path}");
            DotNet.Build(projectFolder.Path);
        }
    }
   
    public void ShouldBuildProjectWithCommitHash()
    {
        using(var projectFolder = new DisposableFolder())
        {
            Command.Execute("dotnet",$"new console -o {projectFolder.Path}");
            DotNet.Build(projectFolder.Path,"123");
        }
    }
   
    public void ShouldExecuteTests()
    {
        using(var projectFolder = new DisposableFolder())
        {
            Command.Execute("dotnet",$"new xunit -o {projectFolder.Path}");
            DotNet.Test(projectFolder.Path);
        }
    }
    
    [OnlyThis]
    public void ShouldAnalyzeCodeCoverageWithxUnit()
    {
        using(var projectFolder = new DisposableFolder())
        {
            Command.Execute("dotnet",$"new xunit -o {projectFolder.Path} -n TestProject");
            ReplaceInFile("<TargetFramework.*", "<TargetFramework>net46</TargetFramework><DebugType>full</DebugType>",Path.Combine(projectFolder.Path,"TestProject.csproj"));
            DotNet.Build(projectFolder.Path);
            var testAssembly = FindFile(Path.Combine(projectFolder.Path,"bin"), "TestProject.dll");
            xUnit.AnalyzeCodeCoverage(testAssembly);
        }
    }

    
    public void ShouldExecuteScriptTests()
    {
        using(var projectFolder = new DisposableFolder())
        {
            var pathToScriptUnitTests = Path.Combine(projectFolder.Path,"ScriptUnitTests.csx");
            Copy(Path.Combine(GetScriptFolder(),"ScriptUnitTests.template"), pathToScriptUnitTests);
            DotNet.Test(pathToScriptUnitTests);
        }
    }

    public void ShouldPublishProject()
    {
        using(var projectFolder = new DisposableFolder())
        {
            Command.Execute("dotnet",$"new console -o {projectFolder.Path}");
            DotNet.Publish(projectFolder.Path);
        }
    }

   
    public void ShouldPublishToGivenFolder()
    {
        using(var projectFolder = new DisposableFolder())
        {
            using(var outputFolder = new DisposableFolder())
            {
                Command.Execute("dotnet",$"new console -o {projectFolder.Path}");
                DotNet.Publish(projectFolder.Path, outputFolder.Path);
                Directory.GetFiles(outputFolder.Path, "*.dll").Should().NotBeEmpty();
            }
        }
    }
}