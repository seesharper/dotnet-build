#! "netcoreapp2.0"
#r "nuget: FluentAssertions, 4.19.4"
#load "../Dotnet.Build/Git.csx"
#load "../Dotnet.Build/FileUtils.csx"
#load "nuget:ScriptUnit, 0.1.1"
#load "TestUtils.csx"

using FluentAssertions;
using static ScriptUnit; 

await AddTestsFrom<GitTests>().Execute();


public class GitTests
{
    public void ShouldGetCurrentCommitHash()
    {
        using (var folder = new DisposableFolder())
        {            
            var repo = Git.Init(folder.Path);                        
            repo.Execute("commit --allow-empty -m \"First Commit\"");
            var latestCommitHash = repo.GetCurrentCommitHash();            
            latestCommitHash.Should().NotBeEmpty();                      
        }
    }

    public void ShouldGetLatestTag()
    {
        using (var folder = new DisposableFolder())
        {            
            var repo = Git.Init(folder.Path);                        
            repo.Execute("commit --allow-empty -m \"First Commit\"");
            repo.Execute("tag 1.0.0");
            var latestTag = repo.GetLatestTag();
            latestTag.Should().Be("1.0.0");              
        }
    }

    public void ShouldReturnEmptyStringForLatestTag()
    {
        using (var folder = new DisposableFolder())
        {            
            var repo = Git.Init(folder.Path);                        
            repo.Execute("commit --allow-empty -m \"First Commit\"");            
            var latestTag = repo.GetLatestTag();
            latestTag.Should().BeEmpty();         
        }
    } 
}

