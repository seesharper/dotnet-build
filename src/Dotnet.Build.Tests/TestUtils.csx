#load "../Dotnet.Build/FileUtils.csx"
#load "../Dotnet.Build/Git.csx"

using System.Collections.ObjectModel;
using static FileUtils;
public static string[] ReadLines(this string value)
{
    Collection<string> result = new Collection<string>();
    var reader = new StringReader(value);
    while (reader.Peek() != -1)
    {
        result.Add(reader.ReadLine());
    }
    return result.ToArray();
}
public static GitRepository Init(this DisposableFolder disposableFolder)
{
    Command.Capture("git", "version").Dump();
    Command.Capture("git", $"-C {disposableFolder.Path} init").EnsureSuccessfulExitCode().Dump();
    var repo = Git.Open(disposableFolder.Path);
    repo.Execute("config --local user.email \"email@example.com\"");
    return repo;
}


public class OnlyThisAttribute : Attribute
{

}