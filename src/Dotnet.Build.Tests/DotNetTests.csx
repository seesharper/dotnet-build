#r "nuget: FluentAssertions, 5.6.0"
#load "../Dotnet.Build/Command.csx"
#load "../Dotnet.Build/DotNet.csx"
#load "../Dotnet.Build/xUnit.csx"

#load "nuget:ScriptUnit, 0.1.3"
#load "TestUtils.csx"


using FluentAssertions;
using static ScriptUnit;
using static FileUtils;
using System.Xml.Linq;

//await AddTestsFrom<DotNetTests>().AddFilter(m => m.IsDefined(typeof(OnlyThisAttribute), true)).Execute();

public class DotNetTests
{

    public void ShouldBuildProject()
    {
        using (var projectFolder = new DisposableFolder())
        {
            Command.Execute("dotnet", $"new console -o {projectFolder.Path}");
            DotNet.Build(projectFolder.Path);
        }
    }


    public void ShouldPackProjectWithCommitHash()
    {
        using (var projectFolder = new DisposableFolder())
        {
            Command.Execute("dotnet", $"new console -o {projectFolder.Path}");
            DotNet.Pack(projectFolder.Path, "123");
        }
    }

    public void ShouldExecuteTests()
    {
        using (var projectFolder = new DisposableFolder())
        {
            Command.Execute("dotnet", $"new xunit -o {projectFolder.Path}");
            DotNet.Test(projectFolder.Path);
        }
    }


    public void ShouldAnalyzeCodeCoverageWithxUnit()
    {
        using (var projectFolder = new DisposableFolder())
        {
            Command.Execute("dotnet", $"new xunit -o {projectFolder.Path} -n TestProject");
            ReplaceInFile("<TargetFramework.*", "<TargetFramework>net46</TargetFramework><DebugType>full</DebugType>", Path.Combine(projectFolder.Path, "TestProject.csproj"));
            DotNet.Build(projectFolder.Path);
            var testAssembly = FindFile(Path.Combine(projectFolder.Path, "bin"), "TestProject.dll");
            xUnit.AnalyzeCodeCoverage(testAssembly);
        }
    }


    public void ShouldAnalyzeCodeCoverageUsingCoverletAndReportGenerator()
    {
        using (var solutionFolder = new DisposableFolder())
        {
            var artifactsFolder = CreateDirectory(solutionFolder.Path, "Artifacts");
            var projectFolder = CreateDirectory(solutionFolder.Path, "SampleProject");
            Command.Execute("dotnet", $"new xunit", projectFolder);
            Command.Execute("dotnet", "add package coverlet.collector", projectFolder);
            var csproj = FindFile(projectFolder, "*.csproj");
            var projectFile = XDocument.Load(FindFile(projectFolder, "*.csproj"));
            projectFile.Save(csproj);

            DotNet.TestWithCodeCoverage(projectFolder, artifactsFolder, 100);
        }
    }


    public void ShouldExecuteScriptTests()
    {
        using (var projectFolder = new DisposableFolder())
        {
            var pathToScriptUnitTests = Path.Combine(projectFolder.Path, "ScriptUnitTests.csx");
            Copy(Path.Combine(GetScriptFolder(), "ScriptUnitTests.template"), pathToScriptUnitTests);
            DotNet.Test(pathToScriptUnitTests);
        }
    }

    public void ShouldPublishProject()
    {
        using (var projectFolder = new DisposableFolder())
        {
            Command.Execute("dotnet", $"new console -o {projectFolder.Path}");
            DotNet.Publish(projectFolder.Path);
        }
    }


    public void ShouldPublishToGivenFolder()
    {
        using (var projectFolder = new DisposableFolder())
        {
            using (var outputFolder = new DisposableFolder())
            {
                Command.Execute("dotnet", $"new console -o {projectFolder.Path}");
                DotNet.Publish(projectFolder.Path, outputFolder.Path);
                Directory.GetFiles(outputFolder.Path, "*.dll").Should().NotBeEmpty();
            }
        }
    }

    [OnlyThis]
    public void ShouldPublishWhenProjectIsPublishable()
    {
        using (var solutionFolder = new DisposableFolder())
        {
            var srcFolder = CreateDirectory(solutionFolder.Path, "src");
            var buildFolder = CreateDirectory(solutionFolder.Path, "build");
            var oldRepoFolder = BuildContext.RepositoryFolder;
            BuildContext.RepositoryFolder = solutionFolder.Path;
            using (var outputFolder = new DisposableFolder())
            {
                Command.Execute("dotnet", $"new console -o {srcFolder}");
                var projectFile = FindFile(srcFolder, "*.csproj");
                var document = XDocument.Load(projectFile);
                var propertyGroupElement = document.Descendants("PropertyGroup").Single();
                var isPublishableElement = new XElement("IsPublishable", true);
                propertyGroupElement.Add(isPublishableElement);
                document.Save(projectFile);
                DotNet.Publish();

                Directory.GetFiles(BuildContext.GitHubArtifactsFolder, "*.dll").Should().NotBeEmpty();
            }

            BuildContext.RepositoryFolder = oldRepoFolder;
        }
    }
}