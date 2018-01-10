#load "Command.csx"
using System.Text.RegularExpressions;

private static string RemoveNewLine(this string value)
{    
    string result = Regex.Replace(value, @"\r\n?|\n", "");
    return result;
}

public class GitRepository
{
    public GitRepository(string path = null)
    {
        Path = path;
    }

    public string Path { get; }

    public CommandResult Execute(string command)
    {
        if (Path == null)
        {
            return Command.Execute("git", $"{command}");
        }
        return Command.Execute("git", $"-C {Path} {command}");
        
    }
    
    public string GetCurrentCommitHash()
    {        
        return Execute("rev-list --all --max-count=1").StandardOut.RemoveNewLine();        
    }  

    public string GetLatestTag()
    {                        
        return Execute($"describe --abbrev=0 --tags { GetCurrentCommitHash() }").StandardOut.RemoveNewLine();
    }

    public string GetLatestTagHash()
    {        
        return Execute("rev-list --tags --max-count=1").StandardOut.RemoveNewLine();
    }

    public string GetUrlToPushOrigin()
    {                
        return Execute("remote get-url --push origin").StandardOut.RemoveNewLine();
    }

    private string GetCurrentBranch()
    {
        return Execute("rev-parse --abbrev-ref HEAD").StandardOut.RemoveNewLine().ToLower();    
    }

    public string GetPreviousCommitHash()
    {
        return Execute("rev-list --tags --skip=1 --max-count=1").StandardOut.RemoveNewLine(); 
    }

    public string GetPreviousTag()
    {                
        return Execute($"describe --abbrev=0 --tags { GetPreviousCommitHash() }").StandardOut.RemoveNewLine();;        
    }

    public RepositoryInfo GetRepositoryInfo()
    {
        var urlToPushOrigin = GetUrlToPushOrigin();
        var match = Regex.Match(urlToPushOrigin, @".*.com\/(.*)\/(.*)\.");
        var owner = match.Groups[1].Value;
        var project = match.Groups[2].Value;
        return new RepositoryInfo(){Owner = owner, ProjectName = project};
    }

    public bool IsTagCommit()
    {
        var currentTagHash = GetLatestTagHash();
        var currentCommitHash = GetCurrentCommitHash();
        return currentTagHash == currentCommitHash;
    }
}

public static class Git
{        
    public static GitRepository Default = new GitRepository();
    
    public static GitRepository Open(string path)
    {
        return new GitRepository(path);
    }

    public static string GetAccessToken()
    {
        var accessToken = System.Environment.GetEnvironmentVariable("GITHUB_REPO_TOKEN");
        return accessToken;
    }
}

public class RepositoryInfo 
{
    public string Owner {get;set;}    

    public string ProjectName {get;set;}    
}