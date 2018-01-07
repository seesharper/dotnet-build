#load "Command.csx"
#load "Logger.csx"
#load "Git.csx"

public static class ReleaseNotes
{
    static ReleaseNotes()
    {
       Logger.Log("Installing Github Changelog Generator ...");
       Command.Execute("cmd.exe", "/c gem install github_changelog_generator --prerelease --force");         
    }
    
    public static void Generate(string pathToRepository, string pathToChangeLog)
    {                                
        var repo = Git.Open(pathToRepository);
        
        bool isTagCommit = repo.IsTagCommit();        
        string sinceTag = isTagCommit ? repo.GetPreviousTag() : repo.GetLatestTag();
                
        var repositoryInfo = repo.GetRepositoryInfo();
        
        var token = Git.GetAccessToken();              
        Logger.Log($"Creating changelog since tag {sinceTag}");
        var args = $"/c github_changelog_generator --user {repositoryInfo.Owner} --project {repositoryInfo.ProjectName} --since-tag {sinceTag} --token {token} --output {pathToChangeLog}";
        if (isTagCommit)
        {
            args = args + " --no-unreleased";            
        }        

        Command.Execute("cmd.exe",args).EnsureSuccessfulExitCode().Dump();    
    }
}