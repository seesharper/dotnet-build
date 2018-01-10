#! "netcoreapp2.0"
#r "nuget: FluentAssertions, 4.19.4"
#load "../Dotnet.Build/Command.csx"
#load "../Dotnet.Build/DotNet.csx"
#load "nuget:ScriptUnit, 0.1.1"
#load "TestUtils.csx"


using static ScriptUnit;

await AddTestsFrom<DotNetTests>().AddFilter(m => m.IsDefined(typeof(OnlyThisAttribute), true)).Execute();

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
  
    public void ShouldExecuteTests()
    {
        using(var projectFolder = new DisposableFolder())
        {
            Command.Execute("dotnet",$"new xunit -o {projectFolder.Path}");
            DotNet.Test(projectFolder.Path);
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
}