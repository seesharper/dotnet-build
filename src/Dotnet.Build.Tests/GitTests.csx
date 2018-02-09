#! "netcoreapp2.0"
#r "nuget: FluentAssertions, 4.19.4"
#load "../Dotnet.Build/Git.csx"
#load "../Dotnet.Build/Command.csx"
#load "../Dotnet.Build/FileUtils.csx"
#load "nuget:ScriptUnit, 0.1.3"


using FluentAssertions;
using static ScriptUnit; 
using static FileUtils;
//await AddTestsFrom<GitTests>().Execute();
// await AddTestsFrom<GitTests>().AddFilter(m => m.IsDefined(typeof(OnlyThisAttribute), true)).Execute();

private static GitRepository Init(this DisposableFolder disposableFolder)
{
    Command.Capture("git", $"-C {disposableFolder.Path} init").EnsureSuccessfulExitCode().Dump();    
    var repo = Git.Open(disposableFolder.Path);
    repo.Execute("config --local user.email \"email@example.com\"");
    return repo;
}

public class GitTests
{
    public void ShouldGetCurrentCommitHash()
    {
        using (var folder = new DisposableFolder())
        {                    
            var repo = folder.Init();
            repo.Execute("commit --allow-empty -m \"First Commit\"");
            var latestCommitHash = repo.GetCurrentCommitHash();            
            latestCommitHash.Should().NotBeEmpty();                      
        }
    }

    public void ShouldGetLatestTag()
    {
        using (var folder = new DisposableFolder())
        {            
            var repo = folder.Init();
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
            var repo = folder.Init();
            repo.Execute("commit --allow-empty -m \"First Commit\"");            
            var latestTag = repo.GetLatestTag();
            latestTag.Should().BeEmpty();         
        }
    } 

    
    public void ShouldDetectUntrackedFiles()
    {
        using (var folder = new DisposableFolder())
        {            
            var repo = folder.Init();
            repo.HasUntrackedFiles().Should().BeFalse();
            File.Create(Path.Combine(folder.Path,"README.MD")).Close();
            repo.HasUntrackedFiles().Should().BeTrue();
            repo.Execute("add .");
            repo.HasUntrackedFiles().Should().BeFalse();
        }
    }
    
    public void ShouldDetectStagedFiles()
    {
        using (var folder = new DisposableFolder())
        {            
            var repo = folder.Init();
            repo.HasStagedFiles().Should().BeFalse();
            File.Create(Path.Combine(folder.Path,"README.MD")).Close();            
            repo.Execute("add .");
            repo.HasStagedFiles().Should().BeTrue();
            repo.Execute("commit -m \"Added file \"");
            repo.HasStagedFiles().Should().BeFalse();
        }
    }

    public void ShouldGetRemoteTags()
    {
        
    }


    public void ShouldDetectLocalTags()
    {

    }

}

