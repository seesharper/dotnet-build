#r "nuget: FluentAssertions, 5.6.0"
#load "../Dotnet.Build/Command.csx"
#load "../Dotnet.Build/DotNet.csx"
#load "nuget:ScriptUnit, 0.1.3"
#load "TestUtils.csx"


using FluentAssertions;
using static ScriptUnit;
using static FileUtils;
using System.Xml.Linq;

//await AddTestsFrom<DotNetTests>().AddFilter(m => m.IsDefined(typeof(OnlyThisAttribute), true)).Execute();

//await AddTestsFrom<DotNetTests>().Execute();

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

    //[OnlyThis]
    public void ShouldAnalyzeCodeCoverageUsingCoverletAndReportGenerator()
    {
        using (var solutionFolder = new DisposableFolder())
        {
            //Command.Execute("dotnet", "new sln");
            var srcFolder = CreateDirectory(solutionFolder.Path, "src");
            var buildFolder = CreateDirectory(solutionFolder.Path, "build");
            var artifactsFolder = CreateDirectory(buildFolder, "Artifacts");
            var projectFolder = CreateDirectory(srcFolder, "SampleProject");
            var testFolder = CreateDirectory(srcFolder, "SampleProjects.Tests");
            Command.Execute("dotnet", $"new classlib", projectFolder);
            Command.Execute("dotnet", $"new xunit", testFolder);
            Command.Execute("dotnet", "add package coverlet.collector", testFolder);
            Command.Execute("dotnet", "add reference ../SampleProject", testFolder);
            DotNet.TestWithCodeCoverage(testFolder, artifactsFolder, 100);
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

    //[OnlyThis]
    public void ShouldPackWithTagVersionWhenVersionAttributeIsMissing()
    {
        using (var projectFolder = new DisposableFolder())
        {
            Command.Execute("dotnet", $"new classlib -n TestLib -o {projectFolder.Path}");
            DotNet.Pack(projectFolder.Path, projectFolder.Path);

            string pathToNugetFile = FindFile(projectFolder.Path, "*.nupkg");
            Path.GetFileName(pathToNugetFile).Should().Be($"TestLib.{BuildContext.LatestTag}.nupkg");
        }
    }

    [OnlyThis]
    public void ShouldPackWithVersionWhenVersionIsSpecified()
    {
        using (var projectFolder = new DisposableFolder())
        {
            Command.Execute("dotnet", $"new classlib -n TestLib -o {projectFolder.Path}");


            var projectFile = FindFile(projectFolder.Path, "*.csproj");
            var document = XDocument.Load(projectFile);
            var propertyGroupElement = document.Descendants("PropertyGroup").Single();
            var isPublishableElement = new XElement("Version", "0.0.1");
            propertyGroupElement.Add(isPublishableElement);
            document.Save(projectFile);
            DotNet.Pack(projectFolder.Path, projectFolder.Path);

            string pathToNugetFile = FindFile(projectFolder.Path, "*.nupkg");


            Path.GetFileName(pathToNugetFile).Should().Be($"TestLib.0.0.1.nupkg");
        }
    }

    //[OnlyThis]
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


    //[OnlyThis]
    public void ShouldNotPublishWhenProjectIsPublishableIsSetToFalse()
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
                var isPublishableElement = new XElement("IsPublishable", false);
                propertyGroupElement.Add(isPublishableElement);
                document.Save(projectFile);
                DotNet.Publish();

                Directory.GetFiles(BuildContext.GitHubArtifactsFolder, "*.dll").Should().BeEmpty();
            }

            BuildContext.RepositoryFolder = oldRepoFolder;
        }
    }

    //[OnlyThis]
    public void ShouldNotPublishWhenProjectIsPublishablePropertyIsMissing()
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
                DotNet.Publish();

                Directory.GetFiles(BuildContext.GitHubArtifactsFolder, "*.dll").Should().BeEmpty();
            }

            BuildContext.RepositoryFolder = oldRepoFolder;
        }
    }
}