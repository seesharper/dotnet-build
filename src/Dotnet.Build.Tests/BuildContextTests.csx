#r "nuget: AwesomeAssertions, 9.3.0"
#load "../Dotnet.Build/BuildContext.csx"
#load "nuget:ScriptUnit, 0.1.3"
#load "TestUtils.csx"
#load "../Dotnet.Build/FileUtils.csx"

using AwesomeAssertions;
using static ScriptUnit;
using static FileUtils;
using System.Xml.Linq;
using DisposableFolder = FileUtils.DisposableFolder;
#pragma warning disable 1702

// await AddTestsFrom<BuildContextTests>().Execute();
// await AddTestsFrom<BuildContextTests>().AddFilter(m => m.IsDefined(typeof(OnlyThisAttribute), true)).Execute();
public class BuildContextTests
{
    public void ShouldGetOwnerAndProjectName()
    {
        using (var repositoryFolder = new DisposableFolder())
        {
            Command.Execute("git", "clone https://github.com/seesharper/LightInject.git", repositoryFolder.Path);
            BuildContext.RepositoryFolder = Path.Combine(repositoryFolder.Path, "LightInject");
            BuildContext.TestProjects.Count().Should().Be(1);
            BuildContext.SourceProjects.Count().Should().Be(2);
            BuildContext.PackableProjects.Count().Should().Be(1);
            BuildContext.ProjectName.Should().Be("LightInject");
            BuildContext.Owner.Should().Be("seesharper");
            Directory.Exists(BuildContext.ArtifactsFolder);
            Directory.Exists(BuildContext.NuGetArtifactsFolder);
            Directory.Exists(BuildContext.GitHubArtifactsFolder);
            Directory.Exists(BuildContext.TestCoverageArtifactsFolder);
        }
    }


    public void ShouldIncludePackableConsoleAppAsPackable()
    {
        using (var repositoryFolder = new DisposableFolder())
        {
            Command.Execute("git", "clone https://github.com/seesharper/dotnet-deps.git", repositoryFolder.Path);
            BuildContext.RepositoryFolder = Path.Combine(repositoryFolder.Path, "dotnet-deps");
            BuildContext.PackableProjects.Count().Should().Be(2);
        }
    }


    public void ShouldResolveTestableProjectsWhenNameContainsTests()
    {
        using (var projectFolder = new DisposableFolder())
        {
            var srcDir = CreateDirectory(projectFolder.Path, "src");
            Command.Execute("dotnet", $"new xunit --name Sample.Tests", srcDir);
            BuildContext.RepositoryFolder = projectFolder.Path;
            BuildContext.TestProjects.Count().Should().Be(1);
        }
    }


    [OnlyThis]
    public void ShouldResolveTestableProjectsWhenProjectNameContainerIsTestProjectPropertySetToTrue()
    {
        using (var projectFolder = new DisposableFolder())
        {
            var srcDir = CreateDirectory(projectFolder.Path, "src");
            Command.Execute("dotnet", $"new xunit --name Sample.xUnit", srcDir);
            var projectFilename = Path.Combine(srcDir, "Sample.xUnit", "Sample.xUnit.csproj");
            var projectFile = XDocument.Load(projectFilename);
            var propertyGroupElement = projectFile.Descendants("PropertyGroup").Single();
            propertyGroupElement.Add(new XElement("IsTestProject", true));
            projectFile.Save(projectFilename);
            BuildContext.RepositoryFolder = projectFolder.Path;
            BuildContext.TestProjects.Count().Should().Be(1);
        }
    }

    //[OnlyThis]
    public void ShouldNotResolveTestableProjectsWhenProjectNameContainerIsTestProjectPropertySetTofalse()
    {
        using (var projectFolder = new DisposableFolder())
        {
            var srcDir = CreateDirectory(projectFolder.Path, "src");
            Command.Execute("dotnet", $"new xunit --name Sample.xUnit", srcDir);
            var projectFilename = Path.Combine(srcDir, "Sample.xUnit", "Sample.xUnit.csproj");
            var projectFile = XDocument.Load(projectFilename);
            var propertyGroupElement = projectFile.Descendants("PropertyGroup").Single();
            propertyGroupElement.Descendants("IsTestProject").Remove();
            propertyGroupElement.Add(new XElement("IsTestProject", false));
            projectFile.Save(projectFilename);

            BuildContext.RepositoryFolder = projectFolder.Path;
            BuildContext.TestProjects.Count().Should().Be(0);
        }
    }

    [OnlyThis]
    public void ShouldNotResolveTestableProjectsWhenProjectNameContainerIsTestProjectPropertySetToFalseAndNameContainsTests()
    {
        using (var projectFolder = new DisposableFolder())
        {
            var srcDir = CreateDirectory(projectFolder.Path, "src");
            Command.Execute("dotnet", $"new xunit --name Sample.xUnit.Tests", srcDir);
            var projectFilename = Path.Combine(srcDir, "Sample.xUnit.Tests", "Sample.xUnit.Tests.csproj");
            var projectFile = XDocument.Load(projectFilename);
            var propertyGroupElement = projectFile.Descendants("PropertyGroup").Single();
            propertyGroupElement.Descendants("IsTestProject").Remove();
            propertyGroupElement.Add(new XElement("IsTestProject", false));
            projectFile.Save(projectFilename);

            BuildContext.RepositoryFolder = projectFolder.Path;
            BuildContext.TestProjects.Count().Should().Be(0);
        }
    }
}