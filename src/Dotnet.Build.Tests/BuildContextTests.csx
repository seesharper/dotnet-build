#r "nuget: FluentAssertions, 5.6.0"
#load "../Dotnet.Build/BuildContext.csx"
#load "nuget:ScriptUnit, 0.1.3"
#load "TestUtils.csx"
#load "../Dotnet.Build/FileUtils.csx"

using FluentAssertions;
using static ScriptUnit;
using static FileUtils;

#pragma warning disable 1702

//await AddTestsFrom<BuildContextTests>().AddFilter(m => m.IsDefined(typeof(OnlyThisAttribute), true)).Execute();
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

    [OnlyThis]
    public void ShouldIncludePackableConsoleAppAsPackable()
    {
        using (var repositoryFolder = new DisposableFolder())
        {
            Command.Execute("git", "clone https://github.com/seesharper/dotnet-deps.git", repositoryFolder.Path);
            BuildContext.RepositoryFolder = Path.Combine(repositoryFolder.Path, "dotnet-deps");
            BuildContext.PackableProjects.Count().Should().Be(2);
        }
    }
}