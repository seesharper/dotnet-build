#r "nuget: FluentAssertions, 5.6.0"
#load "../Dotnet.Build/Command.csx"
#load "../Dotnet.Build/NuGet.csx"
#load "../Dotnet.Build/DotNet.csx"
#load "../Dotnet.Build/FileUtils.csx"

#load "nuget:ScriptUnit, 0.1.3"
#load "TestUtils.csx"

using static ScriptUnit;
using static FileUtils;
using FluentAssertions;


return await AddTestsFrom<NuGetTests>().Execute();
//return await AddTestsFrom<NuGetTests>().AddFilter(m => m.IsDefined(typeof(OnlyThisAttribute), true)).Execute();
public class NuGetTests
{
    public void ShouldCreateToolPackage()
    {
        using (var projectFolder = new DisposableFolder())
        {
            Command.Execute("dotnet", $"new console -o {projectFolder.Path}");
            var outputFolder = Path.Combine(projectFolder.Path, "bin");
            DotNet.Publish(projectFolder.Path, outputFolder);
            NuGetHelper.PackAsTool(projectFolder.Path, outputFolder, projectFolder.Path);
        }
    }

    [OnlyThis]
    public void ShoudInstallPackage()
    {
        using (var targetFolder = new DisposableFolder())
        {
            NuGetHelper.Install("LightInject", targetFolder.Path);
            FindFile(targetFolder.Path, "LightInject.dll").Should().NotBeEmpty();
        }
    }

}