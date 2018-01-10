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
    
    public static void Generate(string pathToChangeLog, string pathToRepository = null)
    {                                
        var repo = Git.Open(pathToRepository);                
        bool isTagCommit = repo.IsTagCommit();

        var previousTag = repo.GetPreviousTag();
        var latestTag = repo.GetLatestTag();
        
        var repositoryInfo = repo.GetRepositoryInfo();
        
        var token = Git.GetAccessToken();              
        
        var args = $"/c github_changelog_generator --user {repositoryInfo.Owner} --project {repositoryInfo.ProjectName} --token {token} --output {pathToChangeLog}";
        if (isTagCommit)
        {
            args = args + " --no-unreleased";
            if (!string.IsNullOrWhiteSpace(previousTag))
            {
                Logger.Log($"Creating changelog since tag {previousTag}");
                args = args + $" --since-tag {previousTag}"; 
            }
        }
        else
        {
            if (!string.IsNullOrWhiteSpace(latestTag))
            {
                Logger.Log($"Creating changelog since tag {previousTag}");
                args = args + $" --since-tag {latestTag}"; 
            }
        }
        
        Command.Execute("cmd.exe",args).EnsureSuccessfulExitCode().Dump();    
    }
}